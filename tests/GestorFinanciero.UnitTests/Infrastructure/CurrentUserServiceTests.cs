using System.Security.Claims;
using GestorFinanciero.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace GestorFinanciero.UnitTests.Infrastructure;

public class CurrentUserServiceTests
{
    [Fact]
    public void GetUserId_returns_the_nameidentifier_claim_as_a_Guid()
    {
        var expected = Guid.NewGuid();
        var sut = MakeService(new Claim(ClaimTypes.NameIdentifier, expected.ToString()));

        Assert.Equal(expected, sut.GetUserId());
    }

    [Fact]
    public void TryGetUserId_returns_null_when_the_claim_is_absent()
    {
        var sut = MakeService(/* no claims */);

        Assert.Null(sut.TryGetUserId());
    }

    [Fact]
    public void TryGetUserId_returns_null_when_the_claim_is_not_a_guid()
    {
        var sut = MakeService(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        Assert.Null(sut.TryGetUserId());
    }

    [Fact]
    public void TryGetUserId_returns_null_when_there_is_no_HttpContext()
    {
        var sut = new CurrentUserService(new StubAccessor(context: null));

        Assert.Null(sut.TryGetUserId());
    }

    [Fact]
    public void GetUserId_throws_when_the_request_is_anonymous()
    {
        var sut = MakeService(/* no claims */);

        Assert.Throws<UnauthorizedAccessException>(() => sut.GetUserId());
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static CurrentUserService MakeService(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: claims.Length > 0 ? "Test" : null);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new CurrentUserService(new StubAccessor(context));
    }

    private sealed class StubAccessor : IHttpContextAccessor
    {
        public StubAccessor(HttpContext? context) => HttpContext = context;
        public HttpContext? HttpContext { get; set; }
    }
}
