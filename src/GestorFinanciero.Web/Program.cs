using System.Security.Claims;
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

var builder = WebApplication.CreateBuilder(args);

// ─── Blazor Server ──────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

builder.Services.AddAuthorization();

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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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
