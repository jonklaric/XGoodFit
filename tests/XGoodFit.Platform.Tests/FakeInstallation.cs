using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Velopack;
using Velopack.Locators;

namespace XGoodFit.Platform.Tests;

/// <summary>
/// A pretend Velopack installation of XGoodFit in a temporary folder, with a folder of releases to
/// update from. It records the processes Velopack would start, and the exit it would make.
/// </summary>
internal sealed class FakeInstallation : VelopackLocator, IProcessImpl, IDisposable
{
    public FakeInstallation(string version, string channel = "test-x64")
    {
        Root = Directory.CreateTempSubdirectory("xgf-platform-tests-").FullName;
        Channel = channel;
        CurrentlyInstalledVersion = SemanticVersion.Parse(version);
        Directory.CreateDirectory(PackagesDir);
        Directory.CreateDirectory(AppContentDir);
        Directory.CreateDirectory(Feed);
        // Velopack only starts its updater if it exists.
        File.WriteAllText(UpdateExePath, "");
    }

    /// <summary>The temporary folder: the installation, with the releases in <see cref="Feed"/>.</summary>
    public string Root { get; }

    /// <summary>The folder of releases (a Velopack release folder, as on GitHub).</summary>
    public string Feed => Path.Combine(Root, "releases");

    public override string? AppId => ProductInfo.AppId;

    public override string RootAppDir => Root;

    public override string PackagesDir => Path.Combine(Root, "packages");

    public override string UpdateExePath => Path.Combine(Root, "Update.exe");

    public override string AppContentDir => Path.Combine(Root, "current");

    public override string? Channel { get; }

    public override IProcessImpl Process => this;

    public override SemanticVersion? CurrentlyInstalledVersion { get; }

    /// <summary>The processes Velopack started: executable and arguments.</summary>
    public List<(string Executable, string[] Arguments)> Started { get; } = [];

    /// <summary>The exit code Velopack exited with, if it did.</summary>
    public int? ExitCode { get; private set; }

    /// <summary>
    /// Adds a full release to <see cref="Feed"/>: a package with Velopack's manifest, as
    /// <c>vpk pack</c> makes them.
    /// </summary>
    public VelopackAsset Publish(string version, string notes = "")
    {
        string fileName = $"{ProductInfo.AppId}-{version}-{Channel}-full.nupkg";
        string path = Path.Combine(Feed, fileName);
        using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            using var writer = new StreamWriter(zip.CreateEntry(ProductInfo.AppId + ".nuspec").Open());
            writer.Write($"""
                <?xml version="1.0" encoding="utf-8"?>
                <package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
                  <metadata>
                    <id>{ProductInfo.AppId}</id>
                    <title>{ProductInfo.Name}</title>
                    <version>{version}</version>
                    <channel>{Channel}</channel>
                    <mainExe>app</mainExe>
                  </metadata>
                </package>
                """);
        }

        byte[] bytes = File.ReadAllBytes(path);
        return new VelopackAsset
        {
            PackageId = ProductInfo.AppId,
            Version = SemanticVersion.Parse(version),
            Type = VelopackAssetType.Full,
            FileName = fileName,
            SHA1 = Convert.ToHexString(SHA1.HashData(bytes)),
            SHA256 = Convert.ToHexString(SHA256.HashData(bytes)),
            Size = bytes.Length,
            NotesMarkdown = notes,
        };
    }

    /// <summary>Writes the channel's feed, <c>releases.&lt;channel&gt;.json</c>, as <c>vpk pack</c> does.</summary>
    public void WriteFeed(params VelopackAsset[] releases)
    {
        var feed = new
        {
            Assets = releases.Select(release => new
            {
                release.PackageId,
                Version = release.Version.ToFullString(),
                Type = release.Type.ToString(),
                release.FileName,
                release.SHA1,
                release.SHA256,
                release.Size,
                release.NotesMarkdown,
            }),
        };
        File.WriteAllText(Path.Combine(Feed, $"releases.{Channel}.json"), JsonSerializer.Serialize(feed));
    }

    string IProcessImpl.GetCurrentProcessPath() => Path.Combine(AppContentDir, "app");

    uint IProcessImpl.GetCurrentProcessId() => 4242;

    void IProcessImpl.StartProcess(string exePath, IEnumerable<string> args, string workDir, bool showWindow) =>
        Started.Add((exePath, args.ToArray()));

    void IProcessImpl.Exit(int exitCode) => ExitCode = exitCode;

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
