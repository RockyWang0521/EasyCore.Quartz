using System.Collections.Concurrent;
using EasyCore.Quartz.Dto;
using Quartz;
using Quartz.Listener;

namespace EasyCore.Quartz.History;

/// <summary>
/// Records job execution outcomes into <see cref="IJobExecutionHistoryStore"/>.
/// </summary>
public sealed class JobExecutionHistoryListener : JobListenerSupport
{
    private readonly IJobExecutionHistoryStore _store;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _started = new();

    public JobExecutionHistoryListener(IJobExecutionHistoryStore store)
    {
        _store = store;
    }

    public override string Name => "EasyCore.Quartz.JobExecutionHistoryListener";

    public override Task JobToBeExecuted(
        IJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        _started[context.FireInstanceId] = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public override Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        _started.TryRemove(context.FireInstanceId, out var started);
        var finished = DateTimeOffset.UtcNow;
        if (started == default)
        {
            started = finished;
        }

        _store.Record(new JobExecutionHistoryDto
        {
            JobName = context.JobDetail.Key.Name,
            JobGroup = context.JobDetail.Key.Group,
            TriggerName = context.Trigger.Key.Name,
            TriggerGroup = context.Trigger.Key.Group,
            FiredAtUtc = started,
            FinishedAtUtc = finished,
            DurationMs = (finished - started).TotalMilliseconds,
            Success = jobException is null,
            ExceptionMessage = jobException?.GetBaseException().Message,
            ExceptionType = jobException?.GetBaseException().GetType().FullName,
            FireInstanceId = context.FireInstanceId
        });

        return Task.CompletedTask;
    }

    public override Task JobExecutionVetoed(
        IJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        _started.TryRemove(context.FireInstanceId, out _);
        return Task.CompletedTask;
    }
}
