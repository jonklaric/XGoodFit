using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace XGoodFit.Platform.Tests;

[TestClass]
public sealed class FileOpenRequestsTests
{
    private static readonly string ProjectPath = Path.Combine(Path.GetTempPath(), "Flood study", "Analysis 1.bestfit");

    [TestMethod]
    public void ReadsTheCommandLineAsRmcBestFitDoes()
    {
        CollectionAssert.AreEqual(Array.Empty<string>(), FileOpenRequests.FromCommandLine([]).ToArray());
        CollectionAssert.AreEqual(new[] { ProjectPath }, FileOpenRequests.FromCommandLine([ProjectPath]).ToArray());
        // The Jump List passes a path with spaces unquoted.
        CollectionAssert.AreEqual(new[] { ProjectPath }, FileOpenRequests.FromCommandLine(ProjectPath.Split(' ')).ToArray());
        CollectionAssert.AreEqual(new[] { ProjectPath }, FileOpenRequests.FromCommandLine(["-psn_0_12345", ProjectPath]).ToArray());
        CollectionAssert.AreEqual(Array.Empty<string>(), FileOpenRequests.FromCommandLine(["-psn_0_12345"]).ToArray());
    }

    [TestMethod]
    public void GivesTheStartupFileToTheFirstHandler()
    {
        var requests = new FileOpenRequests([ProjectPath]);
        var first = new List<FileOpenEventArgs>();
        var second = new List<FileOpenEventArgs>();

        requests.FilesOpened += (_, e) => first.Add(e);
        requests.FilesOpened += (_, e) => second.Add(e);

        FileOpenEventArgs request = first.Single();
        Assert.AreEqual(FileOpenSource.CommandLine, request.Source);
        CollectionAssert.AreEqual(new[] { ProjectPath }, request.Paths.ToArray());
        Assert.AreEqual(0, second.Count, "Requests are given once.");
    }

    [TestMethod]
    public void RaisesTheFilesMacOSOpens()
    {
        // As macOS's activatable lifetime raises them (Avalonia doesn't let other code implement one).
        var requests = new FileOpenRequests([]);
        var opened = new List<FileOpenEventArgs>();

        // Finder's files can come before the main window handles them (when they start the application).
        requests.OnActivated(null, new FileActivatedEventArgs([LocalFile(ProjectPath)]));
        requests.FilesOpened += (_, e) => opened.Add(e);
        requests.OnActivated(null, new ActivatedEventArgs(ActivationKind.Reopen));
        requests.OnActivated(null, new FileActivatedEventArgs([LocalFile(ProjectPath), LocalFile(ProjectPath + ".copy")]));

        Assert.AreEqual(2, opened.Count, "Other activations aren't requests to open files.");
        Assert.IsTrue(opened.All(e => e.Source == FileOpenSource.Activation));
        CollectionAssert.AreEqual(new[] { ProjectPath }, opened[0].Paths.ToArray());
        CollectionAssert.AreEqual(new[] { ProjectPath, ProjectPath + ".copy" }, opened[1].Paths.ToArray());
    }

    // A file as Avalonia's storage providers give them (its own class for local files, which isn't public).
    private static IStorageItem LocalFile(string path)
    {
        Type type = typeof(IStorageFile).Assembly.GetType("Avalonia.Platform.Storage.FileIO.BclStorageFile", throwOnError: true)!;
        return (IStorageItem)Activator.CreateInstance(type, new FileInfo(path))!;
    }
}
