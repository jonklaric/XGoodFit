using SoftwareUpdate;
using Velopack.Sources;
using SemanticVersion = SoftwareUpdate.SemanticVersion;

namespace XGoodFit.Platform.Tests;

[TestClass]
public sealed class VelopackUpdateServiceTests
{
    private FakeInstallation _installation = null!;

    [TestInitialize]
    public void CreateInstallation() => _installation = new FakeInstallation("1.0.0");

    [TestCleanup]
    public void DeleteInstallation() => _installation.Dispose();

    private VelopackUpdateService CreateService(string version = "1.0.0", bool installed = true)
    {
        UpdateOptions options = VelopackUpdateService.CreateOptions(SemanticVersion.Parse(version));
        options.SkippedVersionsFilePath = Path.Combine(_installation.Root, "skipped_versions.txt");
        return installed
            ? new VelopackUpdateService(options, new SimpleFileSource(new DirectoryInfo(_installation.Feed)), _installation)
            : new VelopackUpdateService(options, source: null, locator: null);
    }

    [TestMethod]
    public void OptionsPointAtXGoodFitsRepository()
    {
        UpdateOptions options = VelopackUpdateService.CreateOptions(SemanticVersion.Parse("0.1.0-dev"));

        Assert.AreEqual("jonklaric", options.GitHubOwner);
        Assert.AreEqual("XGoodFit", options.GitHubRepo);
        Assert.AreEqual("0.1.0-dev", options.CurrentVersion!.ToString());
        Assert.IsTrue(options.IncludePreReleases, "A pre-release follows pre-releases.");
        Assert.IsFalse(VelopackUpdateService.CreateOptions(SemanticVersion.Parse("1.0.0")).IncludePreReleases);
    }

    [TestMethod]
    public async Task ACopyThatIsntInstalledSaysItCantUpdate()
    {
        VelopackUpdateService service = CreateService(installed: false);
        Exception? error = null;
        service.UpdateError += (_, e) => error = e;

        UpdateCheckResult result = await service.CheckForUpdateAsync();

        Assert.IsFalse(service.IsInstalled);
        Assert.IsFalse(result.IsUpdateAvailable);
        Assert.IsNotNull(result.Error);
        StringAssert.Contains(result.Error.Message, "https://github.com/jonklaric/XGoodFit/releases");
        Assert.AreSame(result.Error, error);
        Assert.AreEqual(UpdateState.Error, service.State);
        Assert.AreEqual("1.0.0", result.CurrentVersion!.ToString());
    }

    [TestMethod]
    public async Task FindsANewerRelease()
    {
        _installation.WriteFeed(_installation.Publish("1.0.0"), _installation.Publish("1.1.0", "Fixes."));
        VelopackUpdateService service = CreateService();
        UpdateCheckResult? completed = null;
        service.UpdateCheckCompleted += (_, e) => completed = e;

        UpdateCheckResult result = await service.CheckForUpdateAsync();

        Assert.IsTrue(service.IsInstalled);
        Assert.IsNull(result.Error, result.Error?.ToString());
        Assert.IsTrue(result.IsUpdateAvailable);
        Assert.IsFalse(result.IsSkippedVersion);
        Assert.AreSame(result, completed);
        Assert.AreEqual(UpdateState.UpdateAvailable, service.State);
        UpdateInfo update = result.Update!;
        Assert.AreSame(update, service.AvailableUpdate);
        Assert.AreEqual("1.1.0", update.Version!.ToString());
        Assert.AreEqual("Fixes.", update.ReleaseNotes);
        Assert.AreEqual("XGoodFit 1.1.0", update.Name);
        Assert.AreEqual($"{ProductInfo.AppId}-1.1.0-test-x64-full.nupkg", update.AssetName);
        Assert.AreEqual(64, update.Sha256Checksum!.Length);
        Assert.IsTrue(update.DownloadSize > 0);
        Assert.AreEqual("https://github.com/jonklaric/XGoodFit/releases", update.ReleasePageUrl);
    }

    [TestMethod]
    public async Task IsUpToDateWithoutANewerRelease()
    {
        _installation.WriteFeed(_installation.Publish("1.0.0"));
        VelopackUpdateService service = CreateService();

        UpdateCheckResult result = await service.CheckForUpdateAsync();

        Assert.IsNull(result.Error, result.Error?.ToString());
        Assert.IsFalse(result.IsUpdateAvailable);
        Assert.AreEqual(UpdateState.UpToDate, service.State);
        Assert.IsNull(service.AvailableUpdate);
    }

    [TestMethod]
    public async Task RemembersSkippedVersions()
    {
        _installation.WriteFeed(_installation.Publish("1.1.0"));
        VelopackUpdateService service = CreateService();
        service.SkipVersion(SemanticVersion.Parse("1.1.0+build.7"));

        UpdateCheckResult result = await CreateService().CheckForUpdateAsync();

        Assert.IsTrue(result.IsUpdateAvailable);
        Assert.IsTrue(result.IsSkippedVersion, "Skipped versions are kept on disk, without build metadata.");
        service.ClearSkippedVersions();
        Assert.IsFalse(CreateService().IsVersionSkipped(SemanticVersion.Parse("1.1.0")));
    }

    [TestMethod]
    public async Task DownloadsInstallsAndExits()
    {
        _installation.WriteFeed(_installation.Publish("1.0.0"), _installation.Publish("1.1.0"));
        VelopackUpdateService service = CreateService();
        UpdateCheckResult check = await service.CheckForUpdateAsync();
        var reports = new List<UpdateDownloadProgress>();

        UpdateDownloadResult download = await service.DownloadUpdateAsync(check.Update!, new SynchronousProgress(reports.Add));

        Assert.IsTrue(download.Success, download.Error?.ToString());
        Assert.AreEqual(UpdateState.ReadyToInstall, service.State);
        Assert.AreEqual(Path.Combine(_installation.PackagesDir, check.Update!.AssetName!), download.FilePath);
        Assert.IsTrue(File.Exists(download.FilePath));
        Assert.AreEqual(100, reports[^1].ProgressPercentage);

        service.InstallUpdateAndRestart(download.FilePath!);

        Assert.AreEqual(UpdateState.Installing, service.State);
        Assert.AreEqual(0, _installation.ExitCode, "The application exits for Velopack's updater.");
        (string executable, string[] arguments) = _installation.Started.Single();
        Assert.AreEqual(_installation.UpdateExePath, executable);
        CollectionAssert.IsSubsetOf(new[] { "apply", "--package", download.FilePath, "--waitPid", "4242" }, arguments);
        CollectionAssert.DoesNotContain(arguments, "--norestart", "Velopack restarts the new version.");
    }

    [TestMethod]
    public async Task ReportsAPackageThatDoesntMatchTheFeed()
    {
        Velopack.VelopackAsset release = _installation.Publish("1.1.0");
        _installation.WriteFeed(release with { SHA256 = new string('0', 64) });
        VelopackUpdateService service = CreateService();
        UpdateCheckResult check = await service.CheckForUpdateAsync();

        UpdateDownloadResult download = await service.DownloadUpdateAsync(check.Update!);

        Assert.IsFalse(download.Success);
        Assert.IsNotNull(download.Error);
        Assert.AreEqual(UpdateState.Error, service.State);
        Assert.IsFalse(File.Exists(Path.Combine(_installation.PackagesDir, release.FileName)));
    }

    [TestMethod]
    public async Task OnlyDownloadsTheUpdateItFound()
    {
        _installation.WriteFeed(_installation.Publish("1.1.0"));
        VelopackUpdateService service = CreateService();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.DownloadUpdateAsync(new UpdateInfo()));

        await service.CheckForUpdateAsync();

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.DownloadUpdateAsync(new UpdateInfo { Version = SemanticVersion.Parse("1.1.0") }));
        Assert.ThrowsException<InvalidOperationException>(() => service.InstallUpdateAndRestart(_installation.UpdateExePath));
    }

    /// <summary>Reports progress on the calling thread (<see cref="Progress{T}"/> posts it).</summary>
    private sealed class SynchronousProgress(Action<UpdateDownloadProgress> report) : IProgress<UpdateDownloadProgress>
    {
        public void Report(UpdateDownloadProgress value) => report(value);
    }
}
