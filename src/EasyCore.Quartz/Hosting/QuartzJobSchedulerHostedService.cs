using Quartz;

namespace EasyCore.Quartz.Hosting;

/// <summary>
/// Schedules discovered attribute-based jobs at application startup.
/// </summary>
public sealed class QuartzJobSchedulerHostedService : IHostedService
{
    private readonly IEnumerable<(IJobDetail, ITrigger)> _jobsToSchedule;
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzJobSchedulerHostedService(
        IEnumerable<(IJobDetail, ITrigger)> jobsToSchedule,
        ISchedulerFactory schedulerFactory)
    {
        _jobsToSchedule = jobsToSchedule;
        _schedulerFactory = schedulerFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);

        foreach (var (job, trigger) in _jobsToSchedule)
        {
            if (!await scheduler.CheckExists(job.Key, cancellationToken).ConfigureAwait(false))
            {
                await scheduler.ScheduleJob(job, trigger, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
