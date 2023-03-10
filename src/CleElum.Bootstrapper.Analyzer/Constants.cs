namespace CleElum.Bootstrapper.Analyzer;

public static class Constants {
#region Identifiers
    public const string INITIALIZATION_FAILED_ID = "CE0001";
    public const string ALREADY_INITIALIZED_ID = "CE0002";
#endregion
    
#region Categories
    public const string CATEGORY_BOOTSTRAP = "Bootstrap";
    public const string CATEGORY_PATCH = "Patch";
#endregion

#region Titles
    public const string INITIALIZATION_FAILED_TITLE =
        "Bootstrap initialization of CleElum failed!";
    public const string ALREADY_INITIALIZED_TITLE =
        "Attempted to bootstrap CleElum more than once!";
#endregion

#region MessageFormat
    public const string INITIALIZATION_FAILED_MESSAGE_FORMAT =
        "Bootstrap initialization of CleElum failed: {0}";
    public const string ALREADY_INITIALIZED_MESSAGE_FORMAT =
        "Attempted to bootstrap CleElum more than once!";
#endregion

#region Descriptions
    public const string INITIALIZED_FAILED_DESCRIPTION = "";
    public const string ALREADY_INITIALIZED_DESCRIPTION = "";
#endregion
}
