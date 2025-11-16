namespace EasyCore.Quartz;

/// <summary>Oracle provider extensions.</summary>
public static class QuartzOracleExtensions
{
    public static EasyCoreQuartzOptions UseOracle(this EasyCoreQuartzOptions options, Action<OracleOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        var oracle = new OracleOptions();
        configure(oracle);
        options.SetOracle(oracle.ConnectionString);
        options.SchemaBootstrapper = new OracleSchemaBootstrapper();
        return options;
    }
}
