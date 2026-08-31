using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using TrackerKerja.Data;
using TrackerKerja.Models;

namespace TrackerKerja.Filters
{
    public class AuditLogActionFilter : IAsyncActionFilter
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AuditLogActionFilter(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
            var actionName = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
            var path = context.HttpContext.Request.Path.Value ?? "/";

            // Ignore background polling endpoints to avoid log clutter
            if (path.Equals("/Task/GetRunningTimer", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var executedContext = await next();
            stopwatch.Stop();

            try
            {
                var httpContext = context.HttpContext;
                var user = httpContext.User;

                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;
                var userName = user.Identity?.Name;

                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
                {
                    ipAddress = forwardedFor.FirstOrDefault();
                }

                var statusCode = httpContext.Response.StatusCode;

                // Create a scope to resolve DbContext safely
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserEmail = userEmail,
                    UserName = userName,
                    ControllerName = controllerName,
                    ActionName = actionName,
                    HttpMethod = httpContext.Request.Method,
                    Path = path,
                    QueryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : null,
                    IpAddress = ipAddress,
                    StatusCode = statusCode,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.Now
                };

                db.AuditLogs.Add(auditLog);
                await db.SaveChangesAsync();
            }
            catch
            {
                // Silently ignore audit logging errors to prevent breaking user requests
            }
        }
    }
}
