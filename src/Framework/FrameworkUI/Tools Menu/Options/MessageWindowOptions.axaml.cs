using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FrameworkUI
{
    /// <summary>
    /// Interaction logic for MessageWindowOptions.xaml providing message window settings configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// <para>
    /// XGoodFit port: the dependency properties are styled properties with the same defaults; WPF's black brush is a
    /// new black SolidColorBrush, as Avalonia's Brushes.Black is immutable.
    /// </para>
    /// </remarks>
    public partial class MessageWindowOptions : UserControl
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageWindowOptions"/> class.
        /// </summary>
        public MessageWindowOptions()
        {
            // This call is required by the designer.
            InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
        }

        /// <summary>
        /// Dependency property for the error beep boolean.
        /// </summary>
        public static readonly StyledProperty<bool> ErrorBeepProperty = AvaloniaProperty.Register<MessageWindowOptions, bool>(nameof(ErrorBeep), true);

        /// <summary>
        /// Gets or sets whether error messages beep.
        /// </summary>
        public bool ErrorBeep
        {
            get { return (bool)GetValue(ErrorBeepProperty); }
            set { SetValue(ErrorBeepProperty, value); }
        }

        /// <summary>
        /// Dependency property for the warning beep boolean.
        /// </summary>
        public static readonly StyledProperty<bool> WarningBeepProperty = AvaloniaProperty.Register<MessageWindowOptions, bool>(nameof(WarningBeep), true);

        /// <summary>
        /// Gets or sets whether warning messages beep.
        /// </summary>
        public bool WarningBeep
        {
            get { return (bool)GetValue(WarningBeepProperty); }
            set { SetValue(WarningBeepProperty, value); }
        }

        /// <summary>
        /// Dependency property for the beep boolean.
        /// </summary>
        public static readonly StyledProperty<bool> MessageBeepProperty = AvaloniaProperty.Register<MessageWindowOptions, bool>(nameof(MessageBeep), false);

        /// <summary>
        /// Gets or sets whether messages beep.
        /// </summary>
        public bool MessageBeep
        {
            get { return (bool)GetValue(MessageBeepProperty); }
            set { SetValue(MessageBeepProperty, value); }
        }

        /// <summary>
        /// Dependency property for the event beep boolean.
        /// </summary>
        public static readonly StyledProperty<bool> EventBeepProperty = AvaloniaProperty.Register<MessageWindowOptions, bool>(nameof(EventBeep), false);

        /// <summary>
        /// Gets or sets whether event messages beep.
        /// </summary>
        public bool EventBeep
        {
            get { return (bool)GetValue(EventBeepProperty); }
            set { SetValue(EventBeepProperty, value); }
        }

        /// <summary>
        /// Dependency property for the error message color.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> ErrorColorProperty = AvaloniaProperty.Register<MessageWindowOptions, SolidColorBrush>(nameof(ErrorColor), new SolidColorBrush(Colors.Black));

        /// <summary>
        /// Gets or sets the error message color.
        /// </summary>
        public SolidColorBrush ErrorColor
        {
            get { return (SolidColorBrush)GetValue(ErrorColorProperty); }
            set { SetValue(ErrorColorProperty, value); }
        }

        /// <summary>
        /// Dependency property for the warning message color.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> WarningColorProperty = AvaloniaProperty.Register<MessageWindowOptions, SolidColorBrush>(nameof(WarningColor), new SolidColorBrush(Colors.Black));

        /// <summary>
        /// Gets or sets the warning message color.
        /// </summary>
        public SolidColorBrush WarningColor
        {
            get { return (SolidColorBrush)GetValue(WarningColorProperty); }
            set { SetValue(WarningColorProperty, value); }
        }

        /// <summary>
        /// Dependency property for the message color.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> MessageColorProperty = AvaloniaProperty.Register<MessageWindowOptions, SolidColorBrush>(nameof(MessageColor), new SolidColorBrush(Colors.Black));

        /// <summary>
        /// Gets or sets the message color.
        /// </summary>
        public SolidColorBrush MessageColor
        {
            get { return (SolidColorBrush)GetValue(MessageColorProperty); }
            set { SetValue(MessageColorProperty, value); }
        }

        /// <summary>
        /// Dependency property for the event message color.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> EventColorProperty = AvaloniaProperty.Register<MessageWindowOptions, SolidColorBrush>(nameof(EventColor), new SolidColorBrush(Colors.Black));

        /// <summary>
        /// Gets or sets the event message color.
        /// </summary>
        public SolidColorBrush EventColor
        {
            get { return (SolidColorBrush)GetValue(EventColorProperty); }
            set { SetValue(EventColorProperty, value); }
        }

    }
}
