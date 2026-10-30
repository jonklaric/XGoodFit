using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace WpfCompat;

/// <summary>
/// The common part of WPF's file dialogs (<c>Microsoft.Win32.FileDialog</c>), over Avalonia's storage
/// provider. <see cref="ShowDialog()"/> waits for the user, as in WPF (see <see cref="Modal"/>).
/// </summary>
public abstract class FileDialog
{
    /// <summary>The dialog's title.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// The file types to offer, in WPF's format: pairs of a description and semicolon-separated
    /// patterns, all separated by bars, e.g. <c>"Projects (*.bestfit)|*.bestfit|All files (*.*)|*.*"</c>.
    /// </summary>
    public string Filter { get; set; } = string.Empty;

    /// <summary>The selected file type, counting from 1 as in WPF.</summary>
    public int FilterIndex { get; set; } = 1;

    /// <summary>The selected file's full path; also the suggested name when the dialog opens.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The selected files' full paths.</summary>
    public string[] FileNames { get; protected set; } = [];

    /// <summary>The selected file's name without its folder.</summary>
    public string SafeFileName => Path.GetFileName(FileName);

    /// <summary>The selected files' names without their folders.</summary>
    public string[] SafeFileNames => FileNames.Select(Path.GetFileName).ToArray()!;

    /// <summary>The folder the dialog opens in.</summary>
    public string? InitialDirectory { get; set; }

    /// <summary>The extension added to a file name without one, e.g. <c>"bestfit"</c> or <c>".bestfit"</c>.</summary>
    public string? DefaultExt { get; set; }

    /// <summary>Whether to add <see cref="DefaultExt"/> to a file name without an extension.</summary>
    public bool AddExtension { get; set; } = true;

    /// <summary>Whether the file must exist. Avalonia's dialogs check this themselves.</summary>
    public bool CheckFileExists { get; set; }

    /// <summary>Whether the folder must exist. Avalonia's dialogs check this themselves.</summary>
    public bool CheckPathExists { get; set; } = true;

    /// <summary>Kept for source compatibility: the operating system's dialog remembers its own folder.</summary>
    public bool RestoreDirectory { get; set; }

    /// <summary>Kept for source compatibility: the operating system's dialog validates names itself.</summary>
    public bool ValidateNames { get; set; } = true;

    /// <summary>Shows the dialog for the active window and waits for the user.</summary>
    /// <returns>True if the user chose a file, otherwise false.</returns>
    public bool? ShowDialog() => ShowDialog(null);

    /// <summary>Shows the dialog for <paramref name="owner"/> and waits for the user.</summary>
    /// <returns>True if the user chose a file, otherwise false.</returns>
    public bool? ShowDialog(Window? owner)
    {
        TopLevel topLevel = (owner ?? Modal.ActiveWindow()) as TopLevel
            ?? throw new InvalidOperationException("A file dialog needs a window to belong to.");
        string[] paths = Modal.Wait(PickAsync(topLevel.StorageProvider));
        if (paths.Length == 0)
        {
            return false;
        }
        FileNames = paths;
        FileName = paths[0];
        return true;
    }

    /// <summary>Shows the platform's picker and returns the chosen local paths.</summary>
    protected abstract Task<string[]> PickAsync(IStorageProvider storage);

    /// <summary>The folder to start in: <see cref="InitialDirectory"/>, or the folder of <see cref="FileName"/>.</summary>
    protected async Task<IStorageFolder?> StartFolderAsync(IStorageProvider storage)
    {
        string? folder = !string.IsNullOrEmpty(InitialDirectory) ? InitialDirectory
            : Path.IsPathRooted(FileName) ? Path.GetDirectoryName(FileName) : null;
        return string.IsNullOrEmpty(folder) || !Directory.Exists(folder) ? null : await storage.TryGetFolderFromPathAsync(folder);
    }

    /// <summary><see cref="Filter"/> as Avalonia's file types, in order.</summary>
    protected IReadOnlyList<FilePickerFileType> FileTypes()
    {
        string[] parts = Filter.Split('|');
        var types = new List<FilePickerFileType>();
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            string[] patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            types.Add(new FilePickerFileType(parts[i].Trim()) { Patterns = patterns });
        }
        return types;
    }

    /// <summary><see cref="DefaultExt"/> without a leading dot, or null.</summary>
    protected string? DefaultExtension() =>
        string.IsNullOrEmpty(DefaultExt) || !AddExtension ? null : DefaultExt.TrimStart('.');

    /// <summary>The local paths of chosen storage items.</summary>
    protected static string[] Paths(IEnumerable<IStorageItem> items) =>
        items.Select(i => i.TryGetLocalPath()).OfType<string>().ToArray();
}

