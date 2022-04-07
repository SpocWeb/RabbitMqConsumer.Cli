using Workflow.Masstransit;

namespace RuleEngine.Cli
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public static class Program
    {
        public static void Main(string[] args) => StartMassTransitRunner(args);//Task.FromResult(StartMassTransitRunner(args));

        public static void StartMassTransitRunner(string[] args)
        {
            //ConsoleHost

            var oldDir = Directory.GetCurrentDirectory();
            try
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                Directory.SetCurrentDirectory(Directory.GetParent(assemblyPath)!.FullName);

                var hostBuilder = args.CreateMassTransitHostBuilder(null, null
                    , ConfigureServices, Assembly.GetExecutingAssembly());
                hostBuilder = hostBuilder.UseWindowsService(); //automatically detects whether in Service or Console...
                //hostBuilder = hostBuilder.UseConsoleLifetime(); ...therefore this is not needed and even toxic for Windows-Service
                var host = hostBuilder.Build();
                host.Run(); //runs all registered IHostedService Classes and waits for their Completion
            }
            finally
            {
                Directory.SetCurrentDirectory(oldDir);
            }
        }

        /// <summary> Creating the Singletons takes Time </summary>
        internal static void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
	        log4net.GlobalContext.Properties["ApplicationName"] = typeof(Program).Assembly.GetName().Name;
	        services.AddLogging(b => b.AddLog4Net("log4net.config", true));
        }
    }
}
