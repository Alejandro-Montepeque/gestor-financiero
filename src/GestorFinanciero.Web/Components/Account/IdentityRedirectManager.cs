using Microsoft.AspNetCore.Components;

namespace GestorFinanciero.Web.Components.Account;

/// <summary>
/// Server-side helpers for redirecting from Identity form handlers. Works in
/// both static SSR (where <see cref="NavigationManager.NavigateTo(string, bool, bool)"/>
/// throws <c>NavigationException</c> so the framework can emit a 302) and
/// interactive Server mode (where the redirect happens via a forced full-page
/// reload). Also exposes a cookie-backed status-message channel so the target
/// page can render a flash message.
/// </summary>
internal sealed class IdentityRedirectManager
{
    public const string StatusCookieName = "Identity.StatusMessage";
    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    private readonly NavigationManager _navigationManager;

    public IdentityRedirectManager(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public void RedirectTo(string? uri)
    {
        uri = string.IsNullOrEmpty(uri) ? "/" : uri;

        // Prevent open redirects to arbitrary external hosts.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = _navigationManager.ToBaseRelativePath(uri);
        }

        // forceLoad = true works everywhere: static SSR throws NavigationException
        // that the framework converts to a 302; interactive Server performs a full
        // browser navigation and destroys the current circuit.
        _navigationManager.NavigateTo(uri, forceLoad: true);
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = _navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = _navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    private string CurrentPath => _navigationManager.ToAbsoluteUri(_navigationManager.Uri).GetLeftPart(UriPartial.Path);

    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
        => RedirectToWithStatus(CurrentPath, message, context);
}
