using Microsoft.Extensions.Hosting;
using Workflow.Masstransit;

namespace RuleEngine.Cli
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class MassTransitRunner
    {
        private Task _task;
        private IHost _host;
        private CancellationTokenSource _Cancellation = new CancellationTokenSource();
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly string[] _args;

        public MassTransitRunner(ILogger logger, IConfiguration config, string[] args)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config;
            _args = args;
        }

        public void Start() {
            try
            {
                _logger.LogInformation("Starting...");

                var hostBuilder = _args.CreateMassTransitHostBuilder(null, null, Program.ConfigureServices, Assembly.GetExecutingAssembly());
                _host = hostBuilder.UseWindowsService().Build();
                _task = _host.StartAsync(_Cancellation.Token);//.ConfigureAwait(false);
                //_host.Start();
                //_host.RunAsync(_Cancellation.Token);//.ConfigureAwait(false);
                //await _task; => stopped without Starting

                _logger.LogInformation("Started");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Starting...");
            }
        }

        public void Stop()
        {
            try
            {
                _logger.LogInformation("Stopping...");
                _task = _host.StopAsync(TimeSpan.FromMilliseconds(200));
                _logger.LogInformation(_task.IsCompleted ? "Stopped " : "Not stopped in time");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Stopping...");
            }
        }
    }
}
