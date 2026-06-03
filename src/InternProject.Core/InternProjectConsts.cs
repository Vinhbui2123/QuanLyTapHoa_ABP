using InternProject.Debugging;

namespace InternProject;

public class InternProjectConsts
{
    public const string LocalizationSourceName = "InternProject";

    public const string ConnectionStringName = "Default";

    public const bool MultiTenancyEnabled = false;


    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public static readonly string DefaultPassPhrase =
        DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "b36a423be8994e1cbeafbede73653b5c";
}
