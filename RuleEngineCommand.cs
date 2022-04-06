// The Namespace is important for matching the incoming Message!
namespace Lpa.Capmatix.KidCalcService.Contracts
{
    public class RuleEngineCommand
    {
        public RuleEngineCommand(int workflowId, string jobId, string taskName)//, string taskResult)
        {
            WorkflowId = workflowId;
            JobId = jobId;
            TaskName = taskName;
            //TaskResult = taskResult;
        }

        public int WorkflowId { get; set; }

        public string JobId {get; set; }

        public string TaskName { get; set; }

        //public string TaskResult { get; set; }
    }
}
