using Quartz;

namespace EasyCore.Quartz
{
    /// <summary>
    /// Represents a marker interface for jobs used in the EasyCoreQuartz framework.
    /// 
    /// Any job that implements this interface will be automatically discovered
    /// and scheduled by the EasyCoreQuartz system, as long as it is also decorated
    /// with the <see cref="EasyCoreCronAttribute"/>.
    /// 
    /// This interface extends Quartz's <see cref="IJob"/>, so implementing classes
    /// must also implement the <see cref="IJob.Execute(IJobExecutionContext)"/> method.
    /// </summary>
    public interface IEasyCoreJob : IJob
    {

    }
}
