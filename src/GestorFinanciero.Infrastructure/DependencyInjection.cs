using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Infrastructure.Email;
using GestorFinanciero.Infrastructure.Identity;
using GestorFinanciero.Infrastructure.Persistence;
using GestorFinanciero.Infrastructure.Persistence.Interceptors;
using GestorFinanciero.Infrastructure.Seeding;
using GestorFinanciero.Infrastructure.Seeding.Seeders;
using GestorFinanciero.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GestorFinanciero.Infrastructure;

/// <summary>
/// Extension method that wires the Infrastructure layer (EF Core + Identity)
/// into the ASP.NET Core service container.
/// </summary>
/// <remarks>
/// Call from <c>Program.cs</c>:
/// <code>builder.Services.AddInfrastructure(builder.Configuration);</code>
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Accepted formats:
        //  1. libpq URI       -> "postgresql://user:pass@host:5432/db?sslmode=require"
        //  2. Npgsql key/value -> "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require"
        //
        // Local dev uses user-secrets (URI is fine).
        // Cloud Run gets the same URI directly from the ConnectionStrings__Default env var.
        var rawConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Missing connection string 'Default'. Set it via user secrets or env vars.");

        var connectionString = ResolvePostgresConnectionString(rawConnectionString);

        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            });

            var interceptor = sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>();
            options.AddInterceptors(interceptor);
        });

        // ASP.NET Core Identity with email + password.
        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true; // requires confirmed email to log in

                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                // Lockout — pairs with the per-IP HTTP rate limiter in Program.cs.
                // The limiter caps *how fast* an attacker can try; lockout caps the
                // *total* attempts per account within the window.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<HibpPasswordValidator<AppUser>>();

        // HttpClient factory for HibpPasswordValidator.
        services.AddHttpClient();

        // Application-level services implemented by Infrastructure.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICategorySeeder, CategorySeeder>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IDebtService, DebtService>();
        services.AddScoped<IAppEventLogger, AppEventLogger>();

        // Email transport (SMTP via MailKit).
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Seeder framework. Register each ISeeder implementation below; the
        // runner reads them all at startup and executes the pending ones.
        services.AddSingleton<SeederRunner>();
        services.AddScoped<ISeeder, SeedDemoUser>();

        return services;
    }

    /// <summary>
    /// Converts a libpq-style Postgres URI ("postgresql://user:pass@host/db?sslmode=require")
    /// into the ADO.NET key/value format that Npgsql understands. If the input is already in
    /// key/value form, it is returned unchanged.
    /// </summary>
    /// <remarks>
    /// Cloud providers (Neon, Supabase, Heroku, Railway, Cloud Run + Secret Manager)
    /// all hand out URIs. Instead of forcing operators to translate the URI by hand
    /// we normalise it here, so the same string works everywhere.
    /// </remarks>
    private static string ResolvePostgresConnectionString(string raw)
    {
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            // Already in Npgsql key/value format — pass through.
            return raw;
        }

        var uri = new Uri(raw);

        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2)
            throw new ArgumentException(
                "Postgres URI must include both username and password (user:pass@host).",
                nameof(raw));

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1]),
            // Neon (and any managed Postgres over the internet) requires TLS.
            // Npgsql 10 no longer needs TrustServerCertificate — SslMode.Require
            // already skips full chain validation.
            SslMode = SslMode.Require,
        };

        // Copy over query-string parameters we know how to translate.
        if (!string.IsNullOrEmpty(uri.Query))
        {
            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length != 2) continue;

                var key = kv[0].ToLowerInvariant();
                var value = Uri.UnescapeDataString(kv[1]);

                switch (key)
                {
                    case "sslmode":
                        builder.SslMode = value.ToLowerInvariant() switch
                        {
                            "disable"     => SslMode.Disable,
                            "allow"       => SslMode.Allow,
                            "prefer"      => SslMode.Prefer,
                            "require"     => SslMode.Require,
                            "verify-ca"   => SslMode.VerifyCA,
                            "verify-full" => SslMode.VerifyFull,
                            _             => builder.SslMode,
                        };
                        break;

                    case "application_name":
                        builder.ApplicationName = value;
                        break;

                    // Neon adds `channel_binding=require`. Npgsql negotiates this
                    // automatically during the SCRAM handshake when SSL is on, so
                    // there's nothing to configure explicitly — we just ignore it.
                    case "channel_binding":
                        break;
                }
            }
        }

        return builder.ConnectionString;
    }
}
