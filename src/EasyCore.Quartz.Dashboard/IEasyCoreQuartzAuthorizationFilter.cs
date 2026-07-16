using Microsoft.AspNetCore.Http;

namespace EasyCore.Quartz.Dashboard;

/// <summary>
/// Authorization filter for the EasyCore Quartz dashboard.
/// </summary>
public interface IEasyCoreQuartzAuthorizationFilter
{
    /// <summary>
    /// Returns true when the current request is allowed to access the dashboard.
    /// </summary>
    bool Authorize(HttpContext context);
}
