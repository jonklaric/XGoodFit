using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform.Storage;

namespace OxyPlotControls.Tests;

/// <summary>
/// Answers the file pickers behind WpfCompat's file dialogs, as a user would: the save picker returns
/// <see cref="SavePath"/> (or is cancelled when it's null) and records the options it was shown with.
/// </summary>
/// <remarks>
/// A window's <c>StorageProvider</c> asks Avalonia's locator for an <see cref="IStorageProviderFactory"/>
/// before it falls back to the platform's (the headless platform's picks nothing). The locator isn't in
/// Avalonia's reference assemblies, and its storage interfaces can't be implemented in user code, so the
/// factory is registered through reflection and the provider and file are dispatch proxies. Windows
/// keep the provider they get first, so the picker is installed before the tests show any window.
/// </remarks>
internal sealed class FakeFilePicker : IStorageProviderFactory
{
    private static FakeFilePicker? _instance;
    private readonly IStorageProvider _provider;

    private FakeFilePicker()
    {
        _provider = DispatchProxy.Create<IStorageProvider, Proxy>();
        ((Proxy)(object)_provider).Handler = HandleProvider;
    }

    /// <summary>The picker, registered with Avalonia's locator on first use.</summary>
    public static FakeFilePicker Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new FakeFilePicker();
                object locator = typeof(AvaloniaLocator).GetProperty("CurrentMutable", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
                object helper = locator.GetType().GetMethod("Bind")!.MakeGenericMethod(typeof(IStorageProviderFactory)).Invoke(locator, null)!;
                helper.GetType().GetMethod("ToConstant")!.MakeGenericMethod(typeof(FakeFilePicker)).Invoke(helper, [_instance]);
            }
            return _instance;
        }
    }

    /// <summary>The path the next save picker returns; null cancels it.</summary>
    public string? SavePath { get; set; }

    /// <summary>The options the last save picker was shown with.</summary>
    public FilePickerSaveOptions? LastSaveOptions { get; private set; }

    /// <inheritdoc/>
    public IStorageProvider CreateProvider(TopLevel topLevel) => _provider;

    private object? HandleProvider(MethodInfo method, object?[] args)
    {
        switch (method.Name)
        {
            case "get_CanOpen":
            case "get_CanSave":
            case "get_CanPickFolder":
                return true;
            case "SaveFilePickerAsync":
                LastSaveOptions = (FilePickerSaveOptions)args[0]!;
                return Task.FromResult(SavePath == null ? null : File(SavePath));
            case "TryGetFolderFromPathAsync":
            case "TryGetWellKnownFolderAsync":
                return Task.FromResult<IStorageFolder?>(null);
            case "TryGetFileFromPathAsync":
                return Task.FromResult<IStorageFile?>(null);
            default:
                throw new NotSupportedException("The fake file picker doesn't support " + method.Name);
        }
    }

    // A file that is only its path, which WpfCompat's dialogs read (TryGetLocalPath).
    private static IStorageFile? File(string path)
    {
        var file = DispatchProxy.Create<IStorageFile, Proxy>();
        ((Proxy)(object)file).Handler = (method, _) => method.Name switch
        {
            "get_Path" => new Uri(Path.GetFullPath(path)),
            "get_Name" => Path.GetFileName(path),
            "get_CanBookmark" => false,
            "Dispose" => null,
            _ => throw new NotSupportedException("The fake file doesn't support " + method.Name),
        };
        return file;
    }

    /// <summary>A dispatch proxy that hands its calls to <see cref="Handler"/>.</summary>
    public class Proxy : DispatchProxy
    {
        /// <summary>Answers the proxy's calls.</summary>
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;

        /// <inheritdoc/>
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args ?? []);
    }
}
