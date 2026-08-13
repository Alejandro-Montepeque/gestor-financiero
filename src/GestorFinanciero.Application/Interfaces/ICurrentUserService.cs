namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// Resolves the currently authenticated user id from the request pipeline.
/// Services depend on this abstraction so they can be exercised in tests
/// without spinning up an HTTP context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Returns the current user id, or throws if the request is anonymous.</summary>
    Guid GetUserId();

    /// <summary>Returns the current user id, or <c>null</c> if the request is anonymous.</summary>
    Guid? TryGetUserId();
}
