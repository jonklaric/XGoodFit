using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        public static DependencyProperty ErrorBeepProperty = DependencyProperty.Register(nameof(ErrorBeep), typeof(bool), typeof(MessageWindowOptions), new UIPropertyMetadata(true));

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
        public static DependencyProperty WarningBeepProperty = DependencyProperty.Register(nameof(WarningBeep), typeof(bool), typeof(MessageWindowOptions), new UIPropertyMetadata(true));

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
        public static DependencyProperty MessageBeepProperty = DependencyProperty.Register(nameof(MessageBeep), typeof(bool), typeof(MessageWindowOptions), new UIPropertyMetadata(false));

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
        public static DependencyProperty EventBeepProperty = DependencyProperty.Register(nameof(EventBeep), typeof(bool), typeof(MessageWindowOptions), new UIPropertyMetadata(false));

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
        public static DependencyProperty ErrorColorProperty = DependencyProperty.Register(nameof(ErrorColor), typeof(SolidColorBrush), typeof(MessageWindowOptions), new UIPropertyMetadata(Brushes.Black));

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
        public static DependencyProperty WarningColorProperty = DependencyProperty.Register(nameof(WarningColor), typeof(SolidColorBrush), typeof(MessageWindowOptions), new UIPropertyMetadata(Brushes.Black));

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
        public static DependencyProperty MessageColorProperty = DependencyProperty.Register(nameof(MessageColor), typeof(SolidColorBrush), typeof(MessageWindowOptions), new UIPropertyMetadata(Brushes.Black));

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
        public static DependencyProperty EventColorProperty = DependencyProperty.Register(nameof(EventColor), typeof(SolidColorBrush), typeof(MessageWindowOptions), new UIPropertyMetadata(Brushes.Black));

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
