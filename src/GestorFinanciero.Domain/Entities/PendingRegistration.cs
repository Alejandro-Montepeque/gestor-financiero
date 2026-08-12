namespace GestorFinanciero.Domain.Entities;

/// <summary>
/// Registration step 1 output: an email + full name that the user has claimed,
/// pending verification of the 6-digit code we mailed them. Only after code
/// verification and password creation does a real <c>AppUser</c> get created.
/// </summary>
/// <remarks>
/// Lifecycle:
///   Register → row created with CodeHash + ExpiresAt (15 min)
///   VerifyCode → row.IsVerified=true, VerificationToken assigned
///   SetPassword → row consumed and deleted, AppUser created
/// The row is also deleted if AttemptsCount reaches the max or ExpiresAt passes.
/// </remarks>
public class PendingRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Normalized email (lowercase, trimmed).</summary>
    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>SHA-256 hex of the 6-digit code. Never store the code plaintext.</summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>Timestamp after which the code no longer works.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Wrong-code attempts. Row is deleted after MaxCodeAttempts.</summary>
    public int AttemptsCount { get; set; }

    /// <summary>Set to true once the code is verified; unlocks the SetPassword step.</summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// One-time token passed to the SetPassword page after successful verification.
    /// Null until the code is verified. Never guessable (Guid).
    /// </summary>
    public Guid? VerificationToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public const int MaxCodeAttempts = 5;
    public const int CodeLifetimeMinutes = 15;
}
