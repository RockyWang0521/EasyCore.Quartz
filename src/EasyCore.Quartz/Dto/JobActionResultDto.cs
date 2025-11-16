namespace EasyCore.Quartz.Dto;

/// <summary>
/// Result of a job management action.
/// </summary>
public sealed class JobActionResultDto
{
    /// <summary>Job name.</summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>Job group.</summary>
    public string Group { get; set; } = "DEFAULT";

    /// <summary>Whether the action succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Human-readable message.</summary>
    public string Message { get; set; } = string.Empty;
}
