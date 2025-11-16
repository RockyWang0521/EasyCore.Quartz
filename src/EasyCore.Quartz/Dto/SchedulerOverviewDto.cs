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
    public int RecentSuccessCount { get; set; }
    public int RecentFailureCount { get; set; }
    public string StoreType { get; set; } = "RAM";
    public DateTimeOffset ServerTimeUtc { get; set; }
}
