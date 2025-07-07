using Quartz;

namespace EasyCore.Quartz.Quarzt
{
    /// <summary>
    /// A hosted service that schedules and starts Quartz jobs at application startup.
    /// </summary>
    public class QuartzJobSchedulerHostedService : IHostedService
    {
        private readonly IEnumerable<(IJobDetail, ITrigger)> _jobsToSchedule;
        private readonly ISchedulerFactory _schedulerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuartzJobSchedulerHostedService"/> class.
        /// </summary>
        /// <param name="jobsToSchedule">A collection of job and trigger pairs to be scheduled.</param>
        /// <param name="schedulerFactory">The Quartz scheduler factory.</param>
        public QuartzJobSchedulerHostedService(
            IEnumerable<(IJobDetail, ITrigger)> jobsToSchedule,
            ISchedulerFactory schedulerFactory)
        {
            _jobsToSchedule = jobsToSchedule;

            _schedulerFactory = schedulerFactory;
        }

        /// <summary>
        /// Starts the hosted service and schedules the configured jobs if they are not already scheduled.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            foreach (var (job, trigger) in _jobsToSchedule)
            {
                if (!await scheduler.CheckExists(job.Key, cancellationToken))
                {
                    await scheduler.ScheduleJob(job, trigger, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Stops the hosted service. No explicit shutdown logic is needed in this implementation.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        public async Task StopAsync(CancellationToken cancellationToken) => await Task.CompletedTask;
    }
}
