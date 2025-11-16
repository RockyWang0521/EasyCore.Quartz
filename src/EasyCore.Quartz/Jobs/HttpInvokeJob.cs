using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EasyCore.Quartz.Jobs;

/// <summary>
/// Invokes an HTTP endpoint when triggered. Job data keys: Url, Method, Body, Headers.
/// </summary>
[DisallowConcurrentExecution]
public sealed class HttpInvokeJob : IJob
{
    public const string JobDataUrl = "Url";
    public const string JobDataMethod = "Method";
    public const string JobDataBody = "Body";
    public const string JobDataHeaders = "Headers";
    public const string HttpClientName = "QuartzHttpClient";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpInvokeJob> _logger;

    /// <summary>
    /// Creates a new HTTP invoke job.
    /// </summary>
    public HttpInvokeJob(IHttpClientFactory httpClientFactory, ILogger<HttpInvokeJob> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.MergedJobDataMap;
        var url = dataMap.GetString(JobDataUrl)
                  ?? throw new InvalidOperationException("HTTP job requires a Url in JobDataMap.");

        var method = (dataMap.GetString(JobDataMethod) ?? "GET").ToUpperInvariant();
        var body = dataMap.GetString(JobDataBody) ?? string.Empty;
        var headersJson = dataMap.GetString(JobDataHeaders);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (!string.IsNullOrEmpty(body) && (method is "POST" or "PUT" or "PATCH"))
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        if (!string.IsNullOrWhiteSpace(headersJson))
        {
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
            if (headers is not null)
            {
                foreach (var kv in headers)
                {
                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }
        }

        _logger.LogInformation("HTTP job {Job} calling {Method} {Url}", context.JobDetail.Key, method, url);

        using var response = await client.SendAsync(request, context.CancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(context.CancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "HTTP job {Job} failed with status {StatusCode}: {Content}",
                context.JobDetail.Key,
                (int)response.StatusCode,
                content);

            throw new HttpRequestException(
                $"HTTP job failed with status {(int)response.StatusCode} ({response.ReasonPhrase}) for {method} {url}.");
        }

        _logger.LogInformation(
            "HTTP job {Job} completed with status {StatusCode}",
            context.JobDetail.Key,
            (int)response.StatusCode);
    }
}
