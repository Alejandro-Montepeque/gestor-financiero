namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// Single entry point for writing to the <c>app_events</c> table. Callers
/// pass the <paramref name="module"/> (Auth.Login, Api.Categories, Middleware…)
/// and <paramref name="action"/> (Attempt, Success, Failed, Exception…) so the
/// operator can slice the log by concern.
/// </summary>
public interface IAppEventLogger
{
    /// <summary>Record a general event (business or technical, no exception).</summary>
    Task LogAsync(
        string module,
        string action,
        string? message = null,
        string level = "Info",
        string? failureReason = null,
        int? statusCode = null,
        Guid? userId = null,
        string? userEmail = null,
        string? extra = null,
        CancellationToken ct = default);

    /// <summary>Record an exception with its stack trace.</summary>
    Task LogExceptionAsync(
        Exception exception,
        string module,
        string level = "Error",
        int? statusCode = null,
        Guid? userId = null,
        string? userEmail = null,
        string? extra = null,
        CancellationToken ct = default);
}
