using System.Security.Claims;
using GestorFinanciero.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GestorFinanciero.Infrastructure.Services;

/// <summary>
/// HTTP-backed implementation of <see cref="ICurrentUserService"/>. Reads the
/// Identity <c>NameIdentifier</c> claim (populated by the cookie handler on
/// every authenticated request) and parses it as a <see cref="Guid"/>.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetUserId()
    {
        return TryGetUserId()
            ?? throw new UnauthorizedAccessException("The current request is not authenticated.");
    }

    public Guid? TryGetUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
