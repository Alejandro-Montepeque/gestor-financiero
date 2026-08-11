namespace GestorFinanciero.Domain.Constants;

/// <summary>Common values for <c>AppEvent.Action</c>.</summary>
public static class EventActions
{
    public const string Attempt   = "Attempt";
    public const string Success   = "Success";
    public const string Failed    = "Failed";
    public const string Exception = "Exception";
    public const string Denied    = "Denied";
    public const string Created   = "Created";
    public const string Updated   = "Updated";
    public const string Deleted   = "Deleted";
}

/// <summary>Common values for <c>AppEvent.Level</c>.</summary>
public static class EventLevels
{
    public const string Info     = "Info";
    public const string Warning  = "Warning";
    public const string Error    = "Error";
    public const string Critical = "Critical";
}
