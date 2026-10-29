using System;
using System.Threading;
using System.Windows;

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

                // Load control templates (done once, never changes)
                _controlTemplatesDictionary = new ResourceDictionary
                {
                    Source = new Uri(ThemeResourceHelper.ControlTemplatesUri, UriKind.Absolute)
                };
                app.Resources.MergedDictionaries.Add(_controlTemplatesDictionary);

                // Load initial color palette
                _currentColorDictionary = new ResourceDictionary
                {
                    Source = new Uri(ThemeResourceHelper.GetColorDictionaryUri(initialTheme), UriKind.Absolute)
                };
                app.Resources.MergedDictionaries.Add(_currentColorDictionary);

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
            var newColorDictionary = new ResourceDictionary
            {
                Source = new Uri(ThemeResourceHelper.GetColorDictionaryUri(theme), UriKind.Absolute)
            };

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

            OnThemeChanged(new ThemeChangedEventArgs(oldTheme, theme, newColorDictionary));
        }

        #endregion
    }
}
