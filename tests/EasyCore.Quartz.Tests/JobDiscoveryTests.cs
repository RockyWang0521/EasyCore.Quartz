using EasyCore.Quartz;
using EasyCore.Quartz.Discovery;
using Quartz;

namespace EasyCoreQuartz.Tests;

[EasyCoreCron("0/5 * * * * ?")]
public sealed class DiscoverableJob : IEasyCoreJob
{
    public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
}

[EasyCoreDisableJob]
[EasyCoreCron("0/5 * * * * ?")]
public sealed class DisabledDiscoverableJob : IEasyCoreJob
{
    public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
}

public class JobDiscoveryTests
{
    [Fact]
    public void Discover_Finds_Cron_Jobs_And_Skips_Disabled()
    {
        var options = new EasyCoreQuartzOptions().AddAssemblyFrom<DiscoverableJob>();
        var types = JobTypeDiscovery.Discover(options);

        Assert.Contains(types, t => t == typeof(DiscoverableJob));
        Assert.DoesNotContain(types, t => t == typeof(DisabledDiscoverableJob));
    }
}
