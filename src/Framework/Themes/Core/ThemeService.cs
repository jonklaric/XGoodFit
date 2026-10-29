using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

#nullable enable

namespace Themes
{
    /// <summary>
    /// Provides a thread-safe singleton service for managing application themes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="ThemeService"/> class is the central hub for theme management
    /// in applications using this library. It maintains the current theme state,
    /// manages theme resource dictionaries, and notifies subscribers of theme changes.
    /// </para>
    /// <para>
    /// This class implements the singleton pattern using <see cref="Lazy{T}"/> with
    /// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> to ensure thread-safe
    /// initialization. All public members are thread-safe.
    /// </para>
    /// <para>
    ///     <b>Usage:</b>
    /// </para>
    /// <para>
    /// During application startup, call <see cref="Initialize"/> with the desired
    /// initial theme. This loads the control templates and applies the initial color
    /// palette.
    /// </para>
    /// <para>
    /// To change themes at runtime, call <see cref="SetTheme"/>. All controls using
    /// <c>DynamicResource</c> bindings will automatically update.
    /// </para>
    /// <para>
    ///     <b>Authors:</b>
    ///     <list type="bullet">
    ///         <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    ///     </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In App.xaml.cs OnStartup
    /// protected override void OnStartup(StartupEventArgs e)
    /// {
    ///     base.OnStartup(e);
    ///
    ///     // Initialize with the user's preferred theme
    ///     ThemeService.Instance.Initialize(Theme.Light);
    /// }
    ///
    /// // In options dialog
    /// private void ApplyTheme_Click(object sender, RoutedEventArgs e)
    /// {
    ///     ThemeService.Instance.SetTheme(Theme.Dark);
    /// }
    /// </code>
    /// </example>
    public sealed class ThemeService : IThemeService
    {
        #region Singleton Implementation

        /// <summary>
        /// Lazy initializer for thread-safe singleton creation.
        /// </summary>
        private static readonly Lazy<ThemeService> _instance =
            new Lazy<ThemeService>(() => new ThemeService(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of the <see cref="ThemeService"/>.
        /// </summary>
        /// <value>The singleton <see cref="ThemeService"/> instance.</value>
        public static ThemeService Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the <see cref="ThemeService"/> class.
        /// </summary>
        private ThemeService()
        {
            _currentTheme = Theme.Light;
        }

        #endregion

        #region Fields

        /// <summary>
        /// Lock object for thread-safe state access.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The current theme.
        /// </summary>
        private Theme _currentTheme;

        /// <summary>
        /// Indicates whether the service has been initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// The currently loaded control templates resource dictionary.
        /// Null until <see cref="Initialize"/> is called.
        /// </summary>
        private ResourceDictionary? _controlTemplatesDictionary;

        /// <summary>
        /// The currently loaded color resource dictionary.
        /// Null until <see cref="Initialize"/> is called.
        /// </summary>
        private ResourceDictionary? _currentColorDictionary;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public Theme CurrentTheme
        {
            get
            {
                lock (_lock)
                {
                    return _currentTheme;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _isInitialized;
                }
            }
        }

        #endregion

        #region Events

        /// <inheritdoc/>
        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// Raises the <see cref="ThemeChanged"/> event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            ThemeChanged?.Invoke(this, e);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Initialize(Theme initialTheme)
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    return;
                }

                var app = Application.Current;
                if (app == null)
                {
                    throw new InvalidOperationException(
                        "Cannot initialize ThemeService when Application.Current is null. " +
                        "Ensure this method is called after the Application object has been created.");
                }

                // XGoodFit port: give controls WPF's defaults where Avalonia's differ: property
                // defaults (WpfDefaults), and styles such as the default font (Resources/Defaults.axaml).
                WpfDefaults.Apply();
                app.Styles.Add((IStyle)AvaloniaXamlLoader.Load(new Uri(ThemeResourceHelper.DefaultsUri, UriKind.Absolute)));

                // Load control templates (done once, never changes)
                _controlTemplatesDictionary = LoadDictionary(ThemeResourceHelper.ControlTemplatesUri);
                app.Resources.MergedDictionaries.Add(_controlTemplatesDictionary);

                // Load initial color palette
                _currentColorDictionary = LoadDictionary(ThemeResourceHelper.GetColorDictionaryUri(initialTheme));
                app.Resources.MergedDictionaries.Add(_currentColorDictionary);
                app.RequestedThemeVariant = GetThemeVariant(initialTheme);

                _currentTheme = initialTheme;
                _isInitialized = true;
            }
        }

