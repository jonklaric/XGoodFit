using System;

namespace Themes
{
    /// <summary>
    /// Defines the contract for a theme management service that provides
    /// application-wide theme switching capabilities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The theme service is responsible for managing the current application theme
    /// and notifying subscribers when the theme changes. This allows multiple WPF
    /// libraries and components to respond to theme changes independently.
    /// </para>
    /// <para>
    /// Implementations should be thread-safe and support being accessed from
    /// multiple threads, though theme changes should always be applied on the
    /// UI thread.
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
    /// // Subscribe to theme changes
    /// ThemeService.Instance.ThemeChanged += (sender, e) =>
    /// {
    ///     Console.WriteLine($"Theme changed from {e.OldTheme} to {e.NewTheme}");
    /// };
    ///
    /// // Change the theme
    /// ThemeService.Instance.SetTheme(Theme.Dark);
    /// </code>
    /// </example>
    public interface IThemeService
    {
        /// <summary>
        /// Gets the currently active theme.
        /// </summary>
        /// <value>The current <see cref="Theme"/> applied to the application.</value>
        Theme CurrentTheme { get; }

        /// <summary>
        /// Gets a value indicating whether the theme service has been initialized.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Initialize"/> has been called and the service
        /// is ready to manage themes; otherwise, <c>false</c>.
        /// </value>
        bool IsInitialized { get; }

        /// <summary>
        /// Occurs when the application theme has changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This event is raised after the theme color resources have been applied
        /// to the application's merged dictionaries. Subscribers can use this event
        /// to perform additional theme-related updates, such as:
        /// </para>
        /// <list type="bullet">
        ///     <item>Updating third-party controls that don't support <c>DynamicResource</c></item>
        ///     <item>Refreshing cached color values</item>
        ///     <item>Applying theme-specific logic</item>
        /// </list>
        /// <para>
        /// This event is always raised on the UI thread.
        /// </para>
        /// </remarks>
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// Initializes the theme service and applies the specified initial theme.
        /// </summary>
        /// <param name="initialTheme">The theme to apply during initialization.</param>
        /// <remarks>
        /// <para>
        /// This method should be called once during application startup, typically
        /// in the <c>App.xaml.cs</c> constructor or <c>OnStartup</c> method.
        /// </para>
        /// <para>
        /// Calling this method multiple times has no effect after the first call.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called when <c>Application.Current</c> is null.
        /// </exception>
        void Initialize(Theme initialTheme);

        /// <summary>
        /// Changes the application theme to the specified theme.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        /// <remarks>
        /// <para>
        /// This method swaps the color resource dictionary in the application's
        /// merged dictionaries and raises the <see cref="ThemeChanged"/> event.
        /// </para>
        /// <para>
        /// If the specified theme is the same as the current theme, this method
        /// does nothing.
        /// </para>
        /// <para>
        /// This method is thread-safe and will marshal to the UI thread if called
        /// from a background thread.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="Initialize"/> has been called.
        /// </exception>
        void SetTheme(Theme theme);
    }
}
