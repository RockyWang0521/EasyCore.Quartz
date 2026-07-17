using EasyCore.Quartz;
using EasyCore.Quartz.History;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace EasyCoreQuartz.Tests;

public class SchedulerIntegrationTests
{
    [Fact]
    public async Task Discovered_Job_Runs_And_Writes_History()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddEasyCoreQuartz(options =>
                {
                    options.AddAssemblyFrom<IntegrationSampleJob>();
                    options.HistoryCapacity = 50;
                });
            })
            .Build();

        await host.StartAsync();
        try
        {
            var schedulerFactory = host.Services.GetRequiredService<ISchedulerFactory>();
            var scheduler = await schedulerFactory.GetScheduler();
            var history = host.Services.GetRequiredService<IJobExecutionHistoryStore>();

            // Wait until discovery hosted service has scheduled and at least one fire occurs.
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < deadline)
            {
                if (history.SuccessCount > 0)
                {
                    break;
                }

                await Task.Delay(200);
            }

            Assert.True(history.SuccessCount > 0, "Expected at least one successful job execution in history.");
            Assert.Contains(history.GetRecent(20), h => h.JobName.Contains(nameof(IntegrationSampleJob), StringComparison.Ordinal));
            Assert.True(scheduler.IsStarted);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}

[EasyCoreCron("0/1 * * * * ?")]
public sealed class IntegrationSampleJob : IEasyCoreJob
{
    public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
}
