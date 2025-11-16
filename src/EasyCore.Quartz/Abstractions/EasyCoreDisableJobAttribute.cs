namespace EasyCore.Quartz;

/// <summary>
/// Excludes a job from automatic discovery and scheduling.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EasyCoreDisableJobAttribute : Attribute
{
}
