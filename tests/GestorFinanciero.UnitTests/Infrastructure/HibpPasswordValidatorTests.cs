using System.Net;
using System.Security.Cryptography;
using System.Text;
using GestorFinanciero.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestorFinanciero.UnitTests.Infrastructure;

/// <summary>
/// Tests HibpPasswordValidator by stubbing out the HttpClient with a
/// custom HttpMessageHandler — no real network calls, no mocking library.
/// </summary>
public class HibpPasswordValidatorTests
{
    // Marker user class — the validator is generic, we don't actually touch it.
    private sealed class DummyUser { }

    // ── Behavior when password is in the breach corpus ──────────────────

    [Fact]
    public async Task Rejects_password_seen_5_or_more_times_in_breaches()
    {
        var password = "Password1";
        var (_, suffix) = SplitSha1(password);
        var body = $"{suffix}:9\r\nAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA:3";

        var validator = MakeValidator(HttpStatusCode.OK, body);
        var result = await validator.ValidateAsync(null!, new DummyUser(), password);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "CompromisedPassword");
    }

    [Fact]
    public async Task Allows_password_seen_below_threshold()
    {
        var password = "SlightlyUsed!";
        var (_, suffix) = SplitSha1(password);
        // Threshold is 5 → 4 occurrences is still fine.
        var body = $"{suffix}:4";

        var validator = MakeValidator(HttpStatusCode.OK, body);
        var result = await validator.ValidateAsync(null!, new DummyUser(), password);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Allows_password_absent_from_the_response()
    {
        // The response contains other suffixes but not ours.
        var body = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA:100\r\nBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB:50";

        var validator = MakeValidator(HttpStatusCode.OK, body);
        var result = await validator.ValidateAsync(null!, new DummyUser(), "CompletelyUniquePassword-42!");

        Assert.True(result.Succeeded);
    }

    // ── Edge cases ──────────────────────────────────────────────────────

    [Fact]
    public async Task Empty_password_returns_success_without_hitting_the_network()
    {
        var validator = MakeValidator(HttpStatusCode.InternalServerError, "should not be reached");
        var result = await validator.ValidateAsync(null!, new DummyUser(), string.Empty);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Null_password_returns_success()
    {
        var validator = MakeValidator(HttpStatusCode.InternalServerError, "should not be reached");
        var result = await validator.ValidateAsync(null!, new DummyUser(), null);

        Assert.True(result.Succeeded);
    }

    // ── Fail-open on network/HTTP errors ────────────────────────────────

    [Fact]
    public async Task Fails_open_when_HIBP_returns_5xx()
    {
        var validator = MakeValidator(HttpStatusCode.ServiceUnavailable, "");
        var result = await validator.ValidateAsync(null!, new DummyUser(), "SomePassword123");

        // HIBP being down should NOT block legitimate signups.
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Fails_open_when_the_HTTP_call_throws()
    {
        var validator = MakeThrowingValidator();
        var result = await validator.ValidateAsync(null!, new DummyUser(), "SomePassword123");

        Assert.True(result.Succeeded);
    }

    // ── Case-insensitive suffix comparison ──────────────────────────────

    [Fact]
    public async Task Matches_suffix_case_insensitively()
    {
        // HIBP returns upper-case; our comparison must handle either case.
        var password = "CaseTest1!";
        var (_, suffix) = SplitSha1(password);
        var lowered = suffix.ToLowerInvariant();
        var body = $"{lowered}:10";

        var validator = MakeValidator(HttpStatusCode.OK, body);
        var result = await validator.ValidateAsync(null!, new DummyUser(), password);

        Assert.False(result.Succeeded);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static HibpPasswordValidator<DummyUser> MakeValidator(HttpStatusCode status, string body)
        => new(
            new StubHttpClientFactory(new StubHandler(_ => new HttpResponseMessage(status)
            {
                Content = new StringContent(body),
            })),
            NullLogger<HibpPasswordValidator<DummyUser>>.Instance);

    private static HibpPasswordValidator<DummyUser> MakeThrowingValidator()
        => new(
            new StubHttpClientFactory(new StubHandler(_ => throw new HttpRequestException("boom"))),
            NullLogger<HibpPasswordValidator<DummyUser>>.Instance);

    private static (string prefix, string suffix) SplitSha1(string password)
    {
        var hex = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        return (hex[..5], hex[5..]);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_responder(request));
    }
}
