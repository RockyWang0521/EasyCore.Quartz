using Quartz;

namespace EasyCore.Quartz;

/// <summary>
/// Marker interface for EasyCore Quartz jobs.
/// Implement this interface and decorate the class with <see cref="EasyCoreCronAttribute"/>
/// to enable automatic discovery and scheduling.
/// </summary>
public interface IEasyCoreJob : IJob
{
}
