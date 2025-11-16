namespace EasyCore.Quartz.Dto;

/// <summary>
/// Status information for a scheduled job trigger.
/// </summary>
public sealed class JobStatusInfoDto
{
    /// <summary>Job name.</summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>Job group.</summary>
    public string Group { get; set; } = "DEFAULT";

    /// <summary>Trigger key string.</summary>
    public string TriggerKey { get; set; } = string.Empty;

    /// <summary>Quartz trigger state.</summary>
    public string TriggerState { get; set; } = string.Empty;

    /// <summary>Cron expression, when applicable.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Next fire time in the configured display offset.</summary>
    public DateTimeOffset? NextFireTime { get; set; }

    /// <summary>Previous fire time in the configured display offset.</summary>
    public DateTimeOffset? PreviousFireTime { get; set; }

    /// <summary>True when this job is currently executing on this scheduler.</summary>
    public bool IsCurrentlyExecuting { get; set; }

    /// <summary>Job class full name.</summary>
    public string? JobType { get; set; }

    /// <summary>Job description.</summary>
    public string? Description { get; set; }

    /// <summary>Selected JobDataMap entries (string values).</summary>
    public Dictionary<string, string>? JobData { get; set; }
}
