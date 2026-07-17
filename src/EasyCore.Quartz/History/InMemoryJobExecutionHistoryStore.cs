using EasyCore.Quartz.Dto;

namespace EasyCore.Quartz.History;

/// <summary>
/// Process-local ring buffer of recent job executions.
/// </summary>
public interface IJobExecutionHistoryStore
{
    void Record(JobExecutionHistoryDto entry);
    IReadOnlyList<JobExecutionHistoryDto> GetRecent(int take = 100);

    /// <summary>Success count within the current in-memory window (not lifetime).</summary>
    int SuccessCount { get; }

    /// <summary>Failure count within the current in-memory window (not lifetime).</summary>
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

    public int SuccessCount
    {
        get { lock (_gate) return _success; }
    }

    public int FailureCount
    {
        get { lock (_gate) return _failure; }
    }

    public void Record(JobExecutionHistoryDto entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_gate)
        {
            _entries.AddLast(entry);
            if (entry.Success)
            {
                _success++;
            }
            else
            {
                _failure++;
            }

            while (_entries.Count > _capacity)
            {
                var removed = _entries.First!.Value;
                _entries.RemoveFirst();
                if (removed.Success)
                {
                    _success--;
                }
                else
                {
                    _failure--;
                }
            }
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
