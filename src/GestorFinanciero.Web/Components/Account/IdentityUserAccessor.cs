using GestorFinanciero.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace GestorFinanciero.Web.Components.Account;

/// <summary>
/// Resolves the currently authenticated <see cref="AppUser"/> from the
/// <see cref="HttpContext"/>. If the user identity cannot be found in the
/// database, the request is short-circuited with a redirect to
/// <c>/Account/InvalidUser</c> so downstream code can always assume a valid user.
/// </summary>
internal sealed class IdentityUserAccessor
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IdentityRedirectManager _redirectManager;

    public IdentityUserAccessor(UserManager<AppUser> userManager, IdentityRedirectManager redirectManager)
    {
        _userManager = userManager;
        _redirectManager = redirectManager;
    }

    public async Task<AppUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await _userManager.GetUserAsync(context.User);

        if (user is null)
        {
            _redirectManager.RedirectToWithStatus(
                "Account/InvalidUser",
                $"Error: Unable to load user with ID '{_userManager.GetUserId(context.User)}'.",
                context);
        }

        return user!;
    }
}
