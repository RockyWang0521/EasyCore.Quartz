using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyCore.Quartz.Hosting;

/// <summary>
/// Runs database schema bootstrap before the scheduler is heavily used.
/// </summary>
public sealed class SchemaBootstrapHostedService : IHostedService
{
    private readonly EasyCoreQuartzOptions _options;
    private readonly ILogger<SchemaBootstrapHostedService> _logger;

    public SchemaBootstrapHostedService(
        IOptions<EasyCoreQuartzOptions> options,
        ILogger<SchemaBootstrapHostedService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoCreateSchema)
        {
            return;
        }

        var (connectionString, sqlType) = _options.GetSettings();
        if (sqlType == SqlType.None || string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        if (_options.SchemaBootstrapper is null)
        {
            _logger.LogWarning(
                "AutoCreateSchema is enabled for {SqlType} but no schema bootstrapper is registered. " +
                "Install the matching EasyCore.Quartz.* provider package and call its Use* extension.",
                sqlType);
            return;
        }

        _logger.LogInformation("Ensuring Quartz schema exists for {SqlType}...", sqlType);
        await _options.SchemaBootstrapper
            .EnsureCreatedAsync(connectionString, _options.TablePrefix, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogInformation("Quartz schema check completed for {SqlType}.", sqlType);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
