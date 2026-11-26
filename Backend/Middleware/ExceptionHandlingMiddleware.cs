using Ecommerce.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Backend.Middleware;

/// <summary>
/// Converts exceptions into consistent RFC 7807 problem+json responses.
/// Expected errors (<see cref="ApiException"/>) expose their message; anything else is
/// logged and returned as a generic 500 without leaking internals.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            await WriteProblemAsync(context, ex.StatusCode, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; nothing to send.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later.");
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string detail)
    {
        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.Clear();
        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrase(status),
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    private static string ReasonPhrase(int status) => status switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        404 => "Not Found",
        409 => "Conflict",
        _ => "Server Error"
    };
}
