namespace GestorFinanciero.Infrastructure.Email;

/// <summary>
/// SMTP configuration bound from the "Smtp" section of IConfiguration.
/// Local dev uses user secrets; production uses env vars / Secret Manager.
/// </summary>
public sealed class SmtpOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Smtp";

    /// <summary>SMTP server host (e.g. "smtp.gmail.com").</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP port. 587 = STARTTLS, 465 = implicit TLS.</summary>
    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Human-readable name shown to the recipient ("Gestor Financiero").</summary>
    public string FromName { get; set; } = "Gestor Financiero";

    /// <summary>The email address that will appear as "From" on the message.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// If true, drop the message into the log instead of hitting the SMTP
    /// server. Useful for local dev when you don't want to configure Gmail.
    /// </summary>
    public bool UseFakeSender { get; set; }
}
