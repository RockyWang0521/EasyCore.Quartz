namespace EasyCore.Quartz;

/// <summary>
/// Extension methods for configuring MySQL persistence.
/// </summary>
public static class QuartzMySqlExtensions
{
    /// <summary>
    /// Configures EasyCore.Quartz to use MySQL as the persistent job store.
    /// </summary>
    public static EasyCoreQuartzOptions UseMySql(this EasyCoreQuartzOptions options, Action<MySqlOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        var mysql = new MySqlOptions();
        configure(mysql);
        options.SetMySql(mysql.ConnectionString);
        options.SchemaBootstrapper = new MySqlSchemaBootstrapper();
        return options;
    }

    /// <summary>
    /// Legacy alias for <see cref="UseMySql"/>.
    /// </summary>
    public static void EasyCoreQuartzMySql(this EasyCoreQuartzOptions options, Action<MySqlOptions> action)
        => UseMySql(options, action);
}
