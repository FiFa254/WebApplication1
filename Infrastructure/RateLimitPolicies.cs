using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace DevFolio.Infrastructure;

public static class RateLimitPolicies
{
    public const string Login = "login";

    /// <summary>At most RateLimiting:LoginPerMinute (default 5) login attempts per minute per client IP.</summary>
    public static void Configure(RateLimiterOptions options, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:LoginPerMinute", 5);

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(Login, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    }
}
