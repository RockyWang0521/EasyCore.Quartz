namespace EasyCore.Quartz.Dto;

/// <summary>
/// A single job execution history record (process-local).
/// </summary>
public sealed class JobExecutionHistoryDto
{
    public string JobName { get; set; } = string.Empty;
    public string JobGroup { get; set; } = "DEFAULT";
    public string TriggerName { get; set; } = string.Empty;
    public string TriggerGroup { get; set; } = "DEFAULT";
    public DateTimeOffset FiredAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public double? DurationMs { get; set; }
    public bool Success { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
    public string FireInstanceId { get; set; } = string.Empty;
}
