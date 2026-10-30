using Avalonia;
using Avalonia.Controls;
using WpfCompat;
using WpfCompat.Input;

namespace GenericControls
{
    /// <summary>
    /// A base class for Metro-styled windows with custom chrome and window buttons.
    /// Provides automatic command binding setup for minimize, maximize, restore, and close operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class sets up the necessary command bindings for the MetroWindowStyle defined in the Themes library.
    /// Windows deriving from this class will automatically have working minimize, maximize, restore, and close buttons.
    /// </para>
    /// <para>
    /// To use this class:
    /// <list type="number">
    /// <item><description>Create a window that inherits from MetroWindow</description></item>
    /// <item><description>Set the Theme to "{DynamicResource MetroWindowStyle}" in XAML</description></item>
    /// <item><description>Ensure the Themes library is initialized before the window is created</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// XGoodFit port: WPF's <c>Window.ResizeMode</c> is WpfCompat's <see cref="WpfWindow.ResizeModeProperty"/>,
    /// which this class takes on as its own property, so XAML and code can set it as before. The command
    /// bindings are WpfCompat's routed commands; C# needs <c>this.</c> to reach their CommandBindings.
    /// </para>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MetroWindow : Window
    {
        /// <summary>
        /// Defines the <see cref="ResizeMode"/> property: WPF's <c>Window.ResizeMode</c>.
        /// </summary>
        public static readonly StyledProperty<ResizeMode> ResizeModeProperty =
            WpfWindow.ResizeModeProperty.AddOwner<MetroWindow>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MetroWindow"/> class.
        /// Sets up command bindings for window operations.
        /// </summary>
        public MetroWindow()
        {
            // Set up command bindings for window chrome buttons
            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, OnCanResizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, OnCanMinimizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestoreWindow, OnCanResizeWindow));
        }

        /// <summary>
        /// Gets or sets how the window can be resized.
        /// </summary>
        public ResizeMode ResizeMode
        {
            get => GetValue(ResizeModeProperty);
            set => SetValue(ResizeModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the window that owns this one, as WPF's <c>Window.Owner</c>.
        /// </summary>
        /// <remarks>
        /// XGoodFit port: Avalonia sets a window's owner only when it's shown. Setting this records the
        /// owner that <c>ShowDialog()</c> (WpfCompat's <see cref="DialogWindows"/>) shows the window for.
        /// </remarks>
        public new WindowBase Owner
        {
            get => base.Owner ?? DialogWindows.GetRequestedOwner(this);
            set => DialogWindows.SetRequestedOwner(this, value as Window);
        }

        /// <summary>
        /// Handles the close window command.
        /// </summary>
        /// <param name="sender">The command source.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        /// <summary>
        /// Handles the maximize window command.
        /// </summary>
        /// <param name="sender">The command source.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        /// <summary>
        /// Handles the minimize window command.
        /// </summary>
        /// <param name="sender">The command source.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        /// <summary>
        /// Handles the restore window command.
        /// </summary>
        /// <param name="sender">The command source.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        /// <summary>
        /// Determines whether the window can be resized.
        /// </summary>
        /// <param name="sender">The command source.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        /// <summary>
        /// Determines whether the window can be minimized.
        /// </summary>
        /// <param name="sender">The command source.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode != ResizeMode.NoResize;
        }
    }
}
