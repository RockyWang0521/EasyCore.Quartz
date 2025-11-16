using EasyCore.Quartz;
using EasyCore.Quartz.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;

namespace EasyCoreQuartz.Tests;

public sealed class ThrowingJob : IEasyCoreJob
{
    public Task Execute(IJobExecutionContext context) => throw new InvalidOperationException("boom");
}

public class JobWrapperTests
{
    [Fact]
    public async Task JobWrapper_Rethrows_Exceptions()
    {
        var wrapper = new JobWrapper<ThrowingJob>(
            new ThrowingJob(),
            NullLogger<JobWrapper<ThrowingJob>>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.Execute(null!));
    }
}
