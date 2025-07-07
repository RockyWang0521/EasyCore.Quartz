using EasyCore.Quartz;
using Quartz;

namespace WebApp.Quartz.Quarzt
{
    [DisallowConcurrentExecution]
    [EasyCoreDisableJob]
    [EasyCoreCron("0/1 * * * * ?")]
    public class DisableJobTask : IEasyCoreJob
    {
        private readonly ILogger<DisableJobTask> _logger;

        public DisableJobTask(ILogger<DisableJobTask> logger) => _logger = logger;

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.CompletedTask;

            _logger.LogInformation("DisableJobTask is running");
        }
    }
}
