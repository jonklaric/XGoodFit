using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using WpfCompat.Input;

namespace GenericControls
{
    /// <summary>
    /// A property control for editing text with validation rules such as character limits and disallowed characters.
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
    /// <para>
    /// XGoodFit port: the properties are Avalonia styled properties. Avalonia has no Preview events, so
    /// the key handler tunnels; MoveFocus and TraversalRequest are WpfCompat's.
    /// </para>
    /// </remarks>
    public partial class NameTextPropertyControl :UserControl, INotifyPropertyChanged
    {


        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="NameTextPropertyControl"/> class.
        /// </summary>
        public NameTextPropertyControl()
        {
            InitializeComponent();

            // XGoodFit port: Avalonia has no Preview events; this is the tunnelling KeyUp.
            this.NameTextBox.AddHandler(KeyUpEvent, NameTextBox_PreviewKeyUp, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Gets the name text box.
        /// </summary>
        /// <remarks>
        /// XGoodFit port: upstream's public field for the element named NameTextBox. Avalonia generates
        /// fields without documentation comments (a build warning), so the element is NameTextBoxElement.
        /// </remarks>
        public NameTextBox NameTextBox => this.NameTextBoxElement;

        #endregion

        #region Members

        /// <summary>
        /// Dependency property for the character limit property.
        /// </summary>
        public static readonly StyledProperty<int> CharacterLimitProperty = AvaloniaProperty.Register<NameTextPropertyControl, int>(nameof(CharacterLimit), 64);

        /// <summary>
        /// Maximum number of characters that the name string can contain. Default is 64 characters.
        /// </summary>
        public int CharacterLimit
        {
            get
            {
                return (int)this.GetValue(CharacterLimitProperty);
            }
            set
            {
                this.SetValue(CharacterLimitProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the existing names property.
        /// </summary>
        public static readonly StyledProperty<string[]> ExistingNamesProperty = AvaloniaProperty.Register<NameTextPropertyControl, string[]>(nameof(ExistingNames), new string[] { });

        /// <summary>
        /// Array of strings that are invalid. Default is no invalid strings.
        /// </summary>
        public string[] ExistingNames
        {
            get
            {
                return (string[])this.GetValue(ExistingNamesProperty);
            }
            set
            {
                this.SetValue(ExistingNamesProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the invalid characters property.
        /// </summary>
        public static readonly StyledProperty<char[]> InvalidCharactersProperty = AvaloniaProperty.Register<NameTextPropertyControl, char[]>(nameof(InvalidCharacters), NameTextBox.GetDefaultInvalidCharacters());

        /// <summary>
        /// Array of characters that are invalid. Default is invalid filename characters with the addition of apostrophe, left bracket, and right bracket.
        /// </summary>
        public char[] InvalidCharacters
        {
            get
            {
                return (char[])this.GetValue(InvalidCharactersProperty);
            }
            set
            {
                this.SetValue(InvalidCharactersProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the text property.
        /// </summary>
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<NameTextPropertyControl, string>(nameof(Text), "");

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text
        {
            get
            {
                return (string)this.GetValue(TextProperty);
            }
            set
            {
                this.SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the title property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<NameTextPropertyControl, string>(nameof(Title), "Title");

        /// <summary>
        /// Gets or sets the title.
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
        /// Dependency property for the property width property.
        /// </summary>
        public static readonly StyledProperty<GridLength> PropertyWidthProperty = AvaloniaProperty.Register<NameTextPropertyControl, GridLength>(nameof(PropertyWidth), PropertyDefaults.DefaultPropertyWidth);

        /// <summary>
        /// Gets or sets the width of the property layout column.
        /// </summary>
        public GridLength PropertyWidth
        {
            get
            {
                return (GridLength)this.GetValue(PropertyWidthProperty);
            }
            set
            {
                this.SetValue(PropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the max width property.
        /// </summary>
        public static readonly StyledProperty<double> MaxPropertyWidthProperty = AvaloniaProperty.Register<NameTextPropertyControl, double>(nameof(MaxPropertyWidth), PropertyDefaults.DefaultMaxPropertyWidth);

        /// <summary>
        /// Gets or sets the maximum width of the control.
        /// </summary>
        public double MaxPropertyWidth
        {
            get
            {
                return (double)this.GetValue(MaxPropertyWidthProperty);
            }
            set
            {
                this.SetValue(MaxPropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the min width property.
        /// </summary>
        public static readonly StyledProperty<double> MinPropertyWidthProperty = AvaloniaProperty.Register<NameTextPropertyControl, double>(nameof(MinPropertyWidth), PropertyDefaults.DefaultMinPropertyWidth);

        /// <summary>
        /// Gets or sets the minimum width of the control.
        /// </summary>
        public double MinPropertyWidth
        {
            get
            {
                return (double)this.GetValue(MinPropertyWidthProperty);
            }
            set
            {
                this.SetValue(MinPropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the show leader line property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<NameTextPropertyControl, bool>(nameof(ShowLeaderLine), true);

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

        #endregion
        private double _actualWidth = 0d;
        /// <summary>
        /// Gets the current rendered width of the control.
        /// </summary>
        public double ActualPropertyWidth
        {
            get
            {
                return _actualWidth;
            }
            private set
            {
                if (_actualWidth != value)
                {
                    _actualWidth = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualPropertyWidth)));
                }
            }
        }

        /// <summary>
        /// Event raised when a property value changes.
        /// </summary>
        /// <remarks>XGoodFit port: <c>new</c> hides Avalonia's own PropertyChanged event; this class's
        /// <see cref="INotifyPropertyChanged"/> implementation raises this one.</remarks>
        public new event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Updates the <see cref="ActualPropertyWidth"/> when the control is resized.
        /// </summary>
        /// <param name="sender">The control raising the event.</param>
        /// <param name="e">The size change event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // XGoodFit port: WPF's FrameworkElement and ActualWidth are Avalonia's Control and Bounds.Width.
            Control el = sender as Control;
            ActualPropertyWidth = el.Bounds.Width;
        }

        /// <summary>
        /// Places focus on the text box and sets the caret position.
        /// </summary>
        /// <param name="caretIndex">The desired position of the caret in the text box.</param>
        public void TextBoxFocus(int caretIndex)
        {
            // XGoodFit port: Avalonia's Focus() gives keyboard focus, as WPF's Keyboard.Focus does.
            this.NameTextBox.NameTBox.Focus();
            this.NameTextBox.NameTBox.CaretIndex = caretIndex;
            this.NameTextBox.NameTBox.Focus();
        }

        /// <summary>
        /// Allows traversal to the next control when Enter is pressed in the NameTextBox.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The key event arguments.</param>
        private void NameTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // XGoodFit port: Avalonia's Source is WPF's OriginalSource.
                ((InputElement)e.Source).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
    }
}