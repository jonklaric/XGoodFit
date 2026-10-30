using Velopack.Locators;

namespace XGoodFit.Platform.Tests;

[TestClass]
public sealed class PlatformStartupTests
{
    [TestMethod]
    public void TheBrandingComesFromDirectoryBuildProps()
    {
        Assert.AreEqual("XGoodFit", ProductInfo.Name);
        Assert.AreEqual("io.github.jonklaric.xgoodfit", ProductInfo.AppId);
        Assert.AreEqual("https://github.com/jonklaric/XGoodFit", ProductInfo.UpdateRepository);
    }

    [TestMethod]
    public void RecognisesVelopacksHookArguments()
    {
        Assert.IsTrue(PlatformStartup.HasHookArguments(["--veloapp-install", "0.1.0"]));
        Assert.IsTrue(PlatformStartup.HasHookArguments(["--VELOAPP-uninstall", "0.1.0"]));
        Assert.IsTrue(PlatformStartup.HasHookArguments(["--squirrel-updated", "0.1.0"]));
        Assert.IsFalse(PlatformStartup.HasHookArguments(["--veloapp-install"]));
        Assert.IsFalse(PlatformStartup.HasHookArguments(["Analysis.bestfit"]));
        Assert.IsFalse(PlatformStartup.HasHookArguments([]));
    }

    [TestMethod]
    public void SkipsVelopackOutsideAPackage()
    {
        Assert.IsFalse(PlatformStartup.IsPackaged, "The test runner isn't packaged.");

        Assert.IsNull(PlatformStartup.BuildVelopackApp(["Analysis.bestfit"]));

        Assert.IsFalse(VelopackLocator.IsCurrentSet, "Velopack wasn't started.");
        Assert.IsNotNull(PlatformStartup.BuildVelopackApp(["--veloapp-install", "0.1.0"]), "A hook argument always gets the hook.");
    }

    [TestMethod]
    public void FindsThisPlatformsPackage()
    {
        string root = Directory.CreateTempSubdirectory("xgf-package-").FullName;
        try
        {
            string exe;
            string updater;
            if (OperatingSystem.IsWindows())
            {
                exe = Path.Combine(root, "current", "XGoodFit.exe");
                updater = Path.Combine(root, "Update.exe");
            }
            else if (OperatingSystem.IsMacOS())
            {
                exe = Path.Combine(root, "XGoodFit.app", "Contents", "MacOS", "XGoodFit");
                updater = Path.Combine(root, "XGoodFit.app", "Contents", "MacOS", "UpdateMac");
            }
            else
            {
                exe = Path.Combine(root, "usr", "bin", "XGoodFit");
                updater = Path.Combine(root, "usr", "bin", "UpdateNix");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
            File.WriteAllText(exe, "");
            string appImage = Path.Combine(root, "XGoodFit.AppImage");

            Assert.IsFalse(PlatformStartup.DetectPackage(exe, appImage), "Without Velopack's updater beside it, it isn't a package.");

            File.WriteAllText(updater, "");
            Assert.IsTrue(PlatformStartup.DetectPackage(exe, appImage));
            if (OperatingSystem.IsLinux())
                Assert.IsFalse(PlatformStartup.DetectPackage(exe, appImage: null), "On Linux, only an AppImage is a package.");
            Assert.IsFalse(PlatformStartup.DetectPackage(null, appImage));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
