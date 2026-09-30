using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Pino.Features.Account.Services;

internal static class AccountRequestLimits
{
    internal static void Configure(RateLimiterOptions options)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.Path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase))
            {
                return RateLimitPartition.GetNoLimiter("outside-account-posts");
            }

            // Use the connection address, never an untrusted forwarded-header value.
            var key = context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new()
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)) : 60;
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            await context.HttpContext.Response.WriteAsync("Too many account requests. Wait a minute, then return to the account page and try again.", token);
        };
    }
}
