using EasyCore.Quartz.Quarzt;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Options;
using Quartz;
using System.Reflection;

namespace EasyCore.Quartz
{
    /// <summary>
    /// Extension methods for registering and configuring Quartz jobs with EasyCore.
    /// Automatically scans assemblies for jobs implementing IEasyCoreJob with Cron attributes,
    /// and registers them into the Quartz scheduler.
    /// </summary>
    public static class EasyCoreQuartzExtensions
    {
        /// <summary>
        /// Adds and configures Quartz with EasyCore integration.
        /// Automatically discovers jobs with the <see cref="EasyCoreCronAttribute"/> 
        /// and registers them with triggers based on their cron expressions.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        /// <param name="configureOptions">An optional delegate to configure Quartz options such as database connection and cluster settings.</param>
        public static void EasyCoreQuartz(this IServiceCollection services, Action<QuarztOptions>? configureOptions = null)
        {
            services.AddOptions();

            var options = new QuarztOptions();

            if (configureOptions != null)
            {
                configureOptions(options);

                services.Configure(configureOptions);
            }

            services.AddSingleton<IOptions<QuarztOptions>>(Options.Create(options));

            var quartzSettings = options.GetSettings();

            var jobsToSchedule = new List<(IJobDetail, ITrigger)>();

            if (quartzSettings.SqlType is SqlType.None)
            {
                services.AddControllers().ConfigureApplicationPartManager(apm =>
                {
                    var partToRemove = apm.ApplicationParts.OfType<AssemblyPart>().FirstOrDefault(p => p.Assembly.GetName().Name == "EasyCore.Quartz");

                    if (partToRemove != null)
                    {
                        apm.ApplicationParts.Remove(partToRemove);
                    }
                });
            }

            services.AddQuartz(q =>
            {
                if (quartzSettings.SqlType != SqlType.None)
                {
                    q.SchedulerId = "AUTO";

                    q.SchedulerName = Assembly.GetEntryAssembly()?.GetName().Name ?? "DefaultScheduler";

                    q.UsePersistentStore(s =>
                    {
                        s.UseProperties = false;

                        s.RetryInterval = TimeSpan.FromSeconds(15);

                        switch (quartzSettings.SqlType)
                        {
                            case SqlType.MySql:

                                s.UseMySql(mysql =>
                                {
                                    mysql.ConnectionString = quartzSettings.ConnectionString;

                                    mysql.TablePrefix = "qrtz_";
                                });

                                break;

                            case SqlType.SqlServer:

                                s.UseSqlServer(sql =>
                                {
                                    sql.ConnectionString = quartzSettings.ConnectionString;

                                    sql.TablePrefix = "qrtz_";
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

                var rootDirectory = AppDomain.CurrentDomain.BaseDirectory;

                string[] dllFiles = Directory.GetFiles(rootDirectory, "*.dll", SearchOption.TopDirectoryOnly).Where(path =>
                {
                    string fileName = Path.GetFileName(path);
                    return !(fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) || fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase));
                }).ToArray();

                var maxConcurrency = 0;

                try
                {
                    foreach (var dllFile in dllFiles)
                    {
                        var assembly = Assembly.LoadFrom(dllFile);

                        var jobTypes = assembly.GetTypes()
                        .Where(t => typeof(IEasyCoreJob)
                        .IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface &&
                        t.GetCustomAttribute<EasyCoreCronAttribute>() != null &&
                        t.GetCustomAttribute<EasyCoreDisableJobAttribute>() == null)
                        .ToList();

                        foreach (var jobType in jobTypes)
                        {
                            var cronAttr = jobType.GetCustomAttribute<EasyCoreCronAttribute>()!;

                            var group = cronAttr.JobGroup ?? "DEFAULT";

                            var jobKeyName = cronAttr.JobKey ?? jobType.FullName!;

                            var jobKey = new JobKey(jobKeyName, group);

                            var triggerKey = new TriggerKey($"{jobKeyName}.trigger", group);

                            var wrapperType = typeof(JobWrapper<>).MakeGenericType(jobType);

                            var requestRecovery  =  cronAttr.RequestRecovery;

                            var jobDetail = JobBuilder.Create(wrapperType)
                                .WithIdentity(jobKey)
                                .StoreDurably()
                                .RequestRecovery(requestRecovery)
                                .Build();

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

                            maxConcurrency += jobTypes.Count;
                        }
                    }
                }
                catch
                {

                }

                q.AddJob<HttpInvokeJob>(opts => opts.StoreDurably());

                q.UseDefaultThreadPool(tp =>
                {
                    tp.MaxConcurrency = options.MaxConcurrency == 0 ? maxConcurrency : options.MaxConcurrency;
                });
            });

            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            services.AddHttpClient("QuartzHttpClient");

            services.AddSingleton<IEnumerable<(IJobDetail, ITrigger)>>(jobsToSchedule);

            services.AddHostedService<QuartzJobSchedulerHostedService>();
        }
    }
}