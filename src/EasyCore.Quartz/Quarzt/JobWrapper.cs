using Microsoft.Extensions.Logging;
using Quartz;

namespace EasyCore.Quartz
{
    /// <summary>
    /// A generic wrapper for scheduled jobs that implements logging and error handling.
    /// 
    /// This class wraps a job of type <typeparamref name="T"/> that implements <see cref="IEasyCoreJob"/>.
    /// It logs when the job starts, completes, or fails, and delegates execution to the inner job instance.
    /// </summary>
    /// <typeparam name="T">The job type to wrap, must implement <see cref="IEasyCoreJob"/>.</typeparam>
    public class JobWrapper<T> : IJob where T : IEasyCoreJob
    {
        private readonly T _inner;
        private readonly ILogger<JobWrapper<T>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobWrapper{T}"/> class.
        /// </summary>
        /// <param name="inner">The inner job instance to execute.</param>
        /// <param name="logger">The logger used for job lifecycle logging.</param>
        public JobWrapper(T inner, ILogger<JobWrapper<T>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        /// <summary>
        /// Executes the job by delegating to the wrapped job instance, with logging and exception handling.
        /// </summary>
        /// <param name="context">The Quartz job execution context.</param>
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Starting job execution: {Job}", typeof(T).Name);

                await _inner.Execute(context);

                _logger.LogInformation("Job execution completed: {Job}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job execution failed: {Job}", typeof(T).Name);
            }
        }
    }
}
