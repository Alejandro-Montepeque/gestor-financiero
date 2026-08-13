using Microsoft.AspNetCore.Identity;

namespace GestorFinanciero.Infrastructure.Identity;

/// <summary>
/// Application user extended from ASP.NET Core Identity. Uses <see cref="Guid"/> as
/// the primary key so it lines up with the rest of the domain entities.
/// </summary>
/// <remarks>
/// Domain entities reference this user only through <c>UserId</c> (Guid) — they
/// never depend on Identity directly, keeping the Domain layer clean.
/// </remarks>
public class AppUser : IdentityUser<Guid>
{
    /// <summary>Full name shown in the UI (e.g. "Alejandro Montepeque").</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Preferred display currency (ISO 4217, e.g. "USD").</summary>
    public string PreferredCurrency { get; set; } = "USD";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
