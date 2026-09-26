using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace DevFolio.Infrastructure;

public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Contact = "contact";

    /// <summary>
    /// Login: RateLimiting:LoginPerMinute (default 5) attempts per minute per client IP.
    /// Contact: RateLimiting:ContactPerMinute (default 10) e-mail reveals per minute per client IP, to slow scrapers.
    /// </summary>
    public static void Configure(RateLimiterOptions options, IConfiguration configuration)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        AddPerIpPolicy(options, Login, configuration.GetValue("RateLimiting:LoginPerMinute", 5));
        AddPerIpPolicy(options, Contact, configuration.GetValue("RateLimiting:ContactPerMinute", 10));
    }

    private static void AddPerIpPolicy(RateLimiterOptions options, string name, int permitLimit) =>
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
}
