using EasyCore.Quartz.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.Quartz;

/// <summary>
/// Extension methods to enable the EasyCore Quartz dashboard from <see cref="EasyCoreQuartzOptions"/>.
/// </summary>
public static class EasyCoreQuartzDashboardOptionsExtensions
{
    /// <summary>
    /// Enables the EasyCore Quartz dashboard. Path is relative to the host app base URL.
    /// Username and password are required (HTTP Basic Auth). Middleware is mounted automatically.
    /// </summary>
    public static EasyCoreQuartzOptions UseEasyCoreQuartzDashboard(
        this EasyCoreQuartzOptions options,
        Action<DashboardOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        var dash = new DashboardOptions();
        configure(dash);

        if (string.IsNullOrWhiteSpace(dash.Username) || string.IsNullOrWhiteSpace(dash.Password))
        {
            throw new ArgumentException(
                "Dashboard Username and Password are required. Configure them via UseEasyCoreQuartzDashboard(...).",
                nameof(configure));
        }

        if (string.IsNullOrWhiteSpace(dash.PathMatch))
        {
            dash.PathMatch = "/easy-quartz";
        }

        if (!dash.PathMatch.StartsWith('/'))
        {
            dash.PathMatch = "/" + dash.PathMatch;
        }

        if (dash.Authorization.Count == 0)
        {
            dash.Authorization.Add(new BasicAuthAuthorizationFilter(dash.Username, dash.Password));
        }

        options.AddServiceConfigurator(services =>
        {
            services.AddSingleton(dash);
            services.AddSingleton<IStartupFilter>(_ => new EasyCoreQuartzDashboardStartupFilter(dash));
        });

        return options;
    }
}
