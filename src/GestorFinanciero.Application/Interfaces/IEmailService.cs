namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// Transport-agnostic email sender. Callers hand over the recipient,
/// subject and HTML body; the concrete implementation (SMTP, Resend API,
/// SendGrid, etc.) takes care of the delivery.
/// </summary>
public interface IEmailService
{
    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        CancellationToken cancellationToken = default);
}
