using Microsoft.AspNetCore.Http;

namespace EasyCore.Quartz.Dashboard;

/// <summary>
/// Allows every request. Do not use in production without an outer auth layer.
/// </summary>
public sealed class AllowAllAuthorizationFilter : IEasyCoreQuartzAuthorizationFilter
{
    public bool Authorize(HttpContext context) => true;
}
