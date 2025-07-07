namespace EasyCore.Quartz
{
    /// <summary>
    /// Represents the status information of a Quartz job.
    /// </summary>
    public class JobStatusInfoDto
    {
#pragma warning disable CS8618

        /// <summary>
        /// Gets or sets the name of the job.
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// Gets or sets the group name of the job.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the key of the trigger associated with the job.
        /// </summary>
        public string TriggerKey { get; set; }

        /// <summary>
        /// Gets or sets the current trigger state.
        /// Possible values: Normal, Paused, Complete, Error, Blocked, None.
        /// </summary>
        public string TriggerState { get; set; }

        /// <summary>
        /// Gets or sets the Cron expression that defines the job's execution schedule.
        /// </summary>
        public string? CronExpression { get; set; }

        /// <summary>
        /// Gets or sets the next scheduled fire time of the trigger.
        /// </summary>
        public DateTimeOffset? NextFireTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the job is currently executing.
        /// </summary>
        public bool IsRunning { get; set; }

#pragma warning restore CS8618
    }
}
