using EasyCore.Quartz;
using Quartz;

namespace WebApp.Quartz.Quarzt
{
    [EasyCoreCron("0/1 * * * * ?")]
    public class QuarztTask : IEasyCoreJob
    {
        private readonly ILogger<QuarztTask> _logger;

        public QuarztTask(ILogger<QuarztTask> logger) => _logger = logger;

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.CompletedTask;

            _logger.LogInformation("QuarztTask is running");
        }
    }
}
