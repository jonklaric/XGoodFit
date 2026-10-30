namespace WpfCompat;

/// <summary>
/// Where the application keeps per-user files, by each platform's conventions (migration plan §9.1).
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// The folder for per-user application data: <c>%AppData%</c> (roaming) on Windows,
    /// <c>~/Library/Application Support</c> on macOS, and <c>$XDG_CONFIG_HOME</c> (else
    /// <c>~/.config</c>) on Linux.
    /// </summary>
    /// <remarks>
    /// .NET's <c>SpecialFolder.ApplicationData</c> is <c>~/.config</c> on macOS too, which isn't where
    /// macOS applications keep their data.
    /// </remarks>
    public static string ApplicationData { get; } = GetApplicationData();

    private static string GetApplicationData()
    {
        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
        }
        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify);
    }
}
