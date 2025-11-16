using System.Reflection;
using Microsoft.Extensions.Logging;

namespace EasyCore.Quartz.Discovery;

/// <summary>
/// Discovers <see cref="IEasyCoreJob"/> types decorated with <see cref="EasyCoreCronAttribute"/>.
/// </summary>
public static class JobTypeDiscovery
{
    /// <summary>
    /// Scans the configured assemblies (plus the entry assembly by default) for schedulable jobs.
    /// </summary>
    public static IReadOnlyList<Type> Discover(EasyCoreQuartzOptions options, ILogger? logger = null)
    {
        var assemblies = new HashSet<Assembly>();

        foreach (var assembly in options.Assemblies)
        {
            assemblies.Add(assembly);
        }

        var entry = Assembly.GetEntryAssembly();
        if (entry is not null)
        {
            assemblies.Add(entry);
        }

        var calling = Assembly.GetCallingAssembly();
        if (calling is not null)
        {
            assemblies.Add(calling);
        }

        var result = new List<Type>();
        var errors = new List<Exception>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var jobTypes = assembly.GetTypes()
                    .Where(t => typeof(IEasyCoreJob).IsAssignableFrom(t)
                                && t is { IsAbstract: false, IsInterface: false }
                                && t.GetCustomAttribute<EasyCoreCronAttribute>() is not null
                                && t.GetCustomAttribute<EasyCoreDisableJobAttribute>() is null)
                    .ToList();

                result.AddRange(jobTypes);
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger?.LogWarning(ex, "Failed to load types from assembly {Assembly}", assembly.FullName);
                errors.Add(ex);
                if (ex.Types is not null)
                {
                    foreach (var t in ex.Types.Where(t => t is not null)!)
                    {
                        if (t is not null
                            && typeof(IEasyCoreJob).IsAssignableFrom(t)
                            && t is { IsAbstract: false, IsInterface: false }
                            && t.GetCustomAttribute<EasyCoreCronAttribute>() is not null
                            && t.GetCustomAttribute<EasyCoreDisableJobAttribute>() is null)
                        {
                            result.Add(t);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Job discovery failed for assembly {Assembly}", assembly.FullName);
                errors.Add(ex);
            }
        }

        if (errors.Count > 0 && options.ThrowOnDiscoveryErrors)
        {
            throw new AggregateException("One or more errors occurred during job discovery.", errors);
        }

        return result
            .GroupBy(t => t.FullName)
            .Select(g => g.First())
            .ToList();
    }
}
