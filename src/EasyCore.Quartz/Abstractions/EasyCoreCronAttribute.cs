namespace EasyCore.Quartz;

/// <summary>
/// Marks a job class with a cron schedule for automatic registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EasyCoreCronAttribute : Attribute
{
    /// <summary>
    /// Creates a new cron attribute.
    /// </summary>
    /// <param name="cronExpression">Quartz cron expression.</param>
    public EasyCoreCronAttribute(string cronExpression)
    {
        CronExpression = cronExpression ?? throw new ArgumentNullException(nameof(cronExpression));
    }

    /// <summary>Cron expression defining the job schedule.</summary>
    public string CronExpression { get; }

    /// <summary>Optional job group. Defaults to "DEFAULT".</summary>
    public string? JobGroup { get; set; }

    /// <summary>Optional job key. Defaults to the type full name.</summary>
    public string? JobKey { get; set; }

    /// <summary>Misfire handling policy.</summary>
    public MisfirePolicyType MisfirePolicy { get; set; } = MisfirePolicyType.DoNothing;

    /// <summary>Whether Quartz should request recovery after a node failure.</summary>
    public bool RequestRecovery { get; set; } = true;
}
