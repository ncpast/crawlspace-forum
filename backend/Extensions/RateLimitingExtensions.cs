using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Extensions;

public static class RateLimitingExtensions
{
    private static void AddIpPolicy(
    RateLimiterOptions options,
    string policyName,
    int permitLimit)
    {
        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }
            )
        );
    }

    public static IServiceCollection AddForumRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            AddIpPolicy(options, "create-post", 3);
            AddIpPolicy(options, "create-reply", 10);
            AddIpPolicy(options, "read", 120);
        });

    return services;
    }
}

