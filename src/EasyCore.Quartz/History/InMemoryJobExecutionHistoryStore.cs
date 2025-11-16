using EasyCore.Quartz.Dto;

namespace EasyCore.Quartz.History;

/// <summary>
/// Process-local ring buffer of recent job executions.
/// </summary>
public interface IJobExecutionHistoryStore
{
    void Record(JobExecutionHistoryDto entry);
    IReadOnlyList<JobExecutionHistoryDto> GetRecent(int take = 100);
    int SuccessCount { get; }
    int FailureCount { get; }
}

/// <inheritdoc />
public sealed class InMemoryJobExecutionHistoryStore : IJobExecutionHistoryStore
{
    private readonly LinkedList<JobExecutionHistoryDto> _entries = new();
    private readonly object _gate = new();
    private readonly int _capacity;
    private int _success;
    private int _failure;

    public InMemoryJobExecutionHistoryStore(int capacity = 200)
    {
        _capacity = Math.Max(1, capacity);
    }

    public int SuccessCount => Volatile.Read(ref _success);
    public int FailureCount => Volatile.Read(ref _failure);

    public void Record(JobExecutionHistoryDto entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_gate)
        {
            _entries.AddLast(entry);
            while (_entries.Count > _capacity)
            {
                _entries.RemoveFirst();
            }
        }

        if (entry.Success)
        {
            Interlocked.Increment(ref _success);
        }
        else
        {
            Interlocked.Increment(ref _failure);
        }
    }

    public IReadOnlyList<JobExecutionHistoryDto> GetRecent(int take = 100)
    {
        take = Math.Clamp(take, 1, _capacity);
        lock (_gate)
        {
            return _entries.Reverse().Take(take).ToList();
        }
    }
}
