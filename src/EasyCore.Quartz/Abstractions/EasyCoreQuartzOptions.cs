using System.Reflection;

namespace EasyCore.Quartz;

/// <summary>
/// Configuration options for EasyCore.Quartz.
/// </summary>
public sealed class EasyCoreQuartzOptions
{
    private readonly List<Assembly> _assemblies = new();
    private string? _connectionString;
    private SqlType _sqlType = SqlType.None;

    /// <summary>Cluster check-in misfire threshold in seconds.</summary>
    public int CheckinMisfireThreshold { get; set; } = 10;

    /// <summary>Cluster check-in interval in seconds.</summary>
    public int CheckinInterval { get; set; } = 5;

    /// <summary>Maximum thread-pool concurrency. Zero means auto (discovered job count, minimum 10).</summary>
    public int MaxConcurrency { get; set; } = 20;

    /// <summary>Display offset in hours relative to UTC for next/previous fire times.</summary>
    public int TimeZoneOffsetHours { get; set; } = 0;

    /// <summary>When true, provider packages may auto-create Quartz tables on startup.</summary>
    public bool AutoCreateSchema { get; set; } = true;

    /// <summary>Maximum in-memory execution history records kept per process.</summary>
    public int HistoryCapacity { get; set; } = 200;

    /// <summary>Throw when job discovery fails to load an assembly. Default false (log and continue).</summary>
    public bool ThrowOnDiscoveryErrors { get; set; } = false;

    /// <summary>Quartz ADO table prefix. Must match DDL. Default <c>QRTZ_</c>.</summary>
    public string TablePrefix { get; set; } = "QRTZ_";

    /// <summary>Optional schema bootstrapper registered by database provider packages.</summary>
    public IQuartzSchemaBootstrapper? SchemaBootstrapper { get; set; }

    /// <summary>Assemblies explicitly registered for job discovery.</summary>
    public IReadOnlyList<Assembly> Assemblies => _assemblies;

    /// <summary>Adds an assembly to scan for <see cref="IEasyCoreJob"/> types.</summary>
    public EasyCoreQuartzOptions AddAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (!_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }

        return this;
    }

    /// <summary>Adds the assembly that contains <typeparamref name="T"/>.</summary>
    public EasyCoreQuartzOptions AddAssemblyFrom<T>() => AddAssembly(typeof(T).Assembly);

    internal void SetStore(SqlType sqlType, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        _sqlType = sqlType;
        _connectionString = connectionString;
    }

    /// <summary>Configures SQL Server persistence (prefer provider extension UseSqlServer).</summary>
    public void SetSqlServer(string connectionString) => SetStore(SqlType.SqlServer, connectionString);

    /// <summary>Configures MySQL persistence (prefer provider extension UseMySql).</summary>
    public void SetMySql(string connectionString) => SetStore(SqlType.MySql, connectionString);

    /// <summary>Configures PostgreSQL persistence (prefer provider extension UsePostgreSql).</summary>
    public void SetPostgreSql(string connectionString) => SetStore(SqlType.PostgreSql, connectionString);

    /// <summary>Configures Oracle persistence (prefer provider extension UseOracle).</summary>
    public void SetOracle(string connectionString) => SetStore(SqlType.Oracle, connectionString);

    /// <summary>Returns the configured connection string, or null for RAM store.</summary>
    public string? GetSqlConnectionString() => _connectionString;

    internal (string? ConnectionString, SqlType SqlType) GetSettings() => (_connectionString, _sqlType);
}
