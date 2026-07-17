using EasyCore.Quartz;
using EasyCore.Quartz.Dto;
using EasyCore.Quartz.History;
using EasyCore.Quartz.Jobs;
using EasyCore.Quartz.Management;
using Microsoft.Extensions.Options;
using Quartz;

namespace EasyCoreQuartz.Tests;

public class JobManagementServiceTests
{
    [Fact]
    public async Task Http_Method_Validation_Is_Case_Insensitive()
    {
        var (management, scheduler) = await CreateManagementAsync();
        try
        {
            var result = await management.AddOrUpdateHttpJobAsync(new HttpJobInputDto
            {
                JobName = "HttpCase",
                JobGroup = "TEST",
                Url = "https://example.com/",
                Method = "GeT",
                Cron = "0 0 0 1 1 ? 2099"
            });

            Assert.True(result.Success, result.Message);

            var invalid = await management.AddOrUpdateHttpJobAsync(new HttpJobInputDto
            {
                JobName = "HttpBad",
                Url = "https://example.com/",
                Method = "TRACE",
                Cron = "0 0 0 1 1 ? 2099"
            });

            Assert.False(invalid.Success);

            var ssrf = await management.AddOrUpdateHttpJobAsync(new HttpJobInputDto
            {
                JobName = "HttpSsrf",
                Url = "http://127.0.0.1/",
                Method = "GET",
                Cron = "0 0 0 1 1 ? 2099"
            });

            Assert.False(ssrf.Success);
            Assert.Contains("blocked", ssrf.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await scheduler.Shutdown(true);
        }
    }

    [Fact]
    public async Task Pause_And_Resume_Work()
    {
        var (management, scheduler) = await CreateManagementAsync();
        try
        {
            var job = JobBuilder.Create<NoOpJob>().WithIdentity("PauseMe", "TEST").StoreDurably().Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity("PauseMe.trigger", "TEST")
                .ForJob(job)
                .WithCronSchedule("0 0 0 1 1 ? 2099")
                .Build();

            await scheduler.ScheduleJob(job, trigger);

            var paused = await management.PauseJobAsync("PauseMe", "TEST");
            Assert.Equal(TriggerState.Paused.ToString(), paused.TriggerState);

            var resumed = await management.ResumeJobAsync("PauseMe", "TEST");
            Assert.Equal(TriggerState.Normal.ToString(), resumed.TriggerState);
        }
        finally
        {
            await scheduler.Shutdown(true);
        }
    }

    private static async Task<(IJobManagementService Management, IScheduler Scheduler)> CreateManagementAsync()
    {
        var scheduler = await SchedulerBuilder.Create()
            .UseInMemoryStore()
            .UseDefaultThreadPool()
            .BuildScheduler();

        await scheduler.AddJob(
            JobBuilder.Create<HttpInvokeJob>().WithIdentity("HttpInvokeJob", "SYSTEM").StoreDurably().Build(),
            replace: true);

        await scheduler.Start();

        var factory = new FixedSchedulerFactory(scheduler);
        var management = new JobManagementService(
            factory,
            new InMemoryJobExecutionHistoryStore(50),
            Options.Create(new EasyCoreQuartzOptions()));

        return (management, scheduler);
    }

    private sealed class FixedSchedulerFactory : ISchedulerFactory
    {
        private readonly IScheduler _scheduler;

        public FixedSchedulerFactory(IScheduler scheduler) => _scheduler = scheduler;

        public Task<IReadOnlyList<IScheduler>> GetAllSchedulers(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IScheduler>>(new[] { _scheduler });

        public Task<IScheduler> GetScheduler(CancellationToken cancellationToken = default)
            => Task.FromResult(_scheduler);

        public Task<IScheduler?> GetScheduler(string schedName, CancellationToken cancellationToken = default)
            => Task.FromResult<IScheduler?>(_scheduler);
    }

    [DisallowConcurrentExecution]
    private sealed class NoOpJob : IJob
    {
        public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
    }
}
