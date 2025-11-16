namespace EasyCore.Quartz;

/// <summary>
/// Creates Quartz persistence tables when <see cref="EasyCoreQuartzOptions.AutoCreateSchema"/> is enabled.
/// </summary>
public interface IQuartzSchemaBootstrapper
{
    /// <summary>Ensures the Quartz schema exists for the given connection string.</summary>
    Task EnsureCreatedAsync(string connectionString, string tablePrefix, CancellationToken cancellationToken = default);
}
