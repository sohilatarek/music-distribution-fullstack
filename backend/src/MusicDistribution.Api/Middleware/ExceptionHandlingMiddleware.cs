using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MusicDistribution.Application.Common.Exceptions;

namespace MusicDistribution.Api.Middleware;

/// <summary>
/// Central place to translate domain/application exceptions into consistent, meaningful
/// HTTP error responses instead of leaking stack traces to API consumers.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ValidationAppException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Safety net for races that slip past the service-level uniqueness checks.
            _logger.LogWarning(ex, "Unique constraint violated on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteAsync(context, HttpStatusCode.Conflict,
                "A record with the same unique value already exists.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteAsync(context, HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;

    private static async Task WriteAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new { message });
        await context.Response.WriteAsync(payload);
    }
}
