namespace EasyCore.Quartz.Dto;

/// <summary>
/// Input for creating or updating an HTTP job.
/// </summary>
public sealed class HttpJobInputDto
{
    /// <summary>Unique job name.</summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>Job group. Defaults to DEFAULT.</summary>
    public string JobGroup { get; set; } = "DEFAULT";

    /// <summary>Target URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>HTTP method (GET, POST, PUT, DELETE, PATCH).</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Optional JSON body.</summary>
    public string? Body { get; set; }

    /// <summary>Optional request headers.</summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>Cron expression.</summary>
    public string Cron { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
}
