using EasyCore.Quartz.Dto;
using EasyCore.Quartz.History;

namespace EasyCoreQuartz.Tests;

public class HistoryStoreTests
{
    [Fact]
    public void History_Store_Keeps_Recent_Entries_And_Counts()
    {
        var store = new InMemoryJobExecutionHistoryStore(5);

        for (var i = 0; i < 8; i++)
        {
            store.Record(new JobExecutionHistoryDto
            {
                JobName = $"Job{i}",
                FiredAtUtc = DateTimeOffset.UtcNow,
                Success = i % 2 == 0,
                FireInstanceId = i.ToString()
            });
        }

        var recent = store.GetRecent(10);
        Assert.Equal(5, recent.Count);
        Assert.Equal(4, store.SuccessCount);
        Assert.Equal(4, store.FailureCount);
    }
}
