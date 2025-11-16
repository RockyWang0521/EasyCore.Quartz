namespace EasyCore.Quartz.Dashboard;

/// <summary>
/// Options for the embedded EasyCore Quartz dashboard.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// Authorization filters. All registered filters must authorize the request.
    /// When empty, access is denied (fail closed).
    /// </summary>
    public IList<IEasyCoreQuartzAuthorizationFilter> Authorization { get; } =
        new List<IEasyCoreQuartzAuthorizationFilter>();

    /// <summary>
    /// Dashboard page title.
    /// </summary>
    public string AppPath { get; set; } = "/";

    /// <summary>
    /// Display name shown in the sidebar.
    /// </summary>
    public string DashboardTitle { get; set; } = "EasyCore Quartz";
}
