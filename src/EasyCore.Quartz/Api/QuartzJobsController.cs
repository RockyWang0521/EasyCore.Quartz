using EasyCore.Quartz.Dto;
using EasyCore.Quartz.Management;
using Microsoft.AspNetCore.Mvc;

namespace EasyCore.Quartz.Api;

/// <summary>
/// REST API for job management. Prefer the Dashboard for interactive use.
/// </summary>
[Route("api/quartz")]
[ApiController]
public sealed class QuartzJobsController : ControllerBase
{
    private readonly IJobManagementService _jobs;

    public QuartzJobsController(IJobManagementService jobs)
    {
        _jobs = jobs;
    }

    [HttpGet("overview")]
    public Task<SchedulerOverviewDto> Overview(CancellationToken cancellationToken)
        => _jobs.GetOverviewAsync(cancellationToken);

    [HttpGet("jobs")]
    public Task<IReadOnlyList<JobStatusInfoDto>> GetAll(CancellationToken cancellationToken)
        => _jobs.GetAllJobsAsync(cancellationToken);

    [HttpGet("jobs/{jobGroup}/{jobName}")]
    public async Task<ActionResult<JobStatusInfoDto>> Get(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        var job = await _jobs.GetJobAsync(jobName, jobGroup, cancellationToken).ConfigureAwait(false);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("recurring")]
    public Task<IReadOnlyList<JobStatusInfoDto>> Recurring(CancellationToken cancellationToken)
        => _jobs.GetRecurringJobsAsync(cancellationToken);

    [HttpGet("executing")]
    public Task<IReadOnlyList<JobStatusInfoDto>> Executing(CancellationToken cancellationToken)
        => _jobs.GetExecutingJobsAsync(cancellationToken);

    [HttpGet("history")]
    public ActionResult<IReadOnlyList<JobExecutionHistoryDto>> History([FromQuery] int take = 100)
        => Ok(_jobs.GetHistory(take));

    [HttpPut("jobs/{jobGroup}/{jobName}/pause")]
    public async Task<ActionResult<JobStatusInfoDto>> Pause(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _jobs.PauseJobAsync(jobName, jobGroup, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("jobs/{jobGroup}/{jobName}/resume")]
    public async Task<ActionResult<JobStatusInfoDto>> Resume(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _jobs.ResumeJobAsync(jobName, jobGroup, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("jobs/{jobGroup}/{jobName}/cron")]
    public async Task<ActionResult<JobStatusInfoDto>> UpdateCron(
        string jobName,
        [FromQuery] string cron,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _jobs.UpdateCronAsync(jobName, cron, jobGroup, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("jobs/{jobGroup}/{jobName}")]
    public async Task<ActionResult<JobActionResultDto>> Delete(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _jobs.DeleteJobAsync(jobName, jobGroup, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("jobs/{jobGroup}/{jobName}/trigger")]
    public async Task<ActionResult<JobActionResultDto>> Trigger(
        string jobName,
        string jobGroup = "DEFAULT",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _jobs.TriggerJobAsync(jobName, jobGroup, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("http-jobs")]
    public async Task<ActionResult<JobActionResultDto>> AddOrUpdateHttpJob(
        [FromBody] HttpJobInputDto input,
        CancellationToken cancellationToken = default)
    {
        var result = await _jobs.AddOrUpdateHttpJobAsync(input, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
