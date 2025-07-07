using Quartz;
using System.Text;
using System.Text.Json;

namespace EasyCore.Quartz
{
    /// <summary>
    /// A Quartz job that performs an HTTP request when triggered.
    /// </summary>
    [DisallowConcurrentExecution]
    public class HttpInvokeJob : IJob
    {
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpInvokeJob"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory used to create HttpClient instances.</param>
        public HttpInvokeJob(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Executes the job, sending an HTTP request based on the provided job data.
        /// </summary>
        /// <param name="context">The job execution context.</param>
        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;

            var url = dataMap.GetString("Url");

            var method = dataMap.GetString("Method")?.ToUpperInvariant() ?? "GET";

            var body = dataMap.GetString("Body") ?? "";

            var headersJson = dataMap.GetString("Headers");

            var client = _httpClientFactory.CreateClient("QuartzHttpClient");

            var request = new HttpRequestMessage(new HttpMethod(method), url);

            if (!string.IsNullOrEmpty(body) && (method == "POST" || method == "PUT"))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            if (!string.IsNullOrWhiteSpace(headersJson))
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);

                if (headers != null)
                {
                    foreach (var kv in headers)
                    {
                        request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                    }
                }
            }

            var response = await client.SendAsync(request);
        }
    }
}
