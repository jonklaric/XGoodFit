using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Velopack.Locators;

namespace XGoodFit.Platform;

/// <summary>
/// XGoodFit addition: registers XGoodFit for <c>.bestfit</c> files on Windows as an extra "Open with"
/// handler, never as their default (migration plan §1.1): files are shared with RMC-BestFit, whose
/// installer owns the default.
/// </summary>
/// <remarks>
/// <para>
/// Velopack's installer runs <see cref="Register(IVelopackLocator)"/> from its install and update
/// hooks, and <see cref="Unregister()"/> from its uninstall hook (<see cref="PlatformStartup"/>). Everything
/// is per user, under <c>HKEY_CURRENT_USER\Software\Classes</c>, like Velopack's installation:
/// </para>
/// <list type="bullet">
/// <item>A ProgID, <see cref="ProgId"/>, whose open command starts the launcher Velopack keeps in
/// the installation folder (<c>XGoodFit.exe</c>, which starts the current version).</item>
/// <item>The ProgID in <c>.bestfit\OpenWithProgids</c>, which lists it under "Open with".</item>
/// <item><c>Applications\XGoodFit.exe</c>, which names it in "Choose another app".</item>
/// </list>
/// <para>
/// The default value of <c>.bestfit</c>, which picks the default application, is never written, so
/// RMC-BestFit stays the default where it's installed; elsewhere Windows asks which application to use.
/// The portable zip registers nothing.
/// </para>
/// </remarks>
public static class WindowsFileAssociation
{
    /// <summary>The file extension of BestFit projects.</summary>
    public const string Extension = ".bestfit";

    /// <summary>The name file managers show for the file type.</summary>
    public const string TypeName = "BestFit project";

    /// <summary>XGoodFit's ProgID for BestFit projects, from the app ID: <c>io.github.jonklaric.xgoodfit.bestfit</c>.</summary>
    public static string ProgId => ProductInfo.AppId + Extension;

    /// <summary>The launcher Velopack keeps in the installation folder, named after the package's title.</summary>
    internal static string LauncherFileName => ProductInfo.Name + ".exe";

    /// <summary>Registers the installation that <paramref name="locator"/> describes.</summary>
    /// <param name="locator">Velopack's description of this installation.</param>
    [SupportedOSPlatform("windows")]
    public static void Register(IVelopackLocator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        string launcher = Path.Combine(locator.RootAppDir ?? throw new InvalidOperationException("The installation folder is unknown."), LauncherFileName);
        // Velopack names the launcher after the package's title; a package made otherwise still opens files.
        if (!File.Exists(launcher))
            launcher = Environment.ProcessPath ?? throw new InvalidOperationException("The application's path is unknown.");
        using (var classes = new RegistryClasses())
            Register(classes, launcher);
        NotifyShell();
    }

    /// <summary>Removes the registration, leaving other applications' entries for <c>.bestfit</c> as they are.</summary>
    [SupportedOSPlatform("windows")]
    public static void Unregister()
    {
        using (var classes = new RegistryClasses())
            Unregister(classes);
        NotifyShell();
    }

    /// <summary>Writes the registration under <paramref name="classes"/>, for <paramref name="launcherPath"/>.</summary>
    internal static void Register(IClassesKey classes, string launcherPath)
    {
        string command = $"\"{launcherPath}\" \"%1\"";
        string icon = $"\"{launcherPath}\",0";

        classes.SetValue(ProgId, null, TypeName);
        classes.SetValue(ProgId + @"\DefaultIcon", null, icon);
        classes.SetValue(ProgId + @"\shell\open", "FriendlyAppName", ProductInfo.Name);
        classes.SetValue(ProgId + @"\shell\open\command", null, command);

        classes.SetValue(Extension + @"\OpenWithProgids", ProgId, "");

        // "Choose another app" knows applications by their executable's file name. (A Windows path's
        // name, whatever OS the tests run on.)
        string fileName = launcherPath[(launcherPath.LastIndexOfAny(['\\', '/']) + 1)..];
        if (string.Equals(fileName, LauncherFileName, StringComparison.OrdinalIgnoreCase))
        {
            string application = @"Applications\" + LauncherFileName;
            classes.SetValue(application, "FriendlyAppName", ProductInfo.Name);
            classes.SetValue(application + @"\SupportedTypes", Extension, "");
            classes.SetValue(application + @"\shell\open\command", null, command);
        }
    }

    /// <summary>Removes what <see cref="Register(IClassesKey, string)"/> wrote.</summary>
    internal static void Unregister(IClassesKey classes)
    {
        classes.DeleteTree(ProgId);
        classes.DeleteValue(Extension + @"\OpenWithProgids", ProgId);
        classes.DeleteTree(@"Applications\" + LauncherFileName);
    }

    /// <summary>The registry key file types are registered under, as the registration uses it.</summary>
    internal interface IClassesKey
    {
        /// <summary>Sets a string value (<paramref name="name"/> null: the key's default value), creating the key.</summary>
        void SetValue(string subKey, string? name, string value);

        /// <summary>Deletes a value, if the key and value exist.</summary>
        void DeleteValue(string subKey, string name);

        /// <summary>Deletes a key and everything under it, if it exists.</summary>
        void DeleteTree(string subKey);
    }

    [SupportedOSPlatform("windows")]
    private sealed class RegistryClasses : IClassesKey, IDisposable
    {
        private readonly RegistryKey _classes = Registry.CurrentUser.CreateSubKey(@"Software\Classes");

        public void SetValue(string subKey, string? name, string value)
        {
            using RegistryKey key = _classes.CreateSubKey(subKey);
            key.SetValue(name, value);
        }

        public void DeleteValue(string subKey, string name)
        {
            using RegistryKey? key = _classes.OpenSubKey(subKey, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }

        public void DeleteTree(string subKey) => _classes.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);

        public void Dispose() => _classes.Dispose();
    }

    // Tells Explorer that file associations changed, so "Open with" shows the change at once.
    [SupportedOSPlatform("windows")]
    private static void NotifyShell() => SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0;

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);
}
