using Microsoft.AspNetCore.Http;

namespace EasyCore.Quartz.Dashboard;

/// <summary>
/// Allows dashboard access only from loopback addresses.
/// Suitable for local development.
/// </summary>
public sealed class LocalRequestsOnlyAuthorizationFilter : IEasyCoreQuartzAuthorizationFilter
{
    public bool Authorize(HttpContext context)
    {
        var connection = context.Connection;
        if (connection.RemoteIpAddress is null)
        {
            return false;
        }

        if (connection.LocalIpAddress is not null)
        {
            return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
        }

        return System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress);
    }
}
