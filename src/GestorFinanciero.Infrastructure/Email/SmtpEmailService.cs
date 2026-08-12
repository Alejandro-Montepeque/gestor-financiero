using GestorFinanciero.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GestorFinanciero.Infrastructure.Email;

/// <summary>
/// <see cref="IEmailService"/> implementation backed by MailKit's SMTP client.
/// Supports Gmail, Resend, SendGrid, Mailgun, or any RFC-compliant SMTP server.
/// </summary>
/// <remarks>
/// If <see cref="SmtpOptions.UseFakeSender"/> is true, no network call is made
/// and the message is dumped to the logger — useful when Gmail credentials
/// aren't configured yet in local dev.
/// </remarks>
public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        CancellationToken cancellationToken = default)
    {
        if (_options.UseFakeSender || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogInformation(
                "[Fake SMTP] To: {To}  Subject: {Subject}\n{Body}",
                toEmail, subject, htmlBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = plainTextBody ?? StripHtml(htmlBody),
        }.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            // Port 587 uses STARTTLS (upgrade plaintext to TLS mid-session);
            // 465 uses implicit TLS from the start. Auto negotiates both.
            await smtp.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.Auto, cancellationToken);
            await smtp.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            await smtp.SendAsync(message, cancellationToken);

            _logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} — subject: {Subject}", toEmail, subject);
            throw;
        }
        finally
        {
            if (smtp.IsConnected)
                await smtp.DisconnectAsync(quit: true, cancellationToken);
        }
    }

    /// <summary>
    /// Very rough HTML → plain text fallback for the TextBody part when the
    /// caller didn't provide one. Good enough for the emails we send.
    /// </summary>
    private static string StripHtml(string html)
    {
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
    }
}
