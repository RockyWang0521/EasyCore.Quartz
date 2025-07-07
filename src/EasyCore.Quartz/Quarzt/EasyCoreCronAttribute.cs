namespace EasyCore.Quartz
{
    /// <summary>
    /// Defines a Cron-based scheduling attribute for Quartz jobs.
    /// Use this attribute to mark a job class with a CRON expression
    /// so that it can be automatically scheduled by EasyCoreQuartz.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class EasyCoreCronAttribute : Attribute
    {
        /// <summary>
        /// The CRON expression defining the job schedule.
        /// </summary>
        public string CronExpression { get; }

        /// <summary>
        /// Optional job group name. Defaults to "DEFAULT" if not specified.
        /// </summary>
        public string? JobGroup { get; set; }

        /// <summary>
        /// Optional job key (unique identifier). Defaults to the full name of the class.
        /// </summary>
        public string? JobKey { get; set; }

        /// <summary>
        /// Misfire handling strategy to use when a scheduled execution is missed.
        /// Default is MisfirePolicyType.Default.
        /// </summary>
        public MisfirePolicyType MisfirePolicy { get; set; } = MisfirePolicyType.DoNothing;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyCoreCronAttribute"/> class.
        /// </summary>
        /// <param name="cronExpression">The CRON expression used for scheduling.</param>
        public EasyCoreCronAttribute(string cronExpression)
        {
            CronExpression = cronExpression;
        }
    }
}
