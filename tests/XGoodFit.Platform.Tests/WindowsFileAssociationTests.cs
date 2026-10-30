namespace XGoodFit.Platform.Tests;

/// <summary>The registration's keys and values under HKCU\Software\Classes, in a pretend registry (on every OS).</summary>
[TestClass]
public sealed class WindowsFileAssociationTests
{
    private static readonly string Launcher = @"C:\Users\someone\AppData\Local\io.github.jonklaric.xgoodfit\XGoodFit.exe";

    [TestMethod]
    public void RegistersAnExtraOpenWithHandler()
    {
        var registry = new FakeClasses();

        WindowsFileAssociation.Register(registry, Launcher);

        Assert.AreEqual("io.github.jonklaric.xgoodfit.bestfit", WindowsFileAssociation.ProgId);
        Assert.AreEqual("BestFit project", registry.Get("io.github.jonklaric.xgoodfit.bestfit", null));
        Assert.AreEqual($"\"{Launcher}\" \"%1\"", registry.Get(@"io.github.jonklaric.xgoodfit.bestfit\shell\open\command", null));
        Assert.AreEqual($"\"{Launcher}\",0", registry.Get(@"io.github.jonklaric.xgoodfit.bestfit\DefaultIcon", null));
        Assert.AreEqual("", registry.Get(@".bestfit\OpenWithProgids", "io.github.jonklaric.xgoodfit.bestfit"));
        Assert.AreEqual("XGoodFit", registry.Get(@"Applications\XGoodFit.exe", "FriendlyAppName"));
        Assert.AreEqual("", registry.Get(@"Applications\XGoodFit.exe\SupportedTypes", ".bestfit"));
        Assert.IsFalse(registry.Values.Keys.Any(key => key.SubKey.Equals(".bestfit", StringComparison.OrdinalIgnoreCase)),
            ".bestfit's default value picks the default application: RMC-BestFit's installer owns it.");
    }

    [TestMethod]
    public void OnlyNamesTheLauncherInChooseAnotherApp()
    {
        var registry = new FakeClasses();

        WindowsFileAssociation.Register(registry, @"C:\XGoodFit\current\ThemeGallery.exe");

        Assert.AreEqual("\"C:\\XGoodFit\\current\\ThemeGallery.exe\" \"%1\"", registry.Get(@"io.github.jonklaric.xgoodfit.bestfit\shell\open\command", null));
        Assert.IsFalse(registry.Values.Keys.Any(key => key.SubKey.StartsWith("Applications", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void UnregisteringLeavesOtherApplicationsAlone()
    {
        var registry = new FakeClasses();
        registry.SetValue(".bestfit", null, "RMC.BestFit.Project");
        registry.SetValue(@".bestfit\OpenWithProgids", "RMC.BestFit.Project", "");
        WindowsFileAssociation.Register(registry, Launcher);

        WindowsFileAssociation.Unregister(registry);

        CollectionAssert.AreEquivalent(
            new[] { (".bestfit", (string?)null), (@".bestfit\OpenWithProgids", "RMC.BestFit.Project") },
            registry.Values.Keys.Select(key => (key.SubKey, key.Name)).ToArray());
    }

    private sealed class FakeClasses : WindowsFileAssociation.IClassesKey
    {
        public Dictionary<(string SubKey, string? Name), string> Values { get; } = [];

        public string? Get(string subKey, string? name) => Values.GetValueOrDefault((subKey, name));

        public void SetValue(string subKey, string? name, string value) => Values[(subKey, name)] = value;

        public void DeleteValue(string subKey, string name) => Values.Remove((subKey, name));

        public void DeleteTree(string subKey)
        {
            foreach (var key in Values.Keys.Where(key => key.SubKey.Equals(subKey, StringComparison.OrdinalIgnoreCase)
                         || key.SubKey.StartsWith(subKey + @"\", StringComparison.OrdinalIgnoreCase)).ToList())
                Values.Remove(key);
        }
    }
}
