using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using FrameworkInterfaces;

namespace FrameworkUI.Tests;

/// <summary>
/// An element's document for the shell's tests: a text block that shows the element's name. It takes
/// keyboard focus when clicked, as the controls of an application's documents do.
/// </summary>
public sealed class TestDocument : UserControl
{
    /// <summary>Creates the document of <paramref name="element"/>.</summary>
    public TestDocument(IElement element)
    {
        Element = element;
        Focusable = true;
        Background = Avalonia.Media.Brushes.Transparent;
        Content = new TextBlock { Text = element.Name, Margin = new Thickness(8) };
        PointerPressed += (sender, e) => Focus();
    }

    /// <summary>The element the document shows.</summary>
    public IElement Element { get; }
}

/// <summary>An element's properties panel for the shell's tests.</summary>
public sealed class TestProperties : UserControl
{
    /// <summary>Creates the properties panel of <paramref name="element"/>.</summary>
    public TestProperties(IElement element)
    {
        Element = element;
        Content = new TextBlock { Text = element.Name + " properties", Margin = new Thickness(8) };
    }

    /// <summary>The element the panel shows.</summary>
    public IElement Element { get; }
}

/// <summary>
/// The application's side of the shell for the tests, as RMC.BestFit.App's MainProjectNode: documents and
/// properties panels for the elements, and a log of the documents and panels the shell closes.
/// </summary>
public sealed class TestController(TestProject project) : FrameworkUIController(project)
{
    /// <summary>The documents the shell has closed.</summary>
    public List<Control> ClosedDocuments { get; } = [];

    /// <summary>The properties panels the shell has closed.</summary>
    public List<Control> ClosedProperties { get; } = [];

    /// <inheritdoc/>
    public override bool CanMultiSelect => false;

    /// <inheritdoc/>
    protected override void DefineProjectExplorerMenuItems()
    {
    }

    /// <inheritdoc/>
    protected override void DefineProjectMenuItems() => _projectMenuItems.Add(new MenuItem { Header = "New Test Element..." });

    /// <inheritdoc/>
    protected override void DefineToolsMenuItems() => _toolsMenuItems.Add(new MenuItem { Header = "Test Tool..." });

    /// <inheritdoc/>
    public override Control GetDocumentControl(IElement element) => new TestDocument(element);

    /// <inheritdoc/>
    public override void DocumentClosed(Control documentControl) => ClosedDocuments.Add(documentControl);

    /// <inheritdoc/>
    public override void PropertiesClosed(Control propertiesControl) => ClosedProperties.Add(propertiesControl);

    /// <inheritdoc/>
    public override Control GetPropertiesControl(Control documentControl) => new TestProperties(((TestDocument)documentControl).Element);

    /// <inheritdoc/>
    public override Control GetPropertiesControl(IElement element) => null!;

    /// <inheritdoc/>
    public override IElement GetControlElement(Control control) => control switch
    {
        TestDocument document => document.Element,
        TestProperties properties => properties.Element,
        _ => null!,
    };
}

/// <summary>
/// A main window for the shell's tests: the user settings, recent files, layout and message log in a
/// temporary folder, no auto-backup, and the window at upstream's normal size rather than maximized.
/// </summary>
public static class Shell
{
    /// <summary>The temporary folder the shell's files go in for the current test.</summary>
    public static string Folder { get; private set; } = string.Empty;

    /// <summary>
    /// Points the shell's files at a new temporary folder, with the given settings saved there. It is in
    /// the tests' folder rather than the system's temporary folder, where the shell takes a project for a
    /// new, unsaved one (MainWindow.SaveProject saves it as a new file).
    /// </summary>
    public static void UseTemporaryFolder(Action? settings = null)
    {
        Folder = Path.Combine(AppContext.BaseDirectory, "ShellTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Folder);
        ShellPublicVariables.UserSettingsFolderPath = Path.Combine(Folder, "UserSettings") + Path.DirectorySeparatorChar;
        ShellPublicVariables.UserSettingsFilePath = ShellPublicVariables.UserSettingsFolderPath + "UserSettings.xml";
        ShellPublicVariables.AvalonDockLayoutFolderPath = Path.Combine(Folder, "Layout") + Path.DirectorySeparatorChar;
        ShellPublicVariables.DefaultAvalonDockLayoutFilePath = ShellPublicVariables.AvalonDockLayoutFolderPath + "AvalonDock.xml";
        ShellPublicVariables.RecentFileListFolderPath = Path.Combine(Folder, "RecentFiles") + Path.DirectorySeparatorChar;
        ShellPublicVariables.RecentFileListFilePath = ShellPublicVariables.RecentFileListFolderPath + "RecentFileList.xml";
        ShellPublicVariables.MessageLogFolderPath = Path.Combine(Folder, "MessageLog") + Path.DirectorySeparatorChar;
        ShellPublicVariables.MessageLogFilePath = ShellPublicVariables.MessageLogFolderPath + "MessageLog.txt";
        ShellPublicVariables.SoftwareExtension = ".test";

        UserSettings.RestoreDefaults();
        UserSettings.CreateAutoRecoverBackup = false;
        settings?.Invoke();
        Directory.CreateDirectory(ShellPublicVariables.UserSettingsFolderPath);
        UserSettings.Save(ShellPublicVariables.UserSettingsFilePath);
    }

    /// <summary>
    /// Creates and shows a main window for <paramref name="project"/>, as upstream's applications do: the
    /// window starts with the project blank, its node styled with TreeViewItemStyle, and then opens the
    /// project's file (in <see cref="Folder"/>) with <see cref="MainWindow.OpenProject"/>.
    /// </summary>
    public static (MainWindow Window, TestController Controller) Open(TestProject project, double width = 1300, double height = 700)
    {
        project.Close();
        project.FullFileName = Path.Combine(Folder, Path.GetFileName(project.FullFileName));
        var window = new MainWindow { WindowState = WindowState.Normal, Width = width, Height = height };
        // Upstream's menu item collections are shared by every main window (dependency property
        // defaults); each test window gets its own, so that one test's items don't carry over.
        window.ProjectMenuItems = [];
        window.ToolsMenuItems = [];
        window.HelpMenuItems = [];
        var controller = new TestController(project) { Theme = (ControlTheme)Application.Current!.FindResource("TreeViewItemStyle")! };
        window.ProjectNode = controller;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.OpenProject(project.FullFileName);
        Dispatcher.UIThread.RunJobs();
        project.Calls.Clear();
        return (window, controller);
    }

    /// <summary>Closes <paramref name="window"/>, unless a test closed it, and deletes the temporary folder.</summary>
    public static void Close(Window window)
    {
        if (window.IsVisible)
        {
            window.Close();
        }
        Dispatcher.UIThread.RunJobs();
        try
        {
            Directory.Delete(Folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
