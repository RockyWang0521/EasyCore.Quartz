namespace EasyCore.Quartz.Dto;

/// <summary>
/// Scheduler overview statistics for the dashboard.
/// </summary>
public sealed class SchedulerOverviewDto
{
    public string SchedulerName { get; set; } = string.Empty;
    public string SchedulerInstanceId { get; set; } = string.Empty;
    public bool IsStarted { get; set; }
    public bool InStandbyMode { get; set; }
    public bool IsShutdown { get; set; }
    public int JobCount { get; set; }
    public int TriggerCount { get; set; }
    public int ExecutingCount { get; set; }
    public int PausedTriggerCount { get; set; }
    public int ErrorTriggerCount { get; set; }
    /// <summary>
    /// Successful executions currently held in the process-local history window
    /// (ring buffer of <c>HistoryCapacity</c> entries — not a lifetime counter).
    /// </summary>
    public int RecentSuccessCount { get; set; }

    /// <summary>
    /// Failed executions currently held in the process-local history window
    /// (ring buffer of <c>HistoryCapacity</c> entries — not a lifetime counter).
    /// </summary>
    public int RecentFailureCount { get; set; }
    public string StoreType { get; set; } = "RAM";
    public DateTimeOffset ServerTimeUtc { get; set; }
}
