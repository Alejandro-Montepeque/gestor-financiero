namespace GestorFinanciero.Domain.Constants;

/// <summary>
/// Well-known values for <c>AppEvent.Module</c>. Using constants instead of
/// enums keeps the door open for ad-hoc modules (e.g. a future integration)
/// without a domain-model change, while still guarding against typos in the
/// call sites we control.
/// </summary>
public static class EventModules
{
    public const string AuthLogin       = "Auth.Login";
    public const string AuthRegister    = "Auth.Register";
    public const string AuthLogout      = "Auth.Logout";
    public const string AuthPassword    = "Auth.Password";
    public const string AuthProfile     = "Auth.Profile";
    public const string AuthEmail       = "Auth.Email";
    public const string AuthSessions    = "Auth.Sessions";
    public const string AuthDelete      = "Auth.Delete";

    public const string ApiCategories   = "Api.Categories";
    public const string ApiTransactions = "Api.Transactions";
    public const string ApiBudgets      = "Api.Budgets";
    public const string ApiDebts        = "Api.Debts";

    public const string Middleware      = "Middleware";
    public const string Seeder          = "Seeder";
}
