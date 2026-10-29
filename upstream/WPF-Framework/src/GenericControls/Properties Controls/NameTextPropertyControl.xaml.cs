using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        }

        #endregion

        #region Members

        /// <summary>
        /// Dependency property for the character limit property.
        /// </summary>
        public static readonly DependencyProperty CharacterLimitProperty = DependencyProperty.Register(nameof(CharacterLimit), typeof(int), typeof(NameTextPropertyControl), new FrameworkPropertyMetadata(64));

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
        public static readonly DependencyProperty ExistingNamesProperty = DependencyProperty.Register(nameof(ExistingNames), typeof(string[]), typeof(NameTextPropertyControl), new FrameworkPropertyMetadata(new string[] { }));

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
        public static readonly DependencyProperty InvalidCharactersProperty = DependencyProperty.Register(nameof(InvalidCharacters), typeof(char[]), typeof(NameTextPropertyControl), new FrameworkPropertyMetadata(NameTextBox.GetDefaultInvalidCharacters()));

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
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(NameTextPropertyControl), new UIPropertyMetadata(""));

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
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(NameTextPropertyControl), new UIPropertyMetadata("Title"));

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
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(NameTextPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));

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
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(NameTextPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));

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
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(NameTextPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));

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
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(NameTextPropertyControl), new UIPropertyMetadata(true));

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
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Updates the <see cref="ActualPropertyWidth"/> when the control is resized.
        /// </summary>
        /// <param name="sender">The control raising the event.</param>
        /// <param name="e">The size change event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement el = sender as FrameworkElement;
            ActualPropertyWidth = el.ActualWidth;
        }

        /// <summary>
        /// Places focus on the text box and sets the caret position.
        /// </summary>
        /// <param name="caretIndex">The desired position of the caret in the text box.</param>
        public void TextBoxFocus(int caretIndex)
        {
            Keyboard.Focus(this.NameTextBox.NameTBox);
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
                ((UIElement)e.OriginalSource).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
    }
}