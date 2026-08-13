using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GestorFinanciero.Infrastructure.Identity;

/// <summary>
/// Rejects passwords that appear in the Have I Been Pwned breach database.
/// Uses k-anonymity: only the first 5 hex chars of the SHA-1 hash are sent to
/// HIBP; the rest is compared locally. HIBP never sees the full password.
/// </summary>
/// <remarks>
/// Fails open on network errors — we don't want a HIBP outage to block
/// legitimate signups.
/// </remarks>
public sealed class HibpPasswordValidator<TUser> : IPasswordValidator<TUser>
    where TUser : class
{
    // Passwords seen this many times or more are rejected. Common threshold
    // is 5; too low blocks passwords that only leaked in one small breach.
    private const int RejectThreshold = 5;

    private const string HibpEndpoint = "https://api.pwnedpasswords.com/range/";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HibpPasswordValidator<TUser>> _logger;

    public HibpPasswordValidator(
        IHttpClientFactory httpClientFactory,
        ILogger<HibpPasswordValidator<TUser>> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IdentityResult> ValidateAsync(
        UserManager<TUser> manager,
        TUser user,
        string? password)
    {
        if (string.IsNullOrEmpty(password))
            return IdentityResult.Success;

        var (prefix, suffix) = HashPrefixAndSuffix(password);

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);

            using var response = await client.GetAsync(HibpEndpoint + prefix);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HIBP responded {StatusCode} — allowing password", response.StatusCode);
                return IdentityResult.Success;
            }

            var body = await response.Content.ReadAsStringAsync();
            var count = FindOccurrences(body, suffix);

            if (count >= RejectThreshold)
            {
                _logger.LogInformation("HIBP rejected a compromised password (seen {Count}× in breaches)", count);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "CompromisedPassword",
                    Description = "Esta contraseña apareció en filtraciones públicas. Elegí otra.",
                });
            }

            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            // Fail-open: HIBP being down should NOT block registrations.
            _logger.LogWarning(ex, "HIBP check failed — allowing password");
            return IdentityResult.Success;
        }
    }

    /// <summary>
    /// Splits a password's SHA-1 hex hash into a 5-char prefix (sent to HIBP)
    /// and the remaining 35-char suffix (matched locally in the response).
    /// </summary>
    private static (string prefix, string suffix) HashPrefixAndSuffix(string password)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        var hex = Convert.ToHexString(bytes); // 40 upper-case hex chars
        return (hex[..5], hex[5..]);
    }

    /// <summary>
    /// Scans the HIBP response body — lines of "SUFFIX:COUNT" — for the
    /// user's suffix and returns the count, or 0 if not present.
    /// </summary>
    private static int FindOccurrences(string body, string suffix)
    {
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf(':');
            if (separator == -1) continue;

            var candidateSuffix = line[..separator].Trim();
            if (!string.Equals(candidateSuffix, suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var countPart = line[(separator + 1)..].Trim();
            return int.TryParse(countPart, out var n) ? n : 0;
        }

        return 0;
    }
}
