namespace EasyCore.Quartz;

/// <summary>
/// MySQL connection options for EasyCore.Quartz.
/// </summary>
public sealed class MySqlOptions
{
    /// <summary>MySQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
