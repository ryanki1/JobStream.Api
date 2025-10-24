using JobStream.Api.DTOs;
using System.Net;
using System.Text.Json;

namespace JobStream.Api.Middleware;

/// <summary>
/// Global error handling middleware that catches unhandled exceptions
/// and returns standardized error responses
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = "VALIDATION_ERROR";
                errorResponse.Message = exception.Message;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Code = "UNAUTHORIZED";
                errorResponse.Message = "You are not authorized to perform this action";
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Code = "NOT_FOUND";
                errorResponse.Message = exception.Message;
                break;

            case FileNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Code = "FILE_NOT_FOUND";
                errorResponse.Message = exception.Message;
                break;

            case ArgumentNullException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = "INVALID_ARGUMENT";
                errorResponse.Message = exception.Message;
                break;

            case ArgumentException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = "INVALID_ARGUMENT";
                errorResponse.Message = exception.Message;
                break;

            case TimeoutException:
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Code = "TIMEOUT";
                errorResponse.Message = "The request timed out";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Code = "INTERNAL_SERVER_ERROR";
                errorResponse.Message = _environment.IsDevelopment()
                    ? exception.Message
                    : "An internal server error occurred. Please try again later.";
                break;
        }

        // Include stack trace in development mode
        if (_environment.IsDevelopment() && exception.StackTrace != null)
        {
            errorResponse.Details = new ErrorDetails
            {
                Reason = exception.StackTrace
            };
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(errorResponse, options);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension method to easily add error handling middleware
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