/// <summary>A dialog to choose files to open: WPF's <c>Microsoft.Win32.OpenFileDialog</c>.</summary>
public sealed class OpenFileDialog : FileDialog
{
    /// <summary>Whether the user may choose more than one file.</summary>
    public bool Multiselect { get; set; }

    /// <inheritdoc/>
    protected override async Task<string[]> PickAsync(IStorageProvider storage)
    {
        IReadOnlyList<FilePickerFileType> types = FileTypes();
        IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Title,
            AllowMultiple = Multiselect,
            FileTypeFilter = types.Count == 0 ? null : types,
            SuggestedFileType = types.ElementAtOrDefault(FilterIndex - 1),
            SuggestedStartLocation = await StartFolderAsync(storage),
        });
        return Paths(files);
    }
}

/// <summary>A dialog to choose where to save a file: WPF's <c>Microsoft.Win32.SaveFileDialog</c>.</summary>
public sealed class SaveFileDialog : FileDialog
{
    /// <summary>Whether to ask before overwriting an existing file.</summary>
    public bool OverwritePrompt { get; set; } = true;

    /// <summary>Kept for source compatibility: whether to ask before creating a new file.</summary>
    public bool CreatePrompt { get; set; }

    /// <inheritdoc/>
    protected override async Task<string[]> PickAsync(IStorageProvider storage)
    {
        IReadOnlyList<FilePickerFileType> types = FileTypes();
        IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Title,
            SuggestedFileName = string.IsNullOrEmpty(FileName) ? null : Path.GetFileName(FileName),
            DefaultExtension = DefaultExtension(),
            FileTypeChoices = types.Count == 0 ? null : types,
            SuggestedFileType = types.ElementAtOrDefault(FilterIndex - 1),
            ShowOverwritePrompt = OverwritePrompt,
            SuggestedStartLocation = await StartFolderAsync(storage),
        });
        return file is null ? [] : Paths([file]);
    }
}

/// <summary>A dialog to choose folders: WPF's <c>Microsoft.Win32.OpenFolderDialog</c>.</summary>
public sealed class OpenFolderDialog
{
    /// <summary>The dialog's title.</summary>
    public string? Title { get; set; }

    /// <summary>The folder the dialog opens in.</summary>
    public string? InitialDirectory { get; set; }

    /// <summary>Whether the user may choose more than one folder.</summary>
    public bool Multiselect { get; set; }

    /// <summary>The chosen folder's full path.</summary>
    public string FolderName { get; set; } = string.Empty;

    /// <summary>The chosen folders' full paths.</summary>
    public string[] FolderNames { get; private set; } = [];

    /// <summary>Shows the dialog for the active window and waits for the user.</summary>
    public bool? ShowDialog() => ShowDialog(null);

    /// <summary>Shows the dialog for <paramref name="owner"/> and waits for the user.</summary>
    public bool? ShowDialog(Window? owner)
    {
        TopLevel topLevel = (owner ?? Modal.ActiveWindow()) as TopLevel
            ?? throw new InvalidOperationException("A folder dialog needs a window to belong to.");
        string[] paths = Modal.Wait(PickAsync(topLevel.StorageProvider));
        if (paths.Length == 0)
        {
            return false;
        }
        FolderNames = paths;
        FolderName = paths[0];
        return true;
    }

    private async Task<string[]> PickAsync(IStorageProvider storage)
    {
        string? start = !string.IsNullOrEmpty(InitialDirectory) ? InitialDirectory : FolderName;
        IReadOnlyList<IStorageFolder> folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Title,
            AllowMultiple = Multiselect,
            SuggestedStartLocation = string.IsNullOrEmpty(start) || !Directory.Exists(start) ? null : await storage.TryGetFolderFromPathAsync(start),
        });
        return folders.Select(f => f.TryGetLocalPath()).OfType<string>().ToArray();
    }
}
