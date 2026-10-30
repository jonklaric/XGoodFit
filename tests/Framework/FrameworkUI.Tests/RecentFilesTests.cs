using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FrameworkUI.Tests;

/// <summary>
/// The File menu's recent files: their XML file, the missing files dropped from it, the menu entries
/// and the Recent Files dialog for the files the menu has no room for.
/// </summary>
[TestClass]
public sealed class RecentFilesTests
{
    // The recent-files list an earlier session left: two projects that are still there and one that's gone.
    private static (string Old, string Older) WriteRecentFiles()
    {
        string old = Path.Combine(Shell.Folder, "Old Study.test");
        string gone = Path.Combine(Shell.Folder, "Deleted Study.test");
        string older = Path.Combine(Shell.Folder, "Older Study.test");
        File.WriteAllText(old, string.Empty);
        File.WriteAllText(older, string.Empty);
        Directory.CreateDirectory(ShellPublicVariables.RecentFileListFolderPath);
        new XDocument(new XElement("RecentFiles",
            new XElement("Item", new XAttribute("Path", old)),
            new XElement("Item", new XAttribute("Path", gone)),
            new XElement("Item", new XAttribute("Path", older)))).Save(ShellPublicVariables.RecentFileListFilePath);
        return (old, older);
    }

    private static string[] Saved() =>
        XDocument.Load(ShellPublicVariables.RecentFileListFilePath).Root!.Elements("Item").Select(e => (string)e.Attribute("Path")!).ToArray();

    // The File menu's recent-file entries, which it lists as it opens.
    private static string[] MenuEntries(MainWindow window)
    {
        MenuItem fileMenu = window.RecentFiles.FileMenu!;
        fileMenu.IsSubMenuOpen = true;
        TestInput.Frame();
        int first = fileMenu.Items.IndexOf(window.RecentFiles) + 1;
        string[] entries = fileMenu.Items.Skip(first).TakeWhile(i => i is MenuItem).Select(i => (string)((MenuItem)i!).Header!).ToArray();
        fileMenu.IsSubMenuOpen = false;
        TestInput.Frame();
        return entries;
    }

    [TestMethod]
    public void RecentFiles_ComeFromTheirFile_WithoutMissingFiles_AndOpenFromTheFileMenu()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var (old, older) = WriteRecentFiles();
            var project = ExplorerTests.SampleProject();
            var (window, _) = Shell.Open(project);
            try
            {
                var expected = new[] { project.FullFileName, old, older };
                CollectionAssert.AreEqual(expected, window.RecentFiles.Collection.Select(i => i.FilePath).ToArray(),
                    "the opened project comes first, and the missing file is dropped");
                CollectionAssert.AreEqual(expected, Saved(), "in the file too");

                string[] entries = MenuEntries(window);
                Assert.AreEqual(3, entries.Length, "the File menu lists them after its separator");
                for (int i = 0; i < 3; i++)
                {
                    StringAssert.StartsWith(entries[i], $"_{i + 1}:  ", "numbered, with access keys");
                    StringAssert.EndsWith(entries[i], Path.GetFileNameWithoutExtension(expected[i]), "with the path, shortened, without its extension");
                }

                // Choosing one opens it.
                MenuItem fileMenu = window.RecentFiles.FileMenu!;
                fileMenu.IsSubMenuOpen = true;
                TestInput.Frame();
                var entry = window.RecentFiles.Collection[2].MenuItem!;
                entry.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                fileMenu.IsSubMenuOpen = false;
                TestInput.Frame();
                Assert.AreEqual(older, project.FullFileName, "the chosen project is opened");
                Assert.AreEqual(older, window.RecentFiles.Collection[0].FilePath, "and moves to the top of the list");
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }

    [TestMethod]
    public void MoreFiles_ShowsTheFilesTheMenuHasNoRoomFor()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            WriteRecentFiles();
            var (window, _) = Shell.Open(ExplorerTests.SampleProject());
            try
            {
                // As Tools > Options sets a smaller number of files for the menu.
                window.RecentFiles.NumberOfFilesToDisplay = 2;
                string[] entries = MenuEntries(window);
                Assert.AreEqual(3, entries.Length);
                Assert.AreEqual("More Files...", entries[2], "two files, then More Files...");

                string[]? listed = null;
                using (Dialogs.WhenOpened<RecentFilesDialog>(dialog =>
                {
                    listed = dialog.Files.Collection.Select(f => f.Name).ToArray();
                    dialog.Close();
                }))
                {
                    MenuItem fileMenu = window.RecentFiles.FileMenu!;
                    fileMenu.IsSubMenuOpen = true;
                    TestInput.Frame();
                    var more = fileMenu.Items.OfType<MenuItem>().Single(m => (string?)m.Header == "More Files...");
                    more.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    fileMenu.IsSubMenuOpen = false;
                }
                Assert.AreEqual(3, listed?.Length, "the Recent Files dialog lists them all");
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }
}
