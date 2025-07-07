namespace EasyCore.Quartz
{
    /// <summary>
    /// Indicates that a Quartz job should be disabled and not scheduled.
    /// Apply this attribute to a class implementing IEasyCoreJob to exclude it
    /// from automatic registration and scheduling.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class EasyCoreDisableJobAttribute : Attribute
    {
        // This attribute serves as a marker. No properties needed.
    }
}
