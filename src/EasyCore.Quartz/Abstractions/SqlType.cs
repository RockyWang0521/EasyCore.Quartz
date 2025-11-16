namespace EasyCore.Quartz;

/// <summary>
/// Supported persistent store database providers.
/// </summary>
public enum SqlType
{
    /// <summary>In-memory (RAM) job store.</summary>
    None,

    /// <summary>MySQL.</summary>
    MySql,

    /// <summary>Microsoft SQL Server.</summary>
    SqlServer,

    /// <summary>PostgreSQL.</summary>
    PostgreSql,

    /// <summary>Oracle Database.</summary>
    Oracle
}
