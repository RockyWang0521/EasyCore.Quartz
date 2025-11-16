namespace EasyCore.Quartz;

/// <summary>PostgreSQL provider extensions.</summary>
public static class QuartzPostgreSqlExtensions
{
    public static EasyCoreQuartzOptions UsePostgreSql(this EasyCoreQuartzOptions options, Action<PostgreSqlOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        var pg = new PostgreSqlOptions();
        configure(pg);
        options.SetPostgreSql(pg.ConnectionString);
        options.SchemaBootstrapper = new PostgreSqlSchemaBootstrapper();
        return options;
    }
}
