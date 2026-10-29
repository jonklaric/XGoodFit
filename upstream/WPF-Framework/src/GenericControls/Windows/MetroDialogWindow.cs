using System.Windows;
using System.Windows.Input;

namespace GenericControls
{
    /// <summary>
    /// A base class for Metro-styled dialog windows with custom chrome and close button only.
    /// Provides automatic command binding setup for the close operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class sets up the necessary command bindings for the MetroDialogStyle defined in the Themes library.
    /// Dialog windows deriving from this class will automatically have a working close button.
    /// </para>
    /// <para>
    /// To use this class:
    /// <list type="number">
    /// <item><description>Create a window that inherits from MetroDialogWindow</description></item>
    /// <item><description>Set the Style to "{DynamicResource MetroDialogStyle}" in XAML</description></item>
    /// <item><description>Ensure the Themes library is initialized before the window is created</description></item>
    /// </list>
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
    public class MetroDialogWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetroDialogWindow"/> class.
        /// Sets up the close command binding for the dialog.
        /// </summary>
        public MetroDialogWindow()
        {
            // Set up command binding for close button
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));

            // Default settings for dialogs
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
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
    }
}
