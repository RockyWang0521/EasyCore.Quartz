using EasyCore.Quartz;
using Quartz;

namespace WebApp.Quartz.SqlServer.Jobs;

[EasyCoreCron("0/10 * * * * ?")]
[EasyCoreDisallowConcurrentExecution]
public sealed class SampleJob : IEasyCoreJob
{
    private readonly ILogger<SampleJob> _logger;

    public SampleJob(ILogger<SampleJob> logger) => _logger = logger;

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("SampleJob is running at {Time}", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}

[EasyCoreDisableJob]
[EasyCoreCron("0/1 * * * * ?")]
public sealed class DisabledSampleJob : IEasyCoreJob
{
    private readonly ILogger<DisabledSampleJob> _logger;

    public DisabledSampleJob(ILogger<DisabledSampleJob> logger) => _logger = logger;

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("DisabledSampleJob should never run");
        return Task.CompletedTask;
    }
}
