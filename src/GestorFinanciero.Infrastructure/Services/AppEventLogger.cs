using System.Security.Claims;
using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Constants;
using GestorFinanciero.Domain.Entities;
using GestorFinanciero.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GestorFinanciero.Infrastructure.Services;

/// <summary>
/// Persists every logged event to the <c>app_events</c> table, enriching each
/// row with request context (path, method, IP, user agent, current user).
/// </summary>
public sealed class AppEventLogger : IAppEventLogger
{
    private const int MaxStackTrace = 20_000;

    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AppEventLogger> _logger;

    public AppEventLogger(
        AppDbContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AppEventLogger> logger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task LogAsync(
        string module,
        string action,
        string? message = null,
        string level = EventLevels.Info,
        string? failureReason = null,
        int? statusCode = null,
        Guid? userId = null,
        string? userEmail = null,
        string? extra = null,
        CancellationToken ct = default)
    {
        return WriteAsync(
            module: module,
            action: action,
            level: level,
            message: message ?? $"{module}:{action}",
            exceptionType: null,
            stackTrace: null,
            failureReason: failureReason,
            statusCode: statusCode,
            userId: userId,
            userEmail: userEmail,
            extra: extra,
            ct: ct);
    }

    public Task LogExceptionAsync(
        Exception exception,
        string module,
        string level = EventLevels.Error,
        int? statusCode = null,
        Guid? userId = null,
        string? userEmail = null,
        string? extra = null,
        CancellationToken ct = default)
    {
        return WriteAsync(
            module: module,
            action: EventActions.Exception,
            level: level,
            message: exception.Message,
            exceptionType: exception.GetType().FullName,
            stackTrace: exception.ToString(),
            failureReason: null,
            statusCode: statusCode,
            userId: userId,
            userEmail: userEmail,
            extra: extra,
            ct: ct);
    }

    private async Task WriteAsync(
        string module,
        string action,
        string level,
        string message,
        string? exceptionType,
        string? stackTrace,
        string? failureReason,
        int? statusCode,
        Guid? userId,
        string? userEmail,
        string? extra,
        CancellationToken ct)
    {
        var context = _httpContextAccessor.HttpContext;

        var evt = new AppEvent
        {
            Module = module,
            Action = action,
            Level = level,
            Message = message,
            ExceptionType = exceptionType,
            StackTrace = Truncate(stackTrace, MaxStackTrace),
            FailureReason = failureReason,
            StatusCode = statusCode ?? context?.Response.StatusCode,
            UserId = userId ?? TryGetUserId(context),
            UserEmail = userEmail?.Trim().ToLowerInvariant()
                     ?? context?.User.FindFirstValue(ClaimTypes.Email)
                     ?? context?.User.Identity?.Name,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(context?.Request.Headers.UserAgent.ToString(), 500),
            RequestPath = context?.Request.Path.Value,
            RequestMethod = context?.Request.Method,
            Extra = extra,
        };

        try
        {
            _db.Set<AppEvent>().Add(evt);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception saveEx)
        {
            _logger.LogError(saveEx,
                "Failed to persist AppEvent {Module}/{Action}: {Message}",
                module, action, message);
        }
    }

    private static Guid? TryGetUserId(HttpContext? context)
    {
        var raw = context?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= max ? value : value[..max] + "…[truncated]";
    }
}
