namespace OmniRemote.Backend.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none';";
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            if (context.User.Identity?.IsAuthenticated == true || context.Request.Path.StartsWithSegments("/api/v1/auth"))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                headers["Pragma"] = "no-cache";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
