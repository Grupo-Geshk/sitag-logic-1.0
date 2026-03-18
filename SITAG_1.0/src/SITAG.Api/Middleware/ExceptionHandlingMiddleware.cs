using System.Text.Json;
using FluentValidation;
using SITAG.Domain.Common;

namespace SITAG.Api.Middleware;

/// <summary>
/// Global exception handler. Converts unhandled exceptions into structured
/// JSON problem responses. Stack traces are only included in Development.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            // FluentValidation failures → 400 with structured error list
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await WriteAsync(context, new
            {
                type    = "https://httpstatuses.com/400",
                title   = "Validation Failed",
                status  = 400,
                traceId = context.TraceIdentifier,
                errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title) = exception switch
        {
            ArgumentException or ArgumentNullException    => (400, "Bad Request"),
            UnauthorizedAccessException                   => (401, "Unauthorized"),
            KeyNotFoundException                          => (404, "Not Found"),
            ConflictException                             => (409, "Conflict"),
            InvalidOperationException                     => (422, "Unprocessable Operation"),
            _                                             => (500, "Internal Server Error"),
        };

        context.Response.StatusCode = statusCode;

        var body = new Dictionary<string, object?>
        {
            ["type"]    = $"https://httpstatuses.com/{statusCode}",
            ["title"]   = title,
            ["status"]  = statusCode,
            ["traceId"] = context.TraceIdentifier,
        };

        if (_env.IsDevelopment())
        {
            body["detail"]     = exception.Message;
            body["stackTrace"] = exception.StackTrace;
        }

        await WriteAsync(context, body);
    }

    private static Task WriteAsync(HttpContext context, object payload) =>
        context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
}
