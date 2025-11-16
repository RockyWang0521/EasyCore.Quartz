namespace EasyCore.Quartz;

/// <summary>
/// Strategy used when a scheduled fire time is missed.
/// </summary>
public enum MisfirePolicyType
{
    /// <summary>Use Quartz default misfire instructions.</summary>
    Default,

    /// <summary>Ignore misfires and continue with the next fire time.</summary>
    Ignore,

    /// <summary>Fire immediately once, then resume the schedule.</summary>
    FireNow,

    /// <summary>Do nothing; wait for the next scheduled fire time.</summary>
    DoNothing
}
