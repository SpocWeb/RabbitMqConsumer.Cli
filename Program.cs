namespace RuleEngine.Cli
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Topshelf;

    public static class Program
    {
        private static IConfigurationRoot _config;
        private static ILogger _logger;

        public static Task Main(string[] args) => Task.FromResult(StartMassTransitRunner(args));

        public static Task StartMassTransitRunner(string[] args)
        {
            var oldDir = Directory.GetCurrentDirectory();
            try
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                Directory.SetCurrentDirectory(Directory.GetParent(assemblyPath).FullName);
                _config = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .AddJsonFile("appSettings.json")
                    .Build();

                void Configure(ILoggingBuilder builder) => builder.AddLog4Net();
                var loggerFactory = LoggerFactory.Create(configure: Configure);
                _logger = loggerFactory.CreateLogger(typeof(Program));
                _logger.LogInformation("Started " + typeof(Program).FullName);

                var massTransitRunner = new MassTransitRunner(_logger, _config, args);
                _ = HostFactory.Run(x =>
                {
                    x.Service<MassTransitRunner>(s =>
                                        {
                                            s.ConstructUsing(runner => massTransitRunner);
                                            s.WhenStarted(runner => runner.Start());
                                            s.WhenStopped(runner => runner.Stop());
                                        });
                    x.RunAsNetworkService();
                    x.SetServiceName(nameof(RuleEngineCommandConsumer));
                    x.SetDisplayName("_RuleEngine-CommandConsumer");
                    x.SetDescription("Processes Commands with Results to trigger Transitions");
                });
            }
            catch (Exception x)
            {
                _logger.LogError(x, nameof(StartMassTransitRunner));
            }
            finally
            {
                Directory.SetCurrentDirectory(oldDir);
            }
            return Task.CompletedTask;
        }

        /// <summary> Creating the Singletons takes Time </summary>
        internal static void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
	        log4net.GlobalContext.Properties["ApplicationName"] = typeof(Program).Assembly.GetName().Name;
	        services.AddLogging(b => b.AddLog4Net());

            services.AddSingleton(_config);
        }
    }
}
