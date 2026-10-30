using System.Runtime.Versioning;
using Velopack;
using Velopack.Locators;

namespace XGoodFit.Platform;

/// <summary>
/// XGoodFit addition: Velopack's startup hook, which the application runs first thing in <c>Main</c>,
/// before anything else starts (decision D7, migration plan §9):
/// <code>PlatformStartup.BuildVelopackApp(args)?.Run();</code>
/// </summary>
/// <remarks>
/// <c>Main</c> calls <c>Run()</c> itself, as Velopack asks: <c>vpk pack</c> refuses a Windows
/// application whose own code doesn't call <c>VelopackApp.Run()</c>.
/// </remarks>
public static class PlatformStartup
{
    /// <summary>
    /// Velopack's startup hook, <c>VelopackApp.Build()</c> with XGoodFit's hooks, when this process
    /// runs from an XGoodFit package; else null. Run it first thing in <c>Main</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Velopack's installer, updater and uninstaller start the application with a hook argument
    /// (<c>--veloapp-install</c>, <c>--veloapp-updated</c>, …) and expect it to exit within seconds:
    /// then <c>Run()</c> runs the hook and exits the process. On Windows those hooks register XGoodFit
    /// as an extra "Open with" handler for <c>.bestfit</c> files, and remove it again on uninstall
    /// (<see cref="WindowsFileAssociation"/>). On a normal start, <c>Run()</c> applies an update that
    /// was downloaded but not installed, and removes old packages.
    /// </para>
    /// <para>
    /// A copy that doesn't run from a package (a development build, or the tests) gets null and skips
    /// the hook, which would only find that the application isn't installed, and write a log file to
    /// the user's folders each time (<c>%LocalAppData%\velopack</c>, <c>~/Library/Logs</c> or the
    /// temporary folder). <see cref="IsPackaged"/> says which it is. A hook argument always gets the hook.
    /// </para>
    /// </remarks>
    /// <param name="args">The command-line arguments <c>Main</c> received.</param>
    /// <returns>The hook to run, or null.</returns>
    public static VelopackApp? BuildVelopackApp(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (VelopackLocator.IsCurrentSet || (!IsPackaged && !HasHookArguments(args)))
            return null;

        VelopackApp app = VelopackApp.Build().SetArgs(args);
        if (OperatingSystem.IsWindows())
            AddWindowsHooks(app);
        return app;
    }

    /// <summary>
    /// True when this process runs from an XGoodFit package: an installation or the portable zip on
    /// Windows, an <c>.app</c> bundle on macOS, or an AppImage on Linux. They're where Velopack's
    /// updater sits beside the application, as Velopack itself looks for it.
    /// </summary>
    public static bool IsPackaged { get; } = DetectPackage(Environment.ProcessPath, Environment.GetEnvironmentVariable("APPIMAGE"));

    /// <summary>Whether <paramref name="processPath"/> runs from a package (see <see cref="IsPackaged"/>).</summary>
    internal static bool DetectPackage(string? processPath, string? appImage)
    {
        if (string.IsNullOrEmpty(processPath))
            return false;
        string folder = Path.GetDirectoryName(processPath) ?? "";
        if (OperatingSystem.IsWindows())
            return File.Exists(Path.Combine(folder, "..", "Update.exe"));
        if (OperatingSystem.IsMacOS())
            return processPath.Contains(".app/Contents/MacOS/", StringComparison.OrdinalIgnoreCase)
                && File.Exists(Path.Combine(folder, "UpdateMac"));
        return !string.IsNullOrEmpty(appImage) && File.Exists(Path.Combine(folder, "UpdateNix"));
    }

    /// <summary>Whether the arguments are a Velopack hook's (which Velopack checks the same way).</summary>
    internal static bool HasHookArguments(string[] args) =>
        args.Length >= 2
        && (args[0].StartsWith("--veloapp-", StringComparison.OrdinalIgnoreCase)
            || args[0].StartsWith("--squirrel-", StringComparison.OrdinalIgnoreCase));

    [SupportedOSPlatform("windows")]
    private static void AddWindowsHooks(VelopackApp app)
    {
        // An update replaces the files the registration points to with new ones at the same paths;
        // registering again keeps it up to date if a later version changes it.
        app.OnAfterInstallFastCallback(_ => WindowsFileAssociation.Register(VelopackLocator.Current))
            .OnAfterUpdateFastCallback(_ => WindowsFileAssociation.Register(VelopackLocator.Current))
            .OnBeforeUninstallFastCallback(_ => WindowsFileAssociation.Unregister());
    }
}
