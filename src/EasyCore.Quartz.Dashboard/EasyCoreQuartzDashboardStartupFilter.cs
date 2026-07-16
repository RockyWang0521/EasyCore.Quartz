using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace EasyCore.Quartz.Dashboard;

/// <summary>
/// Automatically mounts <see cref="EasyCoreQuartzDashboardMiddleware"/> when the dashboard is configured.
/// </summary>
internal sealed class EasyCoreQuartzDashboardStartupFilter : IStartupFilter
{
    private readonly DashboardOptions _options;

    public EasyCoreQuartzDashboardStartupFilter(DashboardOptions options)
    {
        _options = options;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var path = string.IsNullOrWhiteSpace(_options.PathMatch) ? "/easy-quartz" : _options.PathMatch;
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            app.UseMiddleware<EasyCoreQuartzDashboardMiddleware>(new PathString(path), _options);
            next(app);
        };
    }
}
