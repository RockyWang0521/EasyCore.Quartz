namespace EasyCore.Quartz;

/// <summary>SQL Server provider extensions.</summary>
public static class QuartzSqlServerExtensions
{
    public static EasyCoreQuartzOptions UseSqlServer(this EasyCoreQuartzOptions options, Action<SqlServerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        var sql = new SqlServerOptions();
        configure(sql);
        options.SetSqlServer(sql.ConnectionString);
        options.SchemaBootstrapper = new SqlServerSchemaBootstrapper();
        return options;
    }

    public static void EasyCoreQuartzSqlServer(this EasyCoreQuartzOptions options, Action<SqlServerOptions> action)
        => UseSqlServer(options, action);
}
