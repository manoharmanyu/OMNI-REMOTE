using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace OmniRemote.Backend.Middleware;

public sealed class SafeErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SafeErrorHandlingMiddleware> _logger;

    public SafeErrorHandlingMiddleware(RequestDelegate next, ILogger<SafeErrorHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled server exception occurred while processing request.");

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Instance = context.Request.Path,
                };

                await JsonSerializer.SerializeAsync(context.Response.Body, problem);
            }
        }
    }
}