        /// <inheritdoc/>
        public void SetTheme(Theme theme)
        {
            // Check if we need to marshal to UI thread
            var app = Application.Current;
            if (app == null)
            {
                throw new InvalidOperationException(
                    "Cannot set theme when Application.Current is null.");
            }

            if (!app.Dispatcher.CheckAccess())
            {
                // Use Invoke (synchronous) so any exception thrown on the UI thread surfaces
                // back to the original caller. BeginInvoke would fire-and-forget, swallowing
                // failures and making caller-side error handling impossible.
                app.Dispatcher.Invoke(() => SetTheme(theme));
                return;
            }

            // Build the new dictionary outside the lock. Loading XAML via pack URI can be
            // slow and can recursively touch other resource dictionaries; doing it under
            // the lock would extend the critical section unnecessarily.
            var newColorDictionary = LoadDictionary(ThemeResourceHelper.GetColorDictionaryUri(theme));

            // Capture state under the lock, but DO NOT touch MergedDictionaries here.
            // WPF resource invalidation can synchronously fire callbacks into subscribers
            // that re-enter the CurrentTheme getter (which acquires _lock). Holding _lock
            // during MergedDictionaries.Remove/Add would risk a deadlock; mutate after the
            // lock is released. The whole method is already serialized on the UI thread
            // via the BeginInvoke marshal above, so MergedDictionaries access is single-
            // threaded even after lock release.
            Theme oldTheme;
            ResourceDictionary? oldColorDictionary;
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    throw new InvalidOperationException(
                        "ThemeService has not been initialized. Call Initialize() before SetTheme().");
                }

                if (_currentTheme == theme)
                {
                    return;
                }

                oldTheme = _currentTheme;
                oldColorDictionary = _currentColorDictionary;

                _currentTheme = theme;
                _currentColorDictionary = newColorDictionary;
            }

            // Mutate MergedDictionaries and raise the event outside the lock so that
            // subscribers (and synchronous WPF resource-invalidation callbacks) can
            // safely call back into the service.
            if (oldColorDictionary != null)
            {
                app.Resources.MergedDictionaries.Remove(oldColorDictionary);
            }
            app.Resources.MergedDictionaries.Add(newColorDictionary);
            app.RequestedThemeVariant = GetThemeVariant(theme);

            OnThemeChanged(new ThemeChangedEventArgs(oldTheme, theme, newColorDictionary));
        }

        #endregion

        #region Avalonia Support

        /// <summary>
        /// Loads a compiled theme resource dictionary.
        /// </summary>
        /// <param name="uri">The <c>avares://</c> URI of the dictionary.</param>
        /// <returns>The loaded dictionary.</returns>
        /// <remarks>
        /// XGoodFit port: replaces WPF's <c>new ResourceDictionary { Source = uri }</c>.
        /// </remarks>
        private static ResourceDictionary LoadDictionary(string uri)
        {
            return (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri(uri, UriKind.Absolute));
        }

        /// <summary>
        /// Gets the Avalonia theme variant that matches a theme.
        /// </summary>
        /// <param name="theme">The theme.</param>
        /// <returns><see cref="ThemeVariant.Dark"/> for <see cref="Theme.Dark"/>; otherwise <see cref="ThemeVariant.Light"/>.</returns>
        /// <remarks>
        /// XGoodFit port: WPF has no equivalent. Setting the application's variant keeps the base
        /// templates of any control without a ported theme consistent with the palette.
        /// </remarks>
        private static ThemeVariant GetThemeVariant(Theme theme)
        {
            return theme == Theme.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        #endregion
    }
}
