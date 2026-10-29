using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GenericControls
{
    /// <summary>
    /// A user control that displays and allows text entry of a date and time value.
    /// Supports binding, property change notification, and 24-hour formatting.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class DateAndTimeTextBoxControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateAndTimeTextBoxControl"/> class.
        /// </summary>
        public DateAndTimeTextBoxControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Identifies the <see cref="SelectedDateTime"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedDateTimeProperty = DependencyProperty.Register(nameof(SelectedDateTime), typeof(DateTime), typeof(DateAndTimeTextBoxControl), new UIPropertyMetadata(new DateTime(2017, 8, 7, 20, 35, 23), DateChangedCallback));

        /// <summary>
        /// Called when the <see cref="SelectedDateTime"/> property changes.
        /// </summary>
        /// <param name="d">Dependency object that changed.</param>
        /// <param name="e">Event data describing the change.</param>
        private static void DateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(DateAndTimeTextBoxControl))
                return;
            DateAndTimeTextBoxControl thisControl = (DateAndTimeTextBoxControl)d;
            // 
            thisControl.OnDateTimeChanged();
        }

        /// <summary>
        /// Gets or sets the selected date and time for this control.
        /// </summary>
        public DateTime SelectedDateTime
        {
            get
            {
                return (DateTime)this.GetValue(SelectedDateTimeProperty);
            }
            set
            {
                this.SetValue(SelectedDateTimeProperty, value);
            }
        }

        /// <summary>
        /// Raised when a property value changes. Used to notify UI bindings.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Notifies listeners that <see cref="SelectedDateTime"/> has changed.
        /// </summary>
        private void OnDateTimeChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDateTime)));
        }

        /// <summary>
        /// Identifies the <see cref="Is24Hour"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Is24HourProperty = DependencyProperty.Register(nameof(Is24Hour), typeof(bool), typeof(DateAndTimeTextBoxControl), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the time should be displayed in 24-hour format.
        /// </summary>
        public bool Is24Hour
        {
            get
            {
                return (bool)this.GetValue(Is24HourProperty);
            }
            set
            {
                this.SetValue(Is24HourProperty, value);
            }
        }

        /// <summary>
        /// Handles key presses in the text box and updates the binding sources on Enter key press.
        /// </summary>
        /// <param name="sender">The TextBox sending the event.</param>
        /// <param name="e">Key event data.</param>
        private void Textbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                var prop = TextBox.TextProperty;
                var binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding is not null)
                    binding.UpdateSource();
            }
        }
    }
}