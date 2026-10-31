using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform.Storage;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: answers the open-file pickers behind WpfCompat's file dialogs as a user who browses to
    /// <see cref="OpenPath"/> does (OxyPlotControls.Tests' FakeFilePicker answers its save pickers the same way).
    /// </summary>
    /// <remarks>
    /// A window's <c>StorageProvider</c> asks Avalonia's locator for an <see cref="IStorageProviderFactory"/> before it
    /// falls back to the platform's (the headless platform's picks nothing). The locator isn't in Avalonia's reference
    /// assemblies, and its storage interfaces can't be implemented in user code, so the factory is registered through
    /// reflection and the provider and file are dispatch proxies. A window keeps the provider it gets first: install the
    /// picker before the main window opens.
    /// </remarks>
    internal sealed class WalkthroughFilePicker : IStorageProviderFactory
    {
        private static WalkthroughFilePicker _instance;
        private readonly IStorageProvider _provider;

        private WalkthroughFilePicker()
        {
            _provider = DispatchProxy.Create<IStorageProvider, Proxy>();
            ((Proxy)(object)_provider).Handler = HandleProvider;
        }

        /// <summary>The picker, registered with Avalonia's locator on first use.</summary>
        public static WalkthroughFilePicker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WalkthroughFilePicker();
                    object locator = typeof(AvaloniaLocator).GetProperty("CurrentMutable", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
                    object helper = locator.GetType().GetMethod("Bind")!.MakeGenericMethod(typeof(IStorageProviderFactory)).Invoke(locator, null)!;
                    helper.GetType().GetMethod("ToConstant")!.MakeGenericMethod(typeof(WalkthroughFilePicker)).Invoke(helper, new object[] { _instance });
                }
                return _instance;
            }
        }

        /// <summary>The file the next open picker returns; null cancels it.</summary>
        public string OpenPath { get; set; }

        /// <summary>The options the last open picker was shown with.</summary>
        public FilePickerOpenOptions LastOpenOptions { get; private set; }

        /// <inheritdoc/>
        public IStorageProvider CreateProvider(TopLevel topLevel) => _provider;

        private object HandleProvider(MethodInfo method, object[] args)
        {
            switch (method.Name)
            {
                case "get_CanOpen":
                case "get_CanSave":
                case "get_CanPickFolder":
                    return true;
                case "OpenFilePickerAsync":
                    LastOpenOptions = (FilePickerOpenOptions)args[0];
                    IReadOnlyList<IStorageFile> files = OpenPath == null ? Array.Empty<IStorageFile>() : new[] { File(OpenPath) };
                    return Task.FromResult(files);
                case "TryGetFolderFromPathAsync":
                case "TryGetWellKnownFolderAsync":
                    return Task.FromResult<IStorageFolder>(null);
                case "TryGetFileFromPathAsync":
                    return Task.FromResult<IStorageFile>(null);
                default:
                    throw new NotSupportedException("The test's file picker doesn't support " + method.Name);
            }
        }

        // A file that is only its path, which WpfCompat's dialogs read (TryGetLocalPath).
        private static IStorageFile File(string path)
        {
            var file = DispatchProxy.Create<IStorageFile, Proxy>();
            ((Proxy)(object)file).Handler = (method, _) => method.Name switch
            {
                "get_Path" => new Uri(Path.GetFullPath(path)),
                "get_Name" => Path.GetFileName(path),
                "get_CanBookmark" => false,
                "Dispose" => null,
                _ => throw new NotSupportedException("The test's file doesn't support " + method.Name),
            };
            return file;
        }

        /// <summary>A dispatch proxy that hands its calls to <see cref="Handler"/>.</summary>
        public class Proxy : DispatchProxy
        {
            /// <summary>Answers the proxy's calls.</summary>
            public Func<MethodInfo, object[], object> Handler { get; set; }

            /// <inheritdoc/>
            protected override object Invoke(MethodInfo targetMethod, object[] args) => Handler(targetMethod, args ?? Array.Empty<object>());
        }
    }
}
