using System.Security.Claims;
using System.Threading.RateLimiting;
using dotenv.net;
using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Constants;
using GestorFinanciero.Infrastructure;
using GestorFinanciero.Infrastructure.Identity;
using GestorFinanciero.Infrastructure.Seeding;
using GestorFinanciero.Web.Components;
using GestorFinanciero.Web.Components.Account;
using GestorFinanciero.Web.Infrastructure;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using MudBlazor.Services;

// ─── Load .env (if present) BEFORE creating the builder so IConfiguration
//     picks the values up as environment variables. Precedence stays intact:
//     env vars > user-secrets > appsettings.{Environment}.json > appsettings.json.
// ────────────────────────────────────────────────────────────────────────
DotEnv.Load(new DotEnvOptions(
    ignoreExceptions: true,          // no .env file? no problem — user-secrets and env vars still work
    envFilePaths: [".env"],          // look in the current working directory
    overwriteExistingVars: false,    // real env vars (from Cloud Run, Docker) always win
    trimValues: true));

var builder = WebApplication.CreateBuilder(args);

// ─── Cloud Run: honor the $PORT env var if set (default is 8080). Cloud
//     Run injects it at cold-start; the container must listen on that port
//     or the deploy fails healthchecks. Falls through to ASPNETCORE_URLS
//     when PORT is unset, so local dev/dotnet-watch keeps working.
// ────────────────────────────────────────────────────────────────────────
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var runtimePort))
{
    builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(runtimePort));
}

// ─── Blazor Server ──────────────────────────────────────────────────────
// DetailedErrors=true forwards the server-side exception message back to the
// browser instead of the generic "unhandled exception on the current circuit".
// Enable it via env var when debugging containerised runs.
var blazorDetailedErrors =
    string.Equals(Environment.GetEnvironmentVariable("BLAZOR_DETAILED_ERRORS"), "true", StringComparison.OrdinalIgnoreCase)
    || builder.Environment.IsDevelopment();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = blazorDetailedErrors;
    });

// ─── MudBlazor (dialogs, snackbar, popover, resize watcher) ─────────────
builder.Services.AddMudServices();

// ─── Infrastructure: EF Core + Postgres + Identity ──────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ─── Auth: cookies + revalidating state provider for Blazor Server ──────
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

// ─── Cookie hardening + idle timeout ────────────────────────────────────
// Runs AFTER AddIdentityCookies() so we override Identity's defaults for the
// two cookies it registers: Application (the login session) and External
// (the OAuth handshake). Session gets a rolling 60-minute idle timeout.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name        = ".gf.auth";
    options.Cookie.HttpOnly    = true;
    options.Cookie.SameSite    = SameSiteMode.Lax;      // Lax needed for OAuth-style redirects
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);  // idle timeout
    options.SlidingExpiration = true;                    // reset on activity
    options.LoginPath  = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.HttpOnly     = true;
    options.Cookie.SameSite     = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan      = TimeSpan.FromMinutes(15);
});

builder.Services.AddAuthorization();

// ─── Rate limiter: throttle brute force at the HTTP layer ───────────────
// The lockout in Identity is *per account*; this limiter is *per IP* so a
// single client can't try 100 emails × 5 passwords in a row.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // /Account/Login → 5 attempts / minute per IP.
    options.AddPolicy("auth-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true,
            }));

    // /Account/Register + /Account/ForgotPassword → 3 attempts / 5 min per IP.
    options.AddPolicy("auth-sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true,
            }));
});

// ─── Health checks (Cloud Run + Docker HEALTHCHECK) ─────────────────────
// /health/live  → cheap liveness probe (no DB round-trip)
// /health/ready → readiness probe including DB connectivity
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GestorFinanciero.Infrastructure.Persistence.AppDbContext>(
        name: "postgres",
        tags: new[] { "ready" });

// ─── Forwarded headers — trust Cloud Run / reverse-proxy headers so the
// app sees the original scheme (https), remote IP and host. Critical for
// the per-IP rate limiter and for correct absolute URL generation in emails.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;

    // Cloud Run's proxy is on a wide range of GCP internal IPs; clearing the
    // known-networks/proxies lists is the platform-agnostic way to accept
    // forwarded headers from any hop. The alternative would be maintaining
    // Google's IP allowlist here.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ─── Startup seeders (run pending ISeeders once per environment) ────────
// Runs before serving the first request. Migrations must already be applied
// (do `dotnet ef database update` before deploy).
using (var startupScope = app.Services.CreateScope())
{
    var runner = startupScope.ServiceProvider.GetRequiredService<SeederRunner>();
    await runner.RunPendingAsync(CancellationToken.None);
}

// ─── HTTP pipeline ──────────────────────────────────────────────────────
// Runs FIRST so it can log every unhandled exception to error_logs before the
// developer/production error page renders the response.
app.UseExceptionLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Adds CSP, X-Frame-Options, Referrer-Policy, Permissions-Policy, etc.
app.UseSecurityHeaders(app.Environment);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Consume the forwarded headers configured above BEFORE any middleware that
// reads the request scheme/host/IP (auth cookies, rate limiter, redirects).
app.UseForwardedHeaders();

// Skip HTTPS redirect when running inside a container — Cloud Run terminates
// TLS at the edge and forwards plain HTTP to us, so a redirect either loops
// or fails ("Failed to determine the https port"). Kestrel still sees the
// original scheme (https) via the forwarded headers above.
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseRateLimiter();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ─── Health probes (Cloud Run + Docker HEALTHCHECK) ────────────────────
// `/health` = simple liveness (no dependencies checked → won't 503 on DB blips).
// `/health/ready` = readiness (includes Postgres). Cloud Run uses this to know
// when it can start routing traffic.
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    // Skip DB checks so a hiccup doesn't take the container out of rotation.
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

// ─── Logout endpoint (POST, so no CSRF-by-navigation) ───────────────────
app.MapPost("/Account/Logout", async (
    HttpContext ctx,
    SignInManager<AppUser> signInManager,
    IAppEventLogger appEvents) =>
{
    var userIdRaw = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = ctx.User.FindFirstValue(ClaimTypes.Email) ?? ctx.User.Identity?.Name;
    Guid? userId = Guid.TryParse(userIdRaw, out var id) ? id : null;

    await signInManager.SignOutAsync();

    await appEvents.LogAsync(
        module: EventModules.AuthLogout,
        action: EventActions.Success,
        message: "User logged out.",
        userId: userId,
        userEmail: email);

    return Results.LocalRedirect("/");
});

app.Run();
