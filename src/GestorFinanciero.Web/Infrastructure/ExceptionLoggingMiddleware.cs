using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Constants;

namespace GestorFinanciero.Web.Infrastructure;

/// <summary>
/// Middleware that catches every unhandled exception, writes it to
/// <see cref="IAppEventLogger"/> (which persists to the <c>app_events</c> table),
/// and then rethrows so the framework's default handler still renders the
/// developer page in dev / <c>/Error</c> in prod.
/// </summary>
public static class ExceptionLoggingMiddleware
{
    public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                try
                {
                    var events = context.RequestServices.GetRequiredService<IAppEventLogger>();
                    await events.LogExceptionAsync(
                        ex,
                        module: EventModules.Middleware,
                        level: EventLevels.Error,
                        statusCode: 500);
                }
                catch
                {
                    // Never let a logging failure mask the original exception.
                }

                throw;
            }
        });
    }
}
