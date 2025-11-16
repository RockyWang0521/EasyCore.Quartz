using Microsoft.Extensions.Logging;
using Quartz;

namespace EasyCore.Quartz.Jobs;

/// <summary>
/// Wraps an <see cref="IEasyCoreJob"/> with structured logging.
/// Exceptions are logged and rethrown so Quartz can mark the execution as failed.
/// </summary>
public sealed class JobWrapper<T> : IJob where T : IEasyCoreJob
{
    private readonly T _inner;
    private readonly ILogger<JobWrapper<T>> _logger;

    /// <summary>
    /// Creates a new wrapper.
    /// </summary>
    public JobWrapper(T inner, ILogger<JobWrapper<T>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var jobName = typeof(T).Name;
        try
        {
            _logger.LogInformation("Starting job execution: {Job}", jobName);
            await _inner.Execute(context).ConfigureAwait(false);
            _logger.LogInformation("Job execution completed: {Job}", jobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job execution failed: {Job}", jobName);
            throw;
        }
    }
}
