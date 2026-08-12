namespace GestorFinanciero.Web.Infrastructure;

/// <summary>
/// Adds a hardened set of response headers to every HTML response. Values are
/// tuned for a Blazor Server + MudBlazor + Google Fonts setup — tighten CSP
/// further if you drop the CDN dependencies.
/// </summary>
public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        return app.Use(async (ctx, next) =>
        {
            var headers = ctx.Response.Headers;

            // Hides the ASP.NET Core version leak.
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            // Prevents MIME sniffing (e.g. a .txt served as HTML).
            headers["X-Content-Type-Options"] = "nosniff";

            // Blocks the site from being rendered inside a frame → clickjacking guard.
            headers["X-Frame-Options"] = "DENY";

            // Leak as little URL info as possible to third parties.
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Deny access to sensitive browser APIs by default.
            headers["Permissions-Policy"] =
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), " +
                "magnetometer=(), microphone=(), payment=(), usb=()";

            // Cross-Origin Opener Policy — isolates our window from same-origin popups.
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // Content-Security-Policy — the big one. Kept in dev-friendly mode
            // (allow 'unsafe-inline' + 'unsafe-eval') because Blazor Server injects
            // inline scripts on the wire. Tighten with nonces post-launch if desired.
            //
            // Whitelisted:
            //   - self (everything served by us)
            //   - fonts.googleapis.com + fonts.gstatic.com (Inter font)
            //   - cdnjs.cloudflare.com (artifact runtime libs)
            //   - api.pwnedpasswords.com is fetched server-side, no browser rule needed
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com data:; " +
                "img-src 'self' data: https:; " +
                "connect-src 'self' wss: ws:; " +   // Blazor Server needs the wss:// SignalR channel
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";

            // HSTS is set by app.UseHsts() when not development — no dup here.

            await next();
        });
    }
}
