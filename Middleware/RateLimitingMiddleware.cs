using System.Collections.Concurrent;
using System.Net;

namespace JobStream.Api.Middleware;

/// <summary>
/// Simple in-memory rate limiting middleware
/// For production, consider using Redis or a distributed cache
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    private readonly int _requestLimit;
    private readonly TimeSpan _timeWindow;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _requestLimit = configuration.GetValue<int>("RateLimiting:RequestsPerMinute", 10);
        _timeWindow = TimeSpan.FromMinutes(1);

        // Start cleanup task
        _ = Task.Run(CleanupExpiredEntriesAsync);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get client identifier (IP address)
        var clientId = GetClientIdentifier(context);

        // Check rate limit
        if (!CheckRateLimit(clientId))
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            var errorResponse = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = true,
                code = "RATE_LIMIT_EXCEEDED",
                message = $"Too many requests. Maximum {_requestLimit} requests per minute allowed.",
                retryAfter = _timeWindow.TotalSeconds
            });

            await context.Response.WriteAsync(errorResponse);
            return;
        }

        // Continue to next middleware
        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get real IP from X-Forwarded-For header (for proxies/load balancers)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool CheckRateLimit(string clientId)
    {
        var now = DateTime.UtcNow;

        var clientInfo = _clients.GetOrAdd(clientId, _ => new ClientRequestInfo());

        lock (clientInfo)
        {
            // Remove old requests outside the time window
            clientInfo.RequestTimestamps.RemoveAll(timestamp => now - timestamp > _timeWindow);

            // Check if limit exceeded
            if (clientInfo.RequestTimestamps.Count >= _requestLimit)
            {
                return false;
            }

            // Add current request
            clientInfo.RequestTimestamps.Add(now);
            clientInfo.LastAccess = now;

            return true;
        }
    }

    private async Task CleanupExpiredEntriesAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5));

                var now = DateTime.UtcNow;
                var expiredClients = _clients
                    .Where(kvp => now - kvp.Value.LastAccess > TimeSpan.FromMinutes(10))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var clientId in expiredClients)
                {
                    _clients.TryRemove(clientId, out _);
                }

                if (expiredClients.Any())
                {
                    _logger.LogDebug("Cleaned up {Count} expired rate limit entries", expiredClients.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rate limit cleanup task");
            }
        }
    }

    private class ClientRequestInfo
    {
        public List<DateTime> RequestTimestamps { get; set; } = new();
        public DateTime LastAccess { get; set; } = DateTime.UtcNow;
    }
}

/// <summary>
/// Extension method to easily add rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
