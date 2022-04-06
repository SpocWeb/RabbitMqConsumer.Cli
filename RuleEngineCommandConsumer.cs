using Lpa.Capmatix.KidCalcService.Contracts;

namespace RuleEngine.Cli
{
    using System.Threading;
    using System.Threading.Tasks;
    using MassTransit;
    using Microsoft.Extensions.Logging;

    /// <inheritdoc />
    public class RuleEngineCommandConsumer : IConsumer<RuleEngineCommand>
    {
        private readonly ILogger<RuleEngineCommandConsumer> _logger;

        public RuleEngineCommandConsumer(ILogger<RuleEngineCommandConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<RuleEngineCommand> context)
        {
            this._logger.LogInformation("Processing {workflowId}", context.Message.WorkflowId);
            Thread.Sleep(999);
            return Task.CompletedTask;
        }
    }
}