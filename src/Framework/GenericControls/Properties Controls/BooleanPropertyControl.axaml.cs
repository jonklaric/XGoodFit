using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GenericControls
{
    /// <summary>
    /// A user control that displays a titled checkbox allowing the user to toggle a boolean property.
    /// Includes optional leader line and customizable checked/unchecked events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// XGoodFit port: the properties are Avalonia styled properties. The check box's IsCheckedChanged
    /// event raises <see cref="Checked"/> or <see cref="Unchecked"/>, as WPF's check box does.
    /// </para>
    /// </remarks>
    public partial class BooleanPropertyControl:UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanPropertyControl"/> class.
        /// </summary>
        public BooleanPropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Dependency property for the selected state of the checkbox.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<BooleanPropertyControl, bool>(nameof(IsSelected), true);
        /// <summary>
        /// Gets or sets whether the checkbox is selected.
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return (bool)this.GetValue(IsSelectedProperty);
            }
            set
            {
                this.SetValue(IsSelectedProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the display title associated with the control. 
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<BooleanPropertyControl, string>(nameof(Title), "Title");
        /// <summary>
        /// Gets or sets the title text shown next to the checkbox.
        /// </summary>
        public string Title
        {
            get
            {
                return (string)this.GetValue(TitleProperty);
            }
            set
            {
                this.SetValue(TitleProperty, value);
            }
        }

        /// <summary>
        /// Dependency property to control the visibility of the leader line.
        /// </summary>
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<BooleanPropertyControl, bool>(nameof(ShowLeaderLine), true);
        /// <summary>
        /// Gets or sets whether the leader line should be visible.
        /// </summary>
        public bool ShowLeaderLine
        {
            get
            {
                return (bool)this.GetValue(ShowLeaderLineProperty);
            }
            set
            {
                this.SetValue(ShowLeaderLineProperty, value);
            }
        }

        /// <summary>
        /// Occurs when the checkbox is checked 
        /// </summary>
        public event CheckedEventHandler Checked;

        /// <summary>
        /// Delegate for the <see cref="Checked"/> event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        public delegate void CheckedEventHandler(object sender, RoutedEventArgs e);

        /// <summary>
        /// Occurs when the checkbox is unchecked.
        /// </summary>
        public event UncheckedEventHandler Unchecked;

        /// <summary>
        /// Delegate for the <see cref="Unchecked"/> event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        public delegate void UncheckedEventHandler(object sender, RoutedEventArgs e);

        /// <summary>
        /// Handles the <see cref="Checked"/> event and invokes the event.
        /// </summary>
        /// <param name="sender">The checkbox triggering the event.</param>
        /// <param name="e">The event arguments.</param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Checked?.Invoke(this, e);
        }

        /// <summary>
        /// Handles the <see cref="Unchecked"/> event and invokes the event.
        /// </summary>
        /// <param name="sender">The checkbox triggering the event.</param>
        /// <param name="e">The event arguments.</param>
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Unchecked?.Invoke(this, e);
        }

        /// <summary>
        /// Calls <see cref="CheckBox_Checked"/> or <see cref="CheckBox_Unchecked"/> as the checkbox's state changes.
        /// </summary>
        /// <param name="sender">The checkbox triggering the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>XGoodFit addition: Avalonia's check box has one event for both, IsCheckedChanged.</remarks>
        private void CheckBox_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            bool? isChecked = ((CheckBox)sender).IsChecked;
            if (isChecked == true)
                CheckBox_Checked(sender, e);
            else if (isChecked == false)
                CheckBox_Unchecked(sender, e);
        }
    }
}