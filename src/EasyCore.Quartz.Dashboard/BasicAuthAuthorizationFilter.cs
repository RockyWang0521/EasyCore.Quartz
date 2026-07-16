using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace EasyCore.Quartz.Dashboard;

/// <summary>
/// HTTP Basic Authentication filter for the dashboard.
/// </summary>
public sealed class BasicAuthAuthorizationFilter : IEasyCoreQuartzAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _realm;

    public BasicAuthAuthorizationFilter(string username, string password, string realm = "EasyCore Quartz")
    {
        _username = username;
        _password = password;
        _realm = realm;
    }

    public bool Authorize(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) ||
            !AuthenticationHeaderValue.TryParse(header, out var auth) ||
            !string.Equals(auth.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(auth.Parameter))
        {
            Challenge(context);
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter));
            var separator = decoded.IndexOf(':');
            if (separator < 0)
            {
                Challenge(context);
                return false;
            }

            var user = decoded[..separator];
            var pass = decoded[(separator + 1)..];
            if (string.Equals(user, _username, StringComparison.Ordinal) &&
                string.Equals(pass, _password, StringComparison.Ordinal))
            {
                return true;
            }
        }
        catch (FormatException)
        {
            // ignore invalid base64
        }

        Challenge(context);
        return false;
    }

    private void Challenge(HttpContext context)
    {
        context.Response.Headers.WWWAuthenticate = $"Basic realm=\"{_realm}\"";
    }
}
