using EasyCore.Quartz.Dto;

namespace EasyCore.Quartz.Management;

/// <summary>
/// Shared job management operations used by the REST API and Dashboard.
/// </summary>
public interface IJobManagementService
{
    Task<SchedulerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobStatusInfoDto>> GetAllJobsAsync(CancellationToken cancellationToken = default);
    Task<JobStatusInfoDto?> GetJobAsync(string jobName, string jobGroup = "DEFAULT", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobStatusInfoDto>> GetRecurringJobsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobStatusInfoDto>> GetExecutingJobsAsync(CancellationToken cancellationToken = default);
    Task<JobStatusInfoDto> PauseJobAsync(string jobName, string jobGroup = "DEFAULT", CancellationToken cancellationToken = default);
    Task<JobStatusInfoDto> ResumeJobAsync(string jobName, string jobGroup = "DEFAULT", CancellationToken cancellationToken = default);
    Task<JobStatusInfoDto> UpdateCronAsync(string jobName, string newCron, string jobGroup = "DEFAULT", CancellationToken cancellationToken = default);
    Task<JobActionResultDto> DeleteJobAsync(string jobName, string jobGroup = "DEFAULT", CancellationToken cancellationToken = default);
    Task<JobActionResultDto> TriggerJobAsync(string jobName, string jobGroup = "DEFAULT", CancellationToken cancellationToken = default);
    Task<JobActionResultDto> AddOrUpdateHttpJobAsync(HttpJobInputDto input, CancellationToken cancellationToken = default);
    IReadOnlyList<JobExecutionHistoryDto> GetHistory(int take = 100);
}
