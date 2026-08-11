using GestorFinanciero.Domain.Common;

namespace GestorFinanciero.Domain.Entities;

/// <summary>
/// Unified log of everything worth remembering — business events (login,
/// register, logout, CRUD actions, API calls) AND technical errors (exceptions,
/// 4xx/5xx responses). One table, one query surface.
/// </summary>
/// <remarks>
/// Filter by <see cref="Module"/> to slice by concern
/// (e.g. Module='Auth.Login' for login history, Module='Api.Categories' for
/// category API activity, Module='Middleware' for unhandled exceptions).
/// </remarks>
public class AppEvent : BaseEntity
{
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Which part of the app: "Auth.Login", "Api.Categories", "Middleware"…</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>What happened: "Attempt", "Success", "Failed", "Exception"…</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Severity: Info, Warning, Error, Critical.</summary>
    public string Level { get; set; } = "Info";

    /// <summary>Human-readable summary line.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Fully-qualified exception type name (null for non-exception events).</summary>
    public string? ExceptionType { get; set; }

    /// <summary>Full stack trace, truncated to fit the column limit.</summary>
    public string? StackTrace { get; set; }

    /// <summary>Machine-readable failure code: "wrong_password", "user_not_found", "not_found"…</summary>
    public string? FailureReason { get; set; }

    /// <summary>HTTP status returned to the client (200, 400, 404, 500…).</summary>
    public int? StatusCode { get; set; }

    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }

    /// <summary>Free-form JSON payload for anything not covered above.</summary>
    public string? Extra { get; set; }
}
