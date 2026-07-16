using System.Reflection;
using EasyCore.Quartz.Discovery;
using EasyCore.Quartz.History;
using EasyCore.Quartz.Jobs;
using EasyCore.Quartz.Management;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace EasyCore.Quartz;

/// <summary>
/// DI registration entry points for EasyCore.Quartz.
/// </summary>
public static class EasyCoreQuartzExtensions
{
    /// <summary>
    /// Registers Quartz with EasyCore job discovery, management services, and history.
    /// </summary>
    public static IServiceCollection EasyCoreQuartz(
        this IServiceCollection services,
        Action<EasyCoreQuartzOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();

        var options = new EasyCoreQuartzOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<IOptions<EasyCoreQuartzOptions>>(Options.Create(options));

        var jobTypes = JobTypeDiscovery.Discover(options, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        var jobsToSchedule = new List<(IJobDetail Job, ITrigger Trigger)>();
        var discoveredCount = jobTypes.Count;

        foreach (var jobType in jobTypes)
        {
            var cronAttr = jobType.GetCustomAttribute<EasyCoreCronAttribute>()!;
            var group = cronAttr.JobGroup ?? "DEFAULT";
            var jobKeyName = cronAttr.JobKey ?? jobType.FullName!;
            var jobKey = new JobKey(jobKeyName, group);
            var triggerKey = new TriggerKey($"{jobKeyName}.trigger", group);
            var wrapperType = typeof(JobWrapper<>).MakeGenericType(jobType);

            var jobBuilder = JobBuilder.Create(wrapperType)
                .WithIdentity(jobKey)
                .StoreDurably()
                .RequestRecovery(cronAttr.RequestRecovery)
                .WithDescription(jobType.FullName);

            if (jobType.GetCustomAttribute<EasyCoreDisallowConcurrentExecutionAttribute>() is not null)
            {
                jobBuilder.DisallowConcurrentExecution();
            }

            var jobDetail = jobBuilder.Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .ForJob(jobDetail)
                .WithCronSchedule(cronAttr.CronExpression, cron =>
                {
                    switch (cronAttr.MisfirePolicy)
                    {
                        case MisfirePolicyType.Ignore:
                            cron.WithMisfireHandlingInstructionIgnoreMisfires();
                            break;
                        case MisfirePolicyType.FireNow:
                            cron.WithMisfireHandlingInstructionFireAndProceed();
                            break;
                        case MisfirePolicyType.DoNothing:
                            cron.WithMisfireHandlingInstructionDoNothing();
                            break;
                    }
                })
                .StartNow()
                .Build();

            jobsToSchedule.Add((jobDetail, trigger));
            services.AddTransient(jobType);
            services.AddTransient(wrapperType);
        }

        var maxConcurrency = options.MaxConcurrency > 0
            ? options.MaxConcurrency
            : Math.Max(10, discoveredCount);

        services.AddSingleton<IJobExecutionHistoryStore>(
            _ => new InMemoryJobExecutionHistoryStore(options.HistoryCapacity));
        services.AddSingleton<JobExecutionHistoryListener>();
        services.AddSingleton<IJobManagementService, JobManagementService>();
        services.TryAddSingleton<IEnumerable<(IJobDetail, ITrigger)>>(jobsToSchedule);

        // Schema must run before the Quartz hosted service starts the scheduler.
        services.AddHostedService<Hosting.SchemaBootstrapHostedService>();
        services.AddHostedService<Hosting.QuartzJobSchedulerHostedService>();

        var quartzSettings = options.GetSettings();

        services.AddQuartz(q =>
        {
            q.SchedulerId = "AUTO";
            q.SchedulerName = Assembly.GetEntryAssembly()?.GetName().Name ?? "EasyCoreQuartzScheduler";

            if (quartzSettings.SqlType != SqlType.None && !string.IsNullOrWhiteSpace(quartzSettings.ConnectionString))
            {
                q.UsePersistentStore(s =>
                {
                    s.UseProperties = false;
                    s.RetryInterval = TimeSpan.FromSeconds(15);

                    switch (quartzSettings.SqlType)
                    {
                        case SqlType.MySql:
                            s.UseMySql(mysql =>
                            {
                                mysql.ConnectionString = quartzSettings.ConnectionString!;
                                mysql.TablePrefix = options.TablePrefix;
                            });
                            break;
                        case SqlType.SqlServer:
                            s.UseSqlServer(sql =>
                            {
                                sql.ConnectionString = quartzSettings.ConnectionString!;
                                sql.TablePrefix = options.TablePrefix;
                            });
                            break;
                        case SqlType.PostgreSql:
                            s.UsePostgres(pg =>
                            {
                                pg.ConnectionString = quartzSettings.ConnectionString!;
                                pg.TablePrefix = options.TablePrefix;
                            });
                            break;
                        case SqlType.Oracle:
                            s.UseOracle(ora =>
                            {
                                ora.ConnectionString = quartzSettings.ConnectionString!;
                                ora.TablePrefix = options.TablePrefix;
                            });
                            break;
                    }

                    s.UseNewtonsoftJsonSerializer();
                    s.UseClustering(c =>
                    {
                        c.CheckinMisfireThreshold = TimeSpan.FromSeconds(options.CheckinMisfireThreshold);
                        c.CheckinInterval = TimeSpan.FromSeconds(options.CheckinInterval);
                    });
                });
            }

            q.AddJobListener<JobExecutionHistoryListener>();
            q.AddJob<HttpInvokeJob>(opts => opts.StoreDurably());
            q.UseDefaultThreadPool(tp => tp.MaxConcurrency = maxConcurrency);
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        services.AddHttpClient(HttpInvokeJob.HttpClientName);
        services.AddControllers().AddApplicationPart(typeof(EasyCoreQuartzExtensions).Assembly);

        options.ApplyServiceConfigurators(services);

        return services;
    }
}
