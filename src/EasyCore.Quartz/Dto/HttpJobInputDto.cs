namespace EasyCore.Quartz
{
    /// <summary>
    /// Data Transfer Object representing an HTTP-based job to be scheduled by Quartz.
    /// </summary>
    public class HttpJobInputDto
    {
#pragma warning disable CS8618 // Disable warning for non-nullable properties not initialized in constructor

        /// <summary>
        /// The unique name of the job.
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// The group the job belongs to. Defaults to "DEFAULT".
        /// </summary>
        public string JobGroup { get; set; } = "DEFAULT";

        /// <summary>
        /// The target URL that the job will invoke.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The HTTP method used for the request (e.g., GET, POST, PUT, DELETE). Defaults to "GET".
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// The request body content (typically used for POST or PUT methods).
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Optional HTTP headers to include in the request.
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Cron expression that defines the job's schedule.
        /// </summary>
        public string Cron { get; set; }

#pragma warning restore CS8618 // Restore nullability warning
    }

}
