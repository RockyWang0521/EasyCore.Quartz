namespace EasyCore.Quartz.Dashboard;

/// <summary>
/// Options for the EasyCore Quartz dashboard.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// Relative path under the host app (e.g. <c>/easy-quartz</c>).
    /// Full URL is <c>{appBaseUrl}{PathMatch}</c>.
    /// </summary>
    public string PathMatch { get; set; } = "/easy-quartz";

    /// <summary>
    /// Basic Auth username. Required when enabling the dashboard.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Basic Auth password. Required when enabling the dashboard.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Link back to the host application.
    /// </summary>
    public string AppPath { get; set; } = "/";

    /// <summary>
    /// Display name shown in the dashboard header.
    /// </summary>
    public string DashboardTitle { get; set; } = "EasyCore Quartz";

    /// <summary>
    /// Authorization filters. All registered filters must authorize the request.
    /// When empty, access is denied (fail closed).
    /// </summary>
    public IList<IEasyCoreQuartzAuthorizationFilter> Authorization { get; } =
        new List<IEasyCoreQuartzAuthorizationFilter>();
}
