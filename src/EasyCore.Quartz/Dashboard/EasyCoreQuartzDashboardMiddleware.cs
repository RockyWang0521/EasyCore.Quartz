using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasyCore.Quartz.Dto;
using EasyCore.Quartz.Management;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.Quartz.Dashboard;

/// <summary>
/// Serves the embedded English dashboard UI and JSON API under a path prefix.
/// </summary>
public sealed class EasyCoreQuartzDashboardMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly RequestDelegate _next;
    private readonly PathString _pathMatch;
    private readonly DashboardOptions _options;
    private readonly Assembly _assembly = typeof(EasyCoreQuartzDashboardMiddleware).Assembly;

    public EasyCoreQuartzDashboardMiddleware(
        RequestDelegate next,
        PathString pathMatch,
        DashboardOptions options)
    {
        _next = next;
        _pathMatch = pathMatch.HasValue ? pathMatch : new PathString("/easy-quartz");
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(_pathMatch, out var remaining))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (_options.Authorization.Count == 0 || !_options.Authorization.All(f => f.Authorize(context)))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized EasyCore Quartz dashboard request.").ConfigureAwait(false);
            return;
        }

        var path = remaining.Value ?? "/";
        if (string.IsNullOrEmpty(path))
        {
            path = "/";
        }

        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await HandleApiAsync(context, path).ConfigureAwait(false);
            return;
        }

        await ServeStaticAsync(context, path).ConfigureAwait(false);
    }

    private async Task HandleApiAsync(HttpContext context, string path)
    {
        var jobs = context.RequestServices.GetRequiredService<IJobManagementService>();
        var method = context.Request.Method.ToUpperInvariant();

        try
        {
            if (method == "GET" && path.Equals("/api/overview", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context, await jobs.GetOverviewAsync(context.RequestAborted).ConfigureAwait(false))
                    .ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path.Equals("/api/jobs", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context, await jobs.GetAllJobsAsync(context.RequestAborted).ConfigureAwait(false))
                    .ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path.Equals("/api/recurring", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context, await jobs.GetRecurringJobsAsync(context.RequestAborted).ConfigureAwait(false))
                    .ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path.Equals("/api/executing", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context, await jobs.GetExecutingJobsAsync(context.RequestAborted).ConfigureAwait(false))
                    .ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path.Equals("/api/history", StringComparison.OrdinalIgnoreCase))
            {
                var take = 100;
                if (int.TryParse(context.Request.Query["take"], out var t))
                {
                    take = t;
                }

                await WriteJsonAsync(context, jobs.GetHistory(take)).ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path.StartsWith("/api/jobs/", StringComparison.OrdinalIgnoreCase))
            {
                var parts = path["/api/jobs/".Length..].Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var group = Uri.UnescapeDataString(parts[0]);
                    var name = Uri.UnescapeDataString(parts[1]);
                    var job = await jobs.GetJobAsync(name, group, context.RequestAborted).ConfigureAwait(false);
                    if (job is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    await WriteJsonAsync(context, job).ConfigureAwait(false);
                    return;
                }
            }

            if (method == "POST" && path.Equals("/api/http-jobs", StringComparison.OrdinalIgnoreCase))
            {
                var input = await JsonSerializer.DeserializeAsync<HttpJobInputDto>(
                    context.Request.Body, JsonOptions, context.RequestAborted).ConfigureAwait(false);
                if (input is null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                var result = await jobs.AddOrUpdateHttpJobAsync(input, context.RequestAborted).ConfigureAwait(false);
                context.Response.StatusCode = result.Success
                    ? StatusCodes.Status200OK
                    : StatusCodes.Status400BadRequest;
                await WriteJsonAsync(context, result).ConfigureAwait(false);
                return;
            }

            // POST /api/jobs/{group}/{name}/{action}
            if (method == "POST" && path.StartsWith("/api/jobs/", StringComparison.OrdinalIgnoreCase))
            {
                var parts = path["/api/jobs/".Length..].Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var group = Uri.UnescapeDataString(parts[0]);
                    var name = Uri.UnescapeDataString(parts[1]);
                    var action = parts[2].ToLowerInvariant();

                    object payload = action switch
                    {
                        "pause" => await jobs.PauseJobAsync(name, group, context.RequestAborted).ConfigureAwait(false),
                        "resume" => await jobs.ResumeJobAsync(name, group, context.RequestAborted).ConfigureAwait(false),
                        "trigger" => await jobs.TriggerJobAsync(name, group, context.RequestAborted).ConfigureAwait(false),
                        "delete" => await jobs.DeleteJobAsync(name, group, context.RequestAborted).ConfigureAwait(false),
                        "cron" => await UpdateCronFromBodyAsync(jobs, name, group, context).ConfigureAwait(false),
                        _ => throw new InvalidOperationException($"Unknown action '{action}'.")
                    };

                    await WriteJsonAsync(context, payload).ConfigureAwait(false);
                    return;
                }
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Dashboard API endpoint not found.").ConfigureAwait(false);
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteJsonAsync(context, new { error = ex.Message }).ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, new { error = ex.Message }).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, new { error = ex.Message }).ConfigureAwait(false);
        }
    }

    private static async Task<object> UpdateCronFromBodyAsync(
        IJobManagementService jobs,
        string name,
        string group,
        HttpContext context)
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted)
            .ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("cron", out var cronProp))
        {
            throw new ArgumentException("Request body must include a 'cron' property.");
        }

        var cron = cronProp.GetString() ?? string.Empty;
        return await jobs.UpdateCronAsync(name, cron, group, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task ServeStaticAsync(HttpContext context, string path)
    {
        if (path is "/" or "/index.html")
        {
            await WriteEmbeddedAsync(context, "Dashboard.wwwroot.index.html", "text/html; charset=utf-8")
                .ConfigureAwait(false);
            return;
        }

        if (path.Equals("/css/dashboard.css", StringComparison.OrdinalIgnoreCase))
        {
            await WriteEmbeddedAsync(context, "Dashboard.wwwroot.css.dashboard.css", "text/css; charset=utf-8")
                .ConfigureAwait(false);
            return;
        }

        if (path.Equals("/js/dashboard.js", StringComparison.OrdinalIgnoreCase))
        {
            await WriteEmbeddedAsync(context, "Dashboard.wwwroot.js.dashboard.js", "application/javascript; charset=utf-8")
                .ConfigureAwait(false);
            return;
        }

        // SPA fallback
        await WriteEmbeddedAsync(context, "Dashboard.wwwroot.index.html", "text/html; charset=utf-8")
            .ConfigureAwait(false);
    }

    private async Task WriteEmbeddedAsync(HttpContext context, string relativeName, string contentType)
    {
        var resourceName = $"{_assembly.GetName().Name}.{relativeName}";
        await using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync($"Embedded resource not found: {resourceName}").ConfigureAwait(false);
            return;
        }

        context.Response.ContentType = contentType;
        await stream.CopyToAsync(context.Response.Body).ConfigureAwait(false);
    }

    private static Task WriteJsonAsync(HttpContext context, object payload)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8);
    }
}

/// <summary>
/// Extension methods to map the EasyCore Quartz dashboard.
/// </summary>
public static class EasyCoreQuartzDashboardExtensions
{
    /// <summary>
    /// Maps the Hangfire-style English dashboard at the given path (default <c>/easy-quartz</c>).
    /// </summary>
    public static IApplicationBuilder UseEasyCoreQuartzDashboard(
        this IApplicationBuilder app,
        string pathMatch = "/easy-quartz",
        Action<DashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = new DashboardOptions();
        configure?.Invoke(options);

        if (options.Authorization.Count == 0)
        {
            // Fail closed unless the host explicitly adds filters.
            // Development hosts typically add LocalRequestsOnlyAuthorizationFilter.
        }

        pathMatch = string.IsNullOrWhiteSpace(pathMatch) ? "/easy-quartz" : pathMatch;
        if (!pathMatch.StartsWith('/'))
        {
            pathMatch = "/" + pathMatch;
        }

        return app.UseMiddleware<EasyCoreQuartzDashboardMiddleware>(new PathString(pathMatch), options);
    }
}
