using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PersonalisationEngine.Api.Middleware;

public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        var (status, message) = ex switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, e.Message),
            BadRequestException e => (StatusCodes.Status400BadRequest, e.Message),
            DbUpdateException e when IsUniqueConstraintViolation(e) =>
                (StatusCodes.Status409Conflict, "A record with that identifier already exists"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }

    // Npgsql unique-constraint violation = PostgreSQL error code 23505
    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.GetType().Name == "PostgresException" &&
        ex.InnerException.GetType().GetProperty("SqlState")?.GetValue(ex.InnerException) is "23505";
}

public class NotFoundException(string msg) : Exception(msg);
public class ConflictException(string msg) : Exception(msg);
public class BadRequestException(string msg) : Exception(msg);
