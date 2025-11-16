namespace EasyCore.Quartz;

/// <summary>
/// Prevents concurrent execution of the same job instance.
/// Mapped to Quartz <c>DisallowConcurrentExecution</c> on the scheduled job detail.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EasyCoreDisallowConcurrentExecutionAttribute : Attribute
{
}
