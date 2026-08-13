using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestorFinanciero.Infrastructure.Seeding.Seeders;

/// <summary>
/// Creates a demo account used for local development and portfolio screenshots.
/// Email: <c>demo@gestor.dev</c> / Password: <c>Demo1234</c>.
/// </summary>
/// <remarks>
/// The seeder is idempotent even outside the tracker: if the demo email already
/// exists, it exits early. This lets us run it manually with no fear of dupes.
/// </remarks>
public sealed class SeedDemoUser : ISeeder
{
    public const string DemoEmail = "demo@gestor.dev";
    public const string DemoPassword = "Demo1234";

    public string Key => "0001_SeedDemoUser";

    public async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var categorySeeder = services.GetRequiredService<ICategorySeeder>();
        var logger = services.GetRequiredService<ILogger<SeedDemoUser>>();

        var existing = await userManager.FindByEmailAsync(DemoEmail);
        if (existing is not null)
        {
            logger.LogInformation("Demo user already exists — skipping.");
            return;
        }

        var user = new AppUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,          // skip email confirmation for the demo
            FullName = "Demo Gestor",
            PreferredCurrency = "USD",
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Failed to seed demo user: {errors}");
        }

        // Reuse the same per-user seeder that runs during registration to keep
        // the default category list consistent.
        await categorySeeder.SeedForUserAsync(user.Id, cancellationToken);

        logger.LogInformation("Demo user created: {Email}", DemoEmail);
    }
}
