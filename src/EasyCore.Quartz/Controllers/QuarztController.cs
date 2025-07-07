using EasyCore.Quartz.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl.Matchers;
using System.Text.Json;

namespace EasyCore.Quartz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuarztController : ControllerBase
    {
        private readonly ISchedulerFactory _schedulerFactory;

        private readonly int _timeZoneOffsetHours;

        public QuarztController(
            ISchedulerFactory schedulerFactory,
            IOptions<QuarztOptions> options)
        {
            _schedulerFactory = schedulerFactory;

            _timeZoneOffsetHours = options.Value.TimeZoneOffsetHours;
        }

        [HttpGet("get/all/jobs")]
        public async Task<List<JobStatusInfoDto>> GetAllJobsWithStatusAsync()
        {
            var result = new List<JobStatusInfoDto>();

            var scheduler = await _schedulerFactory.GetScheduler();

            var jobGroupNames = await scheduler.GetJobGroupNames();

            foreach (var group in jobGroupNames)
            {
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group));

                foreach (var jobKey in jobKeys)
                {
                    var triggers = await scheduler.GetTriggersOfJob(jobKey);

                    foreach (var trigger in triggers)
                    {
                        var triggerState = await scheduler.GetTriggerState(trigger.Key);

                        bool isActive = triggerState == TriggerState.Normal;

                        string? cron = null;

                        if (trigger is ICronTrigger cronTrigger)
                        {
                            cron = cronTrigger.CronExpressionString;
                        }

                        var nextFireTimeUtc = trigger.GetNextFireTimeUtc();

                        DateTimeOffset? nextFireTimeLocal = null;

                        if (nextFireTimeUtc.HasValue)
                        {
                            nextFireTimeLocal = nextFireTimeUtc.Value.ToOffset(TimeSpan.FromHours(_timeZoneOffsetHours));
                        }

                        result.Add(new JobStatusInfoDto
                        {
                            JobName = jobKey.Name,

                            Group = jobKey.Group,

                            TriggerKey = trigger.Key.ToString(),

                            TriggerState = triggerState.ToString(),

                            IsRunning = isActive,

                            CronExpression = cron,

                            NextFireTime = nextFireTimeLocal
                        });
                    }
                }
            }

            return result;
        }

        [HttpPut("pause/job")]
        public async Task<ActionResult<JobStatusInfoDto>> PauseJobAsync([FromQuery] string jobName, [FromQuery] string jobGroup = "DEFAULT")
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                return BadRequest("JobName is required.");
            }

            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey(jobName, jobGroup);

            bool exists = await scheduler.CheckExists(jobKey);

            if (!exists)
            {
                return NotFound($"Job '{jobName}' in group '{jobGroup}' does not exist.");
            }

            await scheduler.PauseJob(jobKey);

            var triggers = await scheduler.GetTriggersOfJob(jobKey);

            var firstTrigger = triggers.FirstOrDefault();

            TriggerState triggerState = TriggerState.None;

            if (firstTrigger != null)
            {
                triggerState = await scheduler.GetTriggerState(firstTrigger.Key);
            }

            string? cron = null;

            if (firstTrigger is ICronTrigger cronTrigger)
            {
                cron = cronTrigger.CronExpressionString;
            }

            var resultDto = new JobStatusInfoDto
            {
                JobName = jobKey.Name,

                Group = jobKey.Group,

                TriggerKey = firstTrigger?.Key.ToString() ?? "N/A",

                TriggerState = triggerState.ToString(),

                CronExpression = cron,

                IsRunning = triggerState == TriggerState.Normal
            };

            return Ok(resultDto);
        }

        [HttpPut("resume/job")]
        public async Task<ActionResult<JobStatusInfoDto>> ResumeJobAsync([FromQuery] string jobName, [FromQuery] string jobGroup = "DEFAULT")
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                return BadRequest("JobName is required.");
            }

            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey(jobName, jobGroup);

            bool exists = await scheduler.CheckExists(jobKey);

            if (!exists)
            {
                return NotFound($"Job '{jobName}' in group '{jobGroup}' does not exist.");
            }

            await scheduler.ResumeJob(jobKey);

            var triggers = await scheduler.GetTriggersOfJob(jobKey);

            var firstTrigger = triggers.FirstOrDefault();

            TriggerState triggerState = TriggerState.None;

            if (firstTrigger != null)
            {
                triggerState = await scheduler.GetTriggerState(firstTrigger.Key);
            }

            string? cron = null;

            if (firstTrigger is ICronTrigger cronTrigger)
            {
                cron = cronTrigger.CronExpressionString;
            }

            var nextFireTimeUtc = firstTrigger!.GetNextFireTimeUtc();

            DateTimeOffset? nextFireTimeLocal = null;

            if (nextFireTimeUtc.HasValue)
            {
                nextFireTimeLocal = nextFireTimeUtc.Value.ToOffset(TimeSpan.FromHours(_timeZoneOffsetHours));
            }

            var resultDto = new JobStatusInfoDto
            {
                JobName = jobKey.Name,

                Group = jobKey.Group,

                TriggerKey = firstTrigger?.Key.ToString() ?? "N/A",

                TriggerState = triggerState.ToString(),

                CronExpression = cron,

                NextFireTime = nextFireTimeLocal,

                IsRunning = triggerState == TriggerState.Normal
            };

            return Ok(resultDto);
        }

        [HttpPut("update/cron")]
        public async Task<ActionResult<JobStatusInfoDto>> UpdateJobCronAsync([FromQuery] string jobName, [FromQuery] string newCron = "", [FromQuery] string jobGroup = "DEFAULT")
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                return BadRequest("JobName is required.");
            }
            if (string.IsNullOrWhiteSpace(newCron))
            {
                return BadRequest("NewCron expression is required.");
            }

            var isValidCron = CronExpression.IsValidExpression(newCron);

            if (!isValidCron)
            {
                return BadRequest("Invalid Cron expression.");
            }

            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey(jobName, jobGroup);

            bool exists = await scheduler.CheckExists(jobKey);
            if (!exists)
            {
                return NotFound($"Job '{jobName}' in group '{jobGroup}' does not exist.");
            }

            var triggers = await scheduler.GetTriggersOfJob(jobKey);

            if (triggers == null || !triggers.Any())
            {
                return NotFound($"No triggers found for job '{jobName}' in group '{jobGroup}'.");
            }

            var oldTrigger = triggers.First();

            if (oldTrigger is ICronTrigger)
            {
                var triggerKey = oldTrigger.Key;

                var newTrigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .ForJob(jobKey)
                    .WithCronSchedule(newCron)
                    .StartNow()
                    .Build();

                var rescheduleResult = await scheduler.RescheduleJob(triggerKey, newTrigger);

                if (rescheduleResult == null)
                {
                    return StatusCode(500, $"Failed to reschedule job '{jobName}'.");
                }

                var triggerState = await scheduler.GetTriggerState(newTrigger.Key);

                return Ok(new JobStatusInfoDto
                {
                    JobName = jobName,

                    Group = jobGroup,

                    TriggerKey = newTrigger.Key.ToString(),

                    TriggerState = triggerState.ToString(),

                    CronExpression = newCron,

                    NextFireTime = newTrigger.GetNextFireTimeUtc()?.ToLocalTime(),

                    IsRunning = triggerState == TriggerState.Normal
                });
            }
            else
            {
                return BadRequest("The trigger associated with this job is not a Cron trigger.");
            }
        }

        [HttpDelete("delete/job")]
        public async Task<ActionResult<JobActionResultDto>> DeleteJobAsync([FromQuery] string jobName, [FromQuery] string jobGroup = "DEFAULT")
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                return BadRequest(new JobActionResultDto
                {
                    JobName = jobName,
                    Group = jobGroup,
                    Success = false,
                    Message = "JobName is required."
                });
            }

            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey(jobName, jobGroup);

            bool exists = await scheduler.CheckExists(jobKey);

            if (!exists)
            {
                return NotFound(new JobActionResultDto
                {
                    JobName = jobName,
                    Group = jobGroup,
                    Success = false,
                    Message = $"Job '{jobName}' in group '{jobGroup}' does not exist."
                });
            }

            bool deleted = await scheduler.DeleteJob(jobKey);

            if (deleted)
            {
                return Ok(new JobActionResultDto
                {
                    JobName = jobName,
                    Group = jobGroup,
                    Success = true,
                    Message = $"Job '{jobName}' in group '{jobGroup}' has been deleted."
                });
            }
            else
            {
                return StatusCode(500, new JobActionResultDto
                {
                    JobName = jobName,
                    Group = jobGroup,
                    Success = false,
                    Message = $"Failed to delete job '{jobName}' in group '{jobGroup}'."
                });
            }
        }


        [HttpPost("manualtrigger/job")]
        public async Task<ActionResult<JobActionResultDto>> ManualTriggerJob([FromQuery] string jobName, [FromQuery] string jobGroup = "DEFAULT")
        {
            var jobKey = new JobKey(jobName, jobGroup);

            var scheduler = await _schedulerFactory.GetScheduler();

            if (!await scheduler.CheckExists(jobKey))
            {
                return NotFound(new JobActionResultDto
                {
                    JobName = jobName,

                    Group = jobGroup,

                    Success = false,

                    Message = $"Job '{jobName}' in group '{jobGroup}' does not exist."
                });
            }

            await scheduler.TriggerJob(jobKey);

            return Ok(new JobActionResultDto
            {
                JobName = jobName,

                Group = jobGroup,

                Success = true,

                Message = $"Job '{jobName}' in group '{jobGroup}' has been triggered."
            });
        }

        [HttpPost("addorupdate/httpjob")]
        public async Task<ActionResult<JobActionResultDto>> AddOrUpdateHttpJob([FromBody] HttpJobInputDto input)
        {
            if (string.IsNullOrWhiteSpace(input.JobName) || string.IsNullOrWhiteSpace(input.Url) || string.IsNullOrWhiteSpace(input.Cron))
            {
                return BadRequest(new JobActionResultDto
                {
                    JobName = input.JobName,
                    Group = input.JobGroup,
                    Success = false,
                    Message = "JobName, Url and Cron are required."
                });
            }

            var isValidCron = CronExpression.IsValidExpression(input.Cron);

            if (!isValidCron)
            {
                return BadRequest(new JobActionResultDto
                {
                    JobName = input.JobName,
                    Group = input.JobGroup,
                    Success = false,
                    Message = "Invalid Cron expression."
                });
            }

            var methodLower = input.Method.ToLower();

            if (input.Method != "get" && input.Method != "post" && input.Method != "put" && input.Method != "delete")
            {
                return BadRequest("Invalid Http method.");
            }

            if ((methodLower == "post" || methodLower == "put") && !string.IsNullOrWhiteSpace(input.Body))
            {
                try
                {
                    JsonDocument.Parse(input.Body);
                }
                catch (JsonException ex)
                {
                    return BadRequest(new JobActionResultDto
                    {
                        JobName = input.JobName,

                        Group = input.JobGroup,

                        Success = false,

                        Message = $"Invalid JSON body: {ex.Message}"
                    });
                }
            }

            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey(input.JobName, input.JobGroup);

            var triggerKey = new TriggerKey($"{input.JobName}.trigger", input.JobGroup);

            var jobDetail = JobBuilder.Create<HttpInvokeJob>()
                .WithIdentity(jobKey)
                .UsingJobData("Url", input.Url)
                .UsingJobData("Method", methodLower)
                .UsingJobData("Body", input.Body ?? "")
                .UsingJobData("Headers", JsonSerializer.Serialize(input.Headers ?? new()))
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .WithCronSchedule(input.Cron)
                .ForJob(jobKey)
                .Build();

            bool jobExists = await scheduler.CheckExists(jobKey);

            if (jobExists)
            {
                await scheduler.AddJob(jobDetail, true);

                var existingTrigger = await scheduler.GetTrigger(triggerKey);
                if (existingTrigger != null)
                {
                    await scheduler.RescheduleJob(triggerKey, trigger);
                }
                else
                {
                    await scheduler.ScheduleJob(trigger);
                }

                return Ok(new JobActionResultDto
                {
                    JobName = input.JobName,
                    Group = input.JobGroup,
                    Success = true,
                    Message = $"Job '{input.JobName}' updated."
                });
            }
            else
            {
                await scheduler.ScheduleJob(jobDetail, trigger);

                return Ok(new JobActionResultDto
                {
                    JobName = input.JobName,
                    Group = input.JobGroup,
                    Success = true,
                    Message = $"Job '{input.JobName}' created."
                });
            }
        }
    }
}
