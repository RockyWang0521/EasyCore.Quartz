using System.Net;
using EasyCore.Quartz;
using EasyCore.Quartz.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quartz;

namespace EasyCoreQuartz.Tests;

public class HttpInvokeJobTests
{
    [Fact]
    public async Task Execute_Succeeds_On_2xx()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var job = CreateJob(handler, blockPrivate: false);
        var context = CreateContext("https://example.com/ok", "GET");

        await job.Execute(context);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Execute_Throws_On_Non_2xx()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("fail")
        });
        var job = CreateJob(handler, blockPrivate: false);
        var context = CreateContext("https://example.com/fail", "GET");

        await Assert.ThrowsAsync<HttpRequestException>(() => job.Execute(context));
    }

    [Fact]
    public async Task Execute_Rejects_Private_Url_Before_Send()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var job = CreateJob(handler, blockPrivate: true);
        var context = CreateContext("http://127.0.0.1/secret", "GET");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(context));
        Assert.Contains("blocked", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.CallCount);
    }

    private static HttpInvokeJob CreateJob(StubHandler handler, bool blockPrivate)
    {
        var options = new EasyCoreQuartzOptions { HttpJobBlockPrivateNetworks = blockPrivate };
        return new HttpInvokeJob(
            new StubHttpClientFactory(handler),
            Options.Create(options),
            NullLogger<HttpInvokeJob>.Instance);
    }

    private static IJobExecutionContext CreateContext(string url, string method)
    {
        var data = new JobDataMap
        {
            { HttpInvokeJob.JobDataUrl, url },
            { HttpInvokeJob.JobDataMethod, method }
        };

        var detail = JobBuilder.Create<HttpInvokeJob>()
            .WithIdentity("HttpTest", "TEST")
            .UsingJobData(data)
            .Build();

        return new MinimalJobContext(detail, data);
    }

    private sealed class MinimalJobContext : IJobExecutionContext
    {
        public MinimalJobContext(IJobDetail jobDetail, JobDataMap merged)
        {
            JobDetail = jobDetail;
            MergedJobDataMap = merged;
        }

        public IScheduler Scheduler => throw new NotSupportedException();
        public ITrigger Trigger => throw new NotSupportedException();
        public ICalendar? Calendar => null;
        public bool Recovering => false;
        public TriggerKey? RecoveringTriggerKey => null;
        public int RefireCount => 0;
        public JobDataMap MergedJobDataMap { get; }
        public IJobDetail JobDetail { get; }
        public IJob JobInstance => throw new NotSupportedException();
        public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow;
        public DateTimeOffset? ScheduledFireTimeUtc => DateTimeOffset.UtcNow;
        public DateTimeOffset? PreviousFireTimeUtc => null;
        public DateTimeOffset? NextFireTimeUtc => null;
        public string FireInstanceId => "test";
        public object? Result { get; set; }
        public TimeSpan JobRunTime => TimeSpan.Zero;
        public CancellationToken CancellationToken => CancellationToken.None;
        public void Put(object key, object objectValue) { }
        public object? Get(object key) => null;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => _responder = responder;

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_responder(request));
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }
}
