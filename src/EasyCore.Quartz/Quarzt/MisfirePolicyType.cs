namespace EasyCore.Quartz
{
    /// <summary>
    /// Represents the strategy Quartz should use when a scheduled trigger is misfired (i.e., missed its scheduled time).
    /// </summary>
    public enum MisfirePolicyType
    {
        /// <summary>
        /// Use Quartz's default misfire handling strategy.
        /// If no policy is explicitly set, the default behavior will apply (usually depends on trigger type).
        /// </summary>
        Default,

        /// <summary>
        /// Instructs Quartz to ignore misfires and continue with the next scheduled execution.
        /// Missed executions will not be retried.
        /// </summary>
        Ignore,

        /// <summary>
        /// Instructs Quartz to immediately trigger the job once if a misfire occurred,
        /// and then continue with the normal schedule.
        /// </summary>
        FireNow,

        /// <summary>
        /// Instructs Quartz to do nothing when a misfire occurs.
        /// The job will simply wait until the next scheduled execution.
        /// </summary>
        DoNothing
    }

}
