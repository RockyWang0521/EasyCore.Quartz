using System.Text.Json;
using EasyCore.Quartz.Dto;
using EasyCore.Quartz.History;
using EasyCore.Quartz.Jobs;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl.Matchers;

namespace EasyCore.Quartz.Management;

/// <inheritdoc />
public sealed class JobManagementService : IJobManagementService
{
    private static readonly HashSet<string> AllowedHttpMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "POST", "PUT", "DELETE", "PATCH" };

    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobExecutionHistoryStore _historyStore;
    private readonly EasyCoreQuartzOptions _options;

    public JobManagementService(
        ISchedulerFactory schedulerFactory,
        IJobExecutionHistoryStore historyStore,
        IOptions<EasyCoreQuartzOptions> options)
    {
        _schedulerFactory = schedulerFactory;
        _historyStore = historyStore;
        _options = options.Value;
    }

    public async Task<SchedulerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var meta = await scheduler.GetMetaData(cancellationToken).ConfigureAwait(false);
        var jobs = await GetAllJobsAsync(cancellationToken).ConfigureAwait(false);
        var executing = await scheduler.GetCurrentlyExecutingJobs(cancellationToken).ConfigureAwait(false);

        return new SchedulerOverviewDto
        {
            SchedulerName = meta.SchedulerName,
            SchedulerInstanceId = meta.SchedulerInstanceId,
            IsStarted = meta.Started,
            InStandbyMode = meta.InStandbyMode,
            IsShutdown = meta.Shutdown,
            JobCount = jobs.Select(j => (j.JobName, j.Group)).Distinct().Count(),
            TriggerCount = jobs.Count,
            ExecutingCount = executing.Count,
            PausedTriggerCount = jobs.Count(j => j.TriggerState == TriggerState.Paused.ToString()),
            ErrorTriggerCount = jobs.Count(j => j.TriggerState == TriggerState.Error.ToString()),
            RecentSuccessCount = _historyStore.SuccessCount,
            RecentFailureCount = _historyStore.FailureCount,
            StoreType = meta.JobStoreType.Name,
            ServerTimeUtc = DateTimeOffset.UtcNow
        };
    }

    public async Task<IReadOnlyList<JobStatusInfoDto>> GetAllJobsAsync(CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var executingKeys = await GetExecutingJobKeysAsync(scheduler, cancellationToken).ConfigureAwait(false);
        var result = new List<JobStatusInfoDto>();

        foreach (var group in await scheduler.GetJobGroupNames(cancellationToken).ConfigureAwait(false))
        {
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group), cancellationToken)
                .ConfigureAwait(false);

            foreach (var jobKey in jobKeys)
            {
                var detail = await scheduler.GetJobDetail(jobKey, cancellationToken).ConfigureAwait(false);
                var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken).ConfigureAwait(false);

                if (triggers.Count == 0)
                {
                    result.Add(MapJob(jobKey, detail, null, TriggerState.None, executingKeys));
                    continue;
                }

                foreach (var trigger in triggers)
                {
                    var state = await scheduler.GetTriggerState(trigger.Key, cancellationToken).ConfigureAwait(false);
                    result.Add(MapJob(jobKey, detail, trigger, state, executingKeys));
                }
            }
        }

        return result;
    }

    public async Task<JobStatusInfoDto?> GetJobAsync(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var jobKey = new JobKey(jobName, jobGroup);
        if (!await scheduler.CheckExists(jobKey, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var executingKeys = await GetExecutingJobKeysAsync(scheduler, cancellationToken).ConfigureAwait(false);
        var detail = await scheduler.GetJobDetail(jobKey, cancellationToken).ConfigureAwait(false);
        var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken).ConfigureAwait(false);
        var trigger = triggers.FirstOrDefault();
        var state = trigger is null
            ? TriggerState.None
            : await scheduler.GetTriggerState(trigger.Key, cancellationToken).ConfigureAwait(false);

        return MapJob(jobKey, detail, trigger, state, executingKeys, includeJobData: true);
    }

    public async Task<IReadOnlyList<JobStatusInfoDto>> GetRecurringJobsAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllJobsAsync(cancellationToken).ConfigureAwait(false);
        return all.Where(j => !string.IsNullOrWhiteSpace(j.CronExpression)).ToList();
    }

    public async Task<IReadOnlyList<JobStatusInfoDto>> GetExecutingJobsAsync(CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var executing = await scheduler.GetCurrentlyExecutingJobs(cancellationToken).ConfigureAwait(false);
        var result = new List<JobStatusInfoDto>();

        foreach (var ctx in executing)
        {
            var state = await scheduler.GetTriggerState(ctx.Trigger.Key, cancellationToken).ConfigureAwait(false);
            result.Add(MapJob(
                ctx.JobDetail.Key,
                ctx.JobDetail,
                ctx.Trigger,
                state,
                new HashSet<JobKey> { ctx.JobDetail.Key },
                includeJobData: true));
        }

        return result;
    }

    public async Task<JobStatusInfoDto> PauseJobAsync(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var jobKey = await RequireJobAsync(scheduler, jobName, jobGroup, cancellationToken).ConfigureAwait(false);
        await scheduler.PauseJob(jobKey, cancellationToken).ConfigureAwait(false);
        return (await GetJobAsync(jobName, jobGroup, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<JobStatusInfoDto> ResumeJobAsync(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var jobKey = await RequireJobAsync(scheduler, jobName, jobGroup, cancellationToken).ConfigureAwait(false);
        await scheduler.ResumeJob(jobKey, cancellationToken).ConfigureAwait(false);
        return (await GetJobAsync(jobName, jobGroup, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<JobStatusInfoDto> UpdateCronAsync(
        string jobName,
        string newCron,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newCron) || !CronExpression.IsValidExpression(newCron))
        {
            throw new ArgumentException("Invalid cron expression.", nameof(newCron));
        }

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var jobKey = await RequireJobAsync(scheduler, jobName, jobGroup, cancellationToken).ConfigureAwait(false);
        var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken).ConfigureAwait(false);
        var oldTrigger = triggers.FirstOrDefault()
                         ?? throw new InvalidOperationException($"No triggers found for job '{jobName}'.");

        if (oldTrigger is not ICronTrigger)
        {
            throw new InvalidOperationException("The trigger associated with this job is not a cron trigger.");
        }

        var newTrigger = TriggerBuilder.Create()
            .WithIdentity(oldTrigger.Key)
            .ForJob(jobKey)
            .WithCronSchedule(newCron)
            .StartNow()
            .Build();

        var result = await scheduler.RescheduleJob(oldTrigger.Key, newTrigger, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            throw new InvalidOperationException($"Failed to reschedule job '{jobName}'.");
        }

        return (await GetJobAsync(jobName, jobGroup, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<JobActionResultDto> DeleteJobAsync(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var jobKey = await RequireJobAsync(scheduler, jobName, jobGroup, cancellationToken).ConfigureAwait(false);
        var deleted = await scheduler.DeleteJob(jobKey, cancellationToken).ConfigureAwait(false);

        return new JobActionResultDto
        {
            JobName = jobName,
            Group = jobGroup,
            Success = deleted,
            Message = deleted
                ? $"Job '{jobName}' in group '{jobGroup}' has been deleted."
                : $"Failed to delete job '{jobName}' in group '{jobGroup}'."
        };
    }

    public async Task<JobActionResultDto> TriggerJobAsync(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var jobKey = await RequireJobAsync(scheduler, jobName, jobGroup, cancellationToken).ConfigureAwait(false);
        await scheduler.TriggerJob(jobKey, cancellationToken).ConfigureAwait(false);

        return new JobActionResultDto
        {
            JobName = jobName,
            Group = jobGroup,
            Success = true,
            Message = $"Job '{jobName}' in group '{jobGroup}' has been triggered."
        };
    }

    public async Task<JobActionResultDto> AddOrUpdateHttpJobAsync(
        HttpJobInputDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.JobName)
            || string.IsNullOrWhiteSpace(input.Url)
            || string.IsNullOrWhiteSpace(input.Cron))
        {
            return Fail(input, "JobName, Url and Cron are required.");
        }

        if (!CronExpression.IsValidExpression(input.Cron))
        {
            return Fail(input, "Invalid cron expression.");
        }

        var method = (input.Method ?? "GET").Trim().ToUpperInvariant();
        if (!AllowedHttpMethods.Contains(method))
        {
            return Fail(input, "Invalid HTTP method. Allowed: GET, POST, PUT, DELETE, PATCH.");
        }

        var urlError = HttpJobUrlValidator.Validate(input.Url, _options);
        if (urlError is not null)
        {
            return Fail(input, urlError);
        }

        if ((method is "POST" or "PUT" or "PATCH") && !string.IsNullOrWhiteSpace(input.Body))
        {
            try
            {
                JsonDocument.Parse(input.Body);
            }
            catch (JsonException ex)
            {
                return Fail(input, $"Invalid JSON body: {ex.Message}");
            }
        }

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        var jobKey = new JobKey(input.JobName, input.JobGroup);
        var triggerKey = new TriggerKey($"{input.JobName}.trigger", input.JobGroup);

        var jobDetail = JobBuilder.Create<HttpInvokeJob>()
            .WithIdentity(jobKey)
            .WithDescription(input.Description ?? $"HTTP {method} {input.Url}")
            .UsingJobData(HttpInvokeJob.JobDataUrl, input.Url)
            .UsingJobData(HttpInvokeJob.JobDataMethod, method)
            .UsingJobData(HttpInvokeJob.JobDataBody, input.Body ?? string.Empty)
            .UsingJobData(HttpInvokeJob.JobDataHeaders, JsonSerializer.Serialize(input.Headers ?? new Dictionary<string, string>()))
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .WithCronSchedule(input.Cron)
            .ForJob(jobKey)
            .Build();

        if (await scheduler.CheckExists(jobKey, cancellationToken).ConfigureAwait(false))
        {
            await scheduler.AddJob(jobDetail, replace: true, storeNonDurableWhileAwaitingScheduling: true, cancellationToken)
                .ConfigureAwait(false);

            if (await scheduler.CheckExists(triggerKey, cancellationToken).ConfigureAwait(false))
            {
                await scheduler.RescheduleJob(triggerKey, trigger, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await scheduler.ScheduleJob(trigger, cancellationToken).ConfigureAwait(false);
            }

            return new JobActionResultDto
            {
                JobName = input.JobName,
                Group = input.JobGroup,
                Success = true,
                Message = $"HTTP job '{input.JobName}' updated."
            };
        }

        await scheduler.ScheduleJob(jobDetail, trigger, cancellationToken).ConfigureAwait(false);

        return new JobActionResultDto
        {
            JobName = input.JobName,
            Group = input.JobGroup,
            Success = true,
            Message = $"HTTP job '{input.JobName}' created."
        };
    }

    public IReadOnlyList<JobExecutionHistoryDto> GetHistory(int take = 100) => _historyStore.GetRecent(take);

    private JobStatusInfoDto MapJob(
        JobKey jobKey,
        IJobDetail? detail,
        ITrigger? trigger,
        TriggerState state,
        ISet<JobKey> executingKeys,
        bool includeJobData = false)
    {
        string? cron = null;
        if (trigger is ICronTrigger cronTrigger)
        {
            cron = cronTrigger.CronExpressionString;
        }

        Dictionary<string, string>? data = null;
        if (includeJobData && detail is not null)
        {
            data = detail.JobDataMap
                .Keys
                .ToDictionary(k => k, k => detail.JobDataMap.GetString(k) ?? detail.JobDataMap.Get(k)?.ToString() ?? string.Empty);
        }

        return new JobStatusInfoDto
        {
            JobName = jobKey.Name,
            Group = jobKey.Group,
            TriggerKey = trigger?.Key.ToString() ?? string.Empty,
            TriggerState = state.ToString(),
            CronExpression = cron,
            NextFireTime = ToDisplayOffset(trigger?.GetNextFireTimeUtc()),
            PreviousFireTime = ToDisplayOffset(trigger?.GetPreviousFireTimeUtc()),
            IsCurrentlyExecuting = executingKeys.Contains(jobKey),
            JobType = detail?.JobType.FullName,
            Description = detail?.Description,
            JobData = data
        };
    }

    private DateTimeOffset? ToDisplayOffset(DateTimeOffset? utc)
    {
        if (!utc.HasValue)
        {
            return null;
        }

        return utc.Value.ToOffset(TimeSpan.FromHours(_options.TimeZoneOffsetHours));
    }

    private static async Task<JobKey> RequireJobAsync(
        IScheduler scheduler,
        string jobName,
        string jobGroup,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            throw new ArgumentException("JobName is required.", nameof(jobName));
        }

        var jobKey = new JobKey(jobName, jobGroup);
        if (!await scheduler.CheckExists(jobKey, cancellationToken).ConfigureAwait(false))
        {
            throw new KeyNotFoundException($"Job '{jobName}' in group '{jobGroup}' does not exist.");
        }

        return jobKey;
    }

    private static async Task<HashSet<JobKey>> GetExecutingJobKeysAsync(
        IScheduler scheduler,
        CancellationToken cancellationToken)
    {
        var executing = await scheduler.GetCurrentlyExecutingJobs(cancellationToken).ConfigureAwait(false);
        return executing.Select(e => e.JobDetail.Key).ToHashSet();
    }

    private static JobActionResultDto Fail(HttpJobInputDto input, string message) => new()
    {
        JobName = input.JobName,
        Group = input.JobGroup,
        Success = false,
        Message = message
    };
}
