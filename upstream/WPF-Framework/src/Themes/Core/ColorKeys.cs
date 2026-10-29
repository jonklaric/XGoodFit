namespace Themes
{
    /// <summary>
    /// Provides constants for all standardized theme color resource keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class documents all color resource keys that are defined in the theme
    /// color dictionaries. These keys follow the Visual Studio 2013 naming conventions
    /// and can be used with <c>DynamicResource</c> bindings in XAML.
    /// </para>
    /// <para>
    /// The naming convention follows the pattern: <c>Control.State.Property</c>
    /// where:
    /// </para>
    /// <list type="bullet">
    ///     <item><b>Control</b>: The control or component name (e.g., Button, TextBox)</item>
    ///     <item><b>State</b>: The visual state (e.g., Static, MouseOver, Pressed, Disabled)</item>
    ///     <item><b>Property</b>: The property being styled (e.g., Background, Border, Text)</item>
    /// </list>
    /// <para>
    ///     <b>Authors:</b>
    ///     <list type="bullet">
    ///         <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    ///     </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="xaml">
    /// &lt;Button Background="{DynamicResource Button.Static.Background}"
    ///         BorderBrush="{DynamicResource Button.Static.Border}"/&gt;
    /// </code>
    /// </example>
    public static class ColorKeys
    {
        #region Environment / General

        /// <summary>
        /// Main window background color.
        /// </summary>
        public const string EnvironmentWindowBackground = "EnvironmentWindowBackground";

        /// <summary>
        /// Plot window background color. Same as EnvironmentWindowBackground for light and blue themes,
        /// but slightly lighter for dark theme to improve visibility of plot labels.
        /// </summary>
        public const string PlotWindowBackground = "PlotWindowBackground";

        /// <summary>
        /// Main window text color.
        /// </summary>
        public const string EnvironmentWindowText = "EnvironmentWindowText";

        /// <summary>
        /// Tool window text color.
        /// </summary>
        public const string EnvironmentToolWindowText = "EnvironmentToolWindowText";

        /// <summary>
        /// Tool window disabled text color.
        /// </summary>
        public const string EnvironmentToolWindowDisabledText = "EnvironmentToolWindowDisabledText";

        /// <summary>
        /// Tooltip background color.
        /// </summary>
        public const string EnvironmentToolTipBackground = "EnvironmentToolTipBackground";

        /// <summary>
        /// Tooltip border color.
        /// </summary>
        public const string EnvironmentToolTipBorder = "EnvironmentToolTipBorder";

        /// <summary>
        /// Tooltip text color.
        /// </summary>
        public const string EnvironmentToolTipText = "EnvironmentToolTipText";

        #endregion

        #region Button

        /// <summary>
        /// Button static/default background color.
        /// </summary>
        public const string ButtonStaticBackground = "Button.Static.Background";

        /// <summary>
        /// Button static/default border color.
        /// </summary>
        public const string ButtonStaticBorder = "Button.Static.Border";

        /// <summary>
        /// Button mouse-over background color.
        /// </summary>
        public const string ButtonMouseOverBackground = "Button.MouseOver.Background";

        /// <summary>
        /// Button mouse-over border color.
        /// </summary>
        public const string ButtonMouseOverBorder = "Button.MouseOver.Border";

        /// <summary>
        /// Button pressed background color.
        /// </summary>
        public const string ButtonPressedBackground = "Button.Pressed.Background";

        /// <summary>
        /// Button pressed border color.
        /// </summary>
        public const string ButtonPressedBorder = "Button.Pressed.Border";

        /// <summary>
        /// Button disabled background color.
        /// </summary>
        public const string ButtonDisabledBackground = "Button.Disabled.Background";

        /// <summary>
        /// Button disabled border color.
        /// </summary>
        public const string ButtonDisabledBorder = "Button.Disabled.Border";

        /// <summary>
        /// Button disabled foreground/text color.
        /// </summary>
        public const string ButtonDisabledForeground = "Button.Disabled.Foreground";

        #endregion

        #region CheckBox / OptionMark

        /// <summary>
        /// CheckBox/RadioButton static background color.
        /// </summary>
        public const string OptionMarkStaticBackground = "OptionMark.Static.Background";

        /// <summary>
        /// CheckBox/RadioButton static border color.
        /// </summary>
        public const string OptionMarkStaticBorder = "OptionMark.Static.Border";

        /// <summary>
        /// CheckBox/RadioButton static glyph (checkmark) color.
        /// </summary>
        public const string OptionMarkStaticGlyph = "OptionMark.Static.Glyph";

        /// <summary>
        /// CheckBox/RadioButton mouse-over background color.
        /// </summary>
        public const string OptionMarkMouseOverBackground = "OptionMark.MouseOver.Background";

        /// <summary>
        /// CheckBox/RadioButton mouse-over border color.
        /// </summary>
        public const string OptionMarkMouseOverBorder = "OptionMark.MouseOver.Border";

        /// <summary>
        /// CheckBox/RadioButton mouse-over glyph color.
        /// </summary>
        public const string OptionMarkMouseOverGlyph = "OptionMark.MouseOver.Glyph";

        /// <summary>
        /// CheckBox/RadioButton pressed background color.
        /// </summary>
        public const string OptionMarkPressedBackground = "OptionMark.Pressed.Background";

        /// <summary>
        /// CheckBox/RadioButton pressed border color.
        /// </summary>
        public const string OptionMarkPressedBorder = "OptionMark.Pressed.Border";

        /// <summary>
        /// CheckBox/RadioButton pressed glyph color.
        /// </summary>
        public const string OptionMarkPressedGlyph = "OptionMark.Pressed.Glyph";

        /// <summary>
        /// CheckBox/RadioButton disabled background color.
        /// </summary>
        public const string OptionMarkDisabledBackground = "OptionMark.Disabled.Background";

        /// <summary>
        /// CheckBox/RadioButton disabled border color.
        /// </summary>
        public const string OptionMarkDisabledBorder = "OptionMark.Disabled.Border";

        /// <summary>
        /// CheckBox/RadioButton disabled glyph color.
        /// </summary>
        public const string OptionMarkDisabledGlyph = "OptionMark.Disabled.Glyph";

        #endregion

        #region ComboBox

        /// <summary>
        /// ComboBox static glyph (dropdown arrow) color.
        /// </summary>
        public const string ComboBoxStaticGlyph = "ComboBox.Static.Glyph";

        /// <summary>
        /// ComboBox static background color.
        /// </summary>
        public const string ComboBoxStaticBackground = "ComboBox.Static.Background";

        /// <summary>
        /// ComboBox static border color.
        /// </summary>
        public const string ComboBoxStaticBorder = "ComboBox.Static.Border";

        /// <summary>
        /// ComboBox dropdown button static background color.
        /// </summary>
        public const string ComboBoxStaticButtonBackground = "ComboBox.Static.Button.Background";

        /// <summary>
        /// ComboBox dropdown button static border color.
        /// </summary>
        public const string ComboBoxStaticButtonBorder = "ComboBox.Static.Button.Border";

        /// <summary>
        /// ComboBox mouse-over glyph color.
        /// </summary>
        public const string ComboBoxMouseOverGlyph = "ComboBox.MouseOver.Glyph";

        /// <summary>
        /// ComboBox mouse-over background color.
        /// </summary>
        public const string ComboBoxMouseOverBackground = "ComboBox.MouseOver.Background";

        /// <summary>
        /// ComboBox mouse-over border color.
        /// </summary>
        public const string ComboBoxMouseOverBorder = "ComboBox.MouseOver.Border";

        /// <summary>
        /// ComboBox dropdown button mouse-over background color.
        /// </summary>
        public const string ComboBoxMouseOverButtonBackground = "ComboBox.MouseOver.Button.Background";

        /// <summary>
        /// ComboBox dropdown button mouse-over border color.
        /// </summary>
        public const string ComboBoxMouseOverButtonBorder = "ComboBox.MouseOver.Button.Border";

        /// <summary>
        /// ComboBox pressed glyph color.
        /// </summary>
        public const string ComboBoxPressedGlyph = "ComboBox.Pressed.Glyph";

        /// <summary>
        /// ComboBox pressed background color.
        /// </summary>
        public const string ComboBoxPressedBackground = "ComboBox.Pressed.Background";

        /// <summary>
        /// ComboBox pressed border color.
        /// </summary>
        public const string ComboBoxPressedBorder = "ComboBox.Pressed.Border";

        /// <summary>
        /// ComboBox dropdown button pressed background color.
        /// </summary>
        public const string ComboBoxPressedButtonBackground = "ComboBox.Pressed.Button.Background";

        /// <summary>
        /// ComboBox dropdown button pressed border color.
        /// </summary>
        public const string ComboBoxPressedButtonBorder = "ComboBox.Pressed.Button.Border";

        /// <summary>
        /// ComboBox disabled glyph color.
        /// </summary>
        public const string ComboBoxDisabledGlyph = "ComboBox.Disabled.Glyph";

        /// <summary>
        /// ComboBox disabled background color.
        /// </summary>
        public const string ComboBoxDisabledBackground = "ComboBox.Disabled.Background";

        /// <summary>
        /// ComboBox disabled border color.
        /// </summary>
        public const string ComboBoxDisabledBorder = "ComboBox.Disabled.Border";

        /// <summary>
        /// ComboBox dropdown popup border color.
        /// </summary>
        public const string ComboBoxDropDownBorder = "ComboBox.DropDown.Border";

        /// <summary>
        /// ComboBox dropdown popup background color.
        /// </summary>
        public const string ComboBoxDropDownBackground = "ComboBox.DropDown.Background";

        #endregion

        #region DataGrid

        /// <summary>
        /// DataGrid background color.
        /// </summary>
        public const string DataGridBackground = "DataGrid.Static.Background";

        /// <summary>
        /// DataGrid border color.
        /// </summary>
        public const string DataGridBorder = "DataGrid.Static.Border";

        /// <summary>
        /// DataGrid header background color.
        /// </summary>
        public const string DataGridHeaderBackground = "DataGrid.Header.Background";

        /// <summary>
        /// DataGrid header gradient end color.
        /// </summary>
        public const string DataGridHeaderGradientEnd = "DataGrid.Header.GradientEnd";

        /// <summary>
        /// DataGrid header border color.
        /// </summary>
        public const string DataGridHeaderBorder = "DataGrid.Header.Border";

        /// <summary>
        /// DataGrid header foreground/text color.
        /// </summary>
        public const string DataGridHeaderForeground = "DataGrid.Header.Foreground";

        /// <summary>
        /// DataGrid header mouse-over background color.
        /// </summary>
        public const string DataGridHeaderMouseOverBackground = "DataGrid.Header.MouseOver.Background";

        /// <summary>
        /// DataGrid row background color.
        /// </summary>
        public const string DataGridRowBackground = "DataGrid.Row.Background";

        /// <summary>
        /// DataGrid alternating row background color.
        /// </summary>
        public const string DataGridAlternatingRowBackground = "DataGrid.Row.Alternating.Background";

        /// <summary>
        /// DataGrid row foreground/text color.
        /// </summary>
        public const string DataGridRowForeground = "DataGrid.Row.Foreground";

        /// <summary>
        /// DataGrid row selection background color.
        /// </summary>
        public const string DataGridRowSelectionBackground = "DataGrid.Row.Selection.Background";

        /// <summary>
        /// DataGrid row selection foreground color.
        /// </summary>
        public const string DataGridRowSelectionForeground = "DataGrid.Row.Selection.Foreground";

        /// <summary>
        /// DataGrid row selection inactive background color.
        /// </summary>
        public const string DataGridRowSelectionInactiveBackground = "DataGrid.Row.Selection.Inactive.Background";

        /// <summary>
        /// DataGrid row mouse-over background color.
        /// </summary>
        public const string DataGridRowMouseOverBackground = "DataGrid.Row.MouseOver.Background";

        /// <summary>
        /// DataGrid gridline color.
        /// </summary>
        public const string DataGridGridLines = "DataGrid.GridLines";

        /// <summary>
        /// DataGrid cell focus border color.
        /// </summary>
        public const string DataGridCellFocusBorder = "DataGrid.Cell.Focus.Border";

        #endregion

        #region Expander

        /// <summary>
        /// Expander static glyph color.
        /// </summary>
        public const string ExpanderStaticGlyph = "Expander.Static.Glyph";

        /// <summary>
        /// Expander static background color.
        /// </summary>
        public const string ExpanderStaticBackground = "Expander.Static.Background";

        /// <summary>
        /// Expander static border color.
        /// </summary>
        public const string ExpanderStaticBorder = "Expander.Static.Border";

        /// <summary>
        /// Expander static text color.
        /// </summary>
        public const string ExpanderStaticText = "Expander.Static.Text";

        /// <summary>
        /// Expander mouse-over glyph color.
        /// </summary>
        public const string ExpanderMouseOverGlyph = "Expander.MouseOver.Glyph";

        /// <summary>
        /// Expander mouse-over background color.
        /// </summary>
        public const string ExpanderMouseOverBackground = "Expander.MouseOver.Background";

        /// <summary>
        /// Expander mouse-over border color.
        /// </summary>
        public const string ExpanderMouseOverBorder = "Expander.MouseOver.Border";

        /// <summary>
        /// Expander mouse-over text color.
        /// </summary>
        public const string ExpanderMouseOverText = "Expander.MouseOver.Text";

        /// <summary>
        /// Expander pressed glyph color.
        /// </summary>
        public const string ExpanderPressedGlyph = "Expander.Pressed.Glyph";

        /// <summary>
        /// Expander pressed background color.
        /// </summary>
        public const string ExpanderPressedBackground = "Expander.Pressed.Background";

        /// <summary>
        /// Expander pressed border color.
        /// </summary>
        public const string ExpanderPressedBorder = "Expander.Pressed.Border";

        /// <summary>
        /// Expander pressed text color.
        /// </summary>
        public const string ExpanderPressedText = "Expander.Pressed.Text";

        /// <summary>
        /// Expander disabled glyph color.
        /// </summary>
        public const string ExpanderDisabledGlyph = "Expander.Disabled.Glyph";

        /// <summary>
        /// Expander disabled background color.
        /// </summary>
        public const string ExpanderDisabledBackground = "Expander.Disabled.Background";

        /// <summary>
        /// Expander disabled border color.
        /// </summary>
        public const string ExpanderDisabledBorder = "Expander.Disabled.Border";

        /// <summary>
        /// Expander disabled text color.
        /// </summary>
        public const string ExpanderDisabledText = "Expander.Disabled.Text";

        #endregion

        #region Label

        /// <summary>
        /// Label static text color.
        /// </summary>
        public const string LabelStaticText = "Label.Static.Text";

        /// <summary>
        /// Label disabled text color.
        /// </summary>
        public const string LabelDisabledText = "Label.Disabled.Text";

        #endregion

        #region ListView

        /// <summary>
        /// ListView static background color.
        /// </summary>
        public const string ListViewStaticBackground = "ListView.Static.Background";

        /// <summary>
        /// ListView static border color.
        /// </summary>
        public const string ListViewStaticBorder = "ListView.Static.Border";

        /// <summary>
        /// ListView static text color.
        /// </summary>
        public const string ListViewStaticText = "ListView.Static.Text";

        /// <summary>
        /// ListView disabled background color.
        /// </summary>
        public const string ListViewDisabledBackground = "ListView.Disabled.Background";

        /// <summary>
        /// ListView disabled border color.
        /// </summary>
        public const string ListViewDisabledBorder = "ListView.Disabled.Border";

        /// <summary>
        /// ListView item static foreground color.
        /// </summary>
        public const string ListViewItemStaticForeground = "ListViewItem.Static.Foreground";

        /// <summary>
        /// ListView item static border color.
        /// </summary>
        public const string ListViewItemStaticBorder = "ListViewItem.Static.Border";

        /// <summary>
        /// ListView item selection background color.
        /// </summary>
        public const string ListViewItemSelectionBackground = "ListViewItem.Selection.Background";

        /// <summary>
        /// ListView item selection foreground color.
        /// </summary>
        public const string ListViewItemSelectionForeground = "ListViewItem.Selection.Foreground";

        /// <summary>
        /// ListView item selection border color.
        /// </summary>
        public const string ListViewItemSelectionBorder = "ListViewItem.Selection.Border";

        /// <summary>
        /// ListView item mouse-over background color.
        /// </summary>
        public const string ListViewItemMouseOverBackground = "ListViewItem.MouseOver.Background";

        /// <summary>
        /// ListView item mouse-over border color.
        /// </summary>
        public const string ListViewItemMouseOverBorder = "ListViewItem.MouseOver.Border";

        #endregion

        #region Menu

        /// <summary>
        /// Menu default background color.
        /// </summary>
        public const string MenuDefaultBackground = "MenuDefaultBackground";

        /// <summary>
        /// Menu default text color.
        /// </summary>
        public const string MenuDefaultText = "MenuDefaultText";

        /// <summary>
        /// Menu popup default background color.
        /// </summary>
        public const string MenuPopupDefaultBackground = "MenuPopupDefaultBackground";

        /// <summary>
        /// Menu popup default border color.
        /// </summary>
        public const string MenuPopupDefaultBorder = "MenuPopupDefaultBorder";

        /// <summary>
        /// Menu popup default text color.
        /// </summary>
        public const string MenuPopupDefaultText = "MenuPopupDefaultText";

        /// <summary>
        /// Menu popup hovered item background color.
        /// </summary>
        public const string MenuPopupHoveredItemBackground = "MenuPopupHoveredItemBackground";

        /// <summary>
        /// Menu popup hovered text color.
        /// </summary>
        public const string MenuPopupHoveredText = "MenuPopupHoveredText";

        /// <summary>
        /// Menu popup disabled text color.
        /// </summary>
        public const string MenuPopupDisabledText = "MenuPopupDisabledText";

        /// <summary>
        /// Menu popup separator color.
        /// </summary>
        public const string MenuPopupDefaultSeparator = "MenuPopupDefaultSeparator";

        #endregion

        #region ScrollBar

        /// <summary>
        /// ScrollBar static background color.
        /// </summary>
        public const string ScrollBarStaticBackground = "ScrollBar.Static.Background";

        /// <summary>
        /// ScrollBar static border color.
        /// </summary>
        public const string ScrollBarStaticBorder = "ScrollBar.Static.Border";

        /// <summary>
        /// ScrollBar static thumb color.
        /// </summary>
        public const string ScrollBarStaticThumb = "ScrollBar.Static.Thumb";

        /// <summary>
        /// ScrollBar static glyph (arrow) color.
        /// </summary>
        public const string ScrollBarStaticGlyph = "ScrollBar.Static.Glyph";

        /// <summary>
        /// ScrollBar mouse-over thumb color.
        /// </summary>
        public const string ScrollBarMouseOverThumb = "ScrollBar.MouseOver.Thumb";

        /// <summary>
        /// ScrollBar mouse-over glyph color.
        /// </summary>
        public const string ScrollBarMouseOverGlyph = "ScrollBar.MouseOver.Glyph";

        /// <summary>
        /// ScrollBar pressed thumb color.
        /// </summary>
        public const string ScrollBarPressedThumb = "ScrollBar.Pressed.Thumb";

        /// <summary>
        /// ScrollBar pressed glyph color.
        /// </summary>
        public const string ScrollBarPressedGlyph = "ScrollBar.Pressed.Glyph";

        /// <summary>
        /// ScrollBar disabled glyph color.
        /// </summary>
        public const string ScrollBarDisabledGlyph = "ScrollBar.Disabled.Glyph";

        #endregion

        #region TabControl

        /// <summary>
        /// TabControl background color.
        /// </summary>
        public const string TabControlBackground = "TabControl.Background";

        /// <summary>
        /// TabControl foreground/text color.
        /// </summary>
        public const string TabControlForeground = "TabControl.Foreground";

        /// <summary>
        /// TabControl border color.
        /// </summary>
        public const string TabControlBorder = "TabControl.Border";

        /// <summary>
        /// Tab item static foreground color.
        /// </summary>
        public const string TabItemStaticForeground = "TabItem.Static.Foreground";

        /// <summary>
        /// Tab item static border color.
        /// </summary>
        public const string TabItemStaticBorder = "TabItem.Static.Border";

        /// <summary>
        /// Tab item selection background color.
        /// </summary>
        public const string TabItemSelectionBackground = "TabItem.Selection.Background";

        /// <summary>
        /// Tab item selection foreground color.
        /// </summary>
        public const string TabItemSelectionForeground = "TabItem.Selection.Foreground";

        /// <summary>
        /// Tab item selection border color.
        /// </summary>
        public const string TabItemSelectionBorder = "TabItem.Selection.Border";

        /// <summary>
        /// Tab item mouse-over background color.
        /// </summary>
        public const string TabItemMouseOverBackground = "TabItem.MouseOver.Background";

        /// <summary>
        /// Tab item mouse-over border color.
        /// </summary>
        public const string TabItemMouseOverBorder = "TabItem.MouseOver.Border";

        #endregion

        #region TextBox

        /// <summary>
        /// TextBox static background color.
        /// </summary>
        public const string TextBoxStaticBackground = "TextBox.Static.Background";

        /// <summary>
        /// TextBox static border color.
        /// </summary>
        public const string TextBoxStaticBorder = "TextBox.Static.Border";

        /// <summary>
        /// TextBox static foreground/text color.
        /// </summary>
        public const string TextBoxStaticForeground = "TextBox.Static.Foreground";

        /// <summary>
        /// TextBox mouse-over border color.
        /// </summary>
        public const string TextBoxMouseOverBorder = "TextBox.MouseOver.Border";

        /// <summary>
        /// TextBox focus border color.
        /// </summary>
        public const string TextBoxFocusBorder = "TextBox.Focus.Border";

        /// <summary>
        /// TextBox selection background color.
        /// </summary>
        public const string TextBoxSelectionBackground = "TextBox.Selection.Background";

        /// <summary>
        /// TextBox caret color.
        /// </summary>
        public const string TextBoxCaret = "TextBox.Caret";

        /// <summary>
        /// TextBox disabled background color.
        /// </summary>
        public const string TextBoxDisabledBackground = "TextBox.Disabled.Background";

        /// <summary>
        /// TextBox disabled border color.
        /// </summary>
        public const string TextBoxDisabledBorder = "TextBox.Disabled.Border";

        /// <summary>
        /// TextBox disabled foreground color.
        /// </summary>
        public const string TextBoxDisabledForeground = "TextBox.Disabled.Foreground";

        #endregion

        #region TreeView

        /// <summary>
        /// TreeView static background color.
        /// </summary>
        public const string TreeViewStaticBackground = "TreeView.Static.Background";

        /// <summary>
        /// TreeView static border color.
        /// </summary>
        public const string TreeViewStaticBorder = "TreeView.Static.Border";

        /// <summary>
        /// TreeView static text color.
        /// </summary>
        public const string TreeViewStaticText = "TreeView.Static.Text";

        /// <summary>
        /// TreeView disabled background color.
        /// </summary>
        public const string TreeViewDisabledBackground = "TreeView.Disabled.Background";

        /// <summary>
        /// TreeView item static text color.
        /// </summary>
        public const string TreeViewItemStaticText = "TreeViewItem.Static.Text";

        /// <summary>
        /// TreeView item disabled text color.
        /// </summary>
        public const string TreeViewItemDisabledText = "TreeViewItem.Disabled.Text";

        /// <summary>
        /// TreeView item highlight/selection background color.
        /// </summary>
        public const string TreeViewItemHighlightStatic = "TreeViewItem.Highlight.Static";

        /// <summary>
        /// TreeView item highlight/selection text color.
        /// </summary>
        public const string TreeViewItemHighlightStaticText = "TreeViewItem.Highlight.Static.Text";

        /// <summary>
        /// TreeView item inactive highlight background color.
        /// </summary>
        public const string TreeViewItemHighlightInactive = "TreeViewItem.Highlight.Inactive";

        /// <summary>
        /// TreeView item inactive highlight text color.
        /// </summary>
        public const string TreeViewItemHighlightInactiveText = "TreeViewItem.Highlight.Inactive.Text";

        /// <summary>
        /// TreeView expansion arrow static fill color.
        /// </summary>
        public const string TreeViewItemTreeArrowStaticFill = "TreeViewItem.TreeArrow.Static.Fill";

        /// <summary>
        /// TreeView expansion arrow static stroke color.
        /// </summary>
        public const string TreeViewItemTreeArrowStaticStroke = "TreeViewItem.TreeArrow.Static.Stroke";

        /// <summary>
        /// TreeView expansion arrow mouse-over fill color.
        /// </summary>
        public const string TreeViewItemTreeArrowMouseOverFill = "TreeViewItem.TreeArrow.MouseOver.Fill";

        /// <summary>
        /// TreeView expansion arrow mouse-over stroke color.
        /// </summary>
        public const string TreeViewItemTreeArrowMouseOverStroke = "TreeViewItem.TreeArrow.MouseOver.Stroke";

        #endregion

        #region Toolbar

        /// <summary>
        /// Toolbar default background color.
        /// </summary>
        public const string ToolbarDefaultBackground = "ToolbarDefaultBackground";

        /// <summary>
        /// Toolbar default border color.
        /// </summary>
        public const string ToolbarDefaultBorder = "ToolbarDefaultBorder";

        /// <summary>
        /// Toolbar grip color.
        /// </summary>
        public const string ToolbarDefaultGrip = "ToolbarDefaultGrip";

        /// <summary>
        /// Toolbar separator color.
        /// </summary>
        public const string ToolbarDefaultSeparator = "ToolbarDefaultSeparator";

        /// <summary>
        /// Toolbar button hovered background color.
        /// </summary>
        public const string ToolbarButtonHoveredBackground = "ToolbarButtonHoveredBackground";

        /// <summary>
        /// Toolbar button pressed background color.
        /// </summary>
        public const string ToolbarButtonPressedBackground = "ToolbarButtonPressedBackground";

        /// <summary>
        /// Toolbar button checked background color.
        /// </summary>
        public const string ToolbarButtonCheckedBackground = "ToolbarButtonCheckedBackground";

        /// <summary>
        /// Toolbar button checked border color.
        /// </summary>
        public const string ToolbarButtonCheckedBorder = "ToolbarButtonCheckedBorder";

        #endregion

        #region StatusBar

        /// <summary>
        /// StatusBar background color.
        /// </summary>
        public const string StatusBarBackground = "StatusBarBackground";

        /// <summary>
        /// StatusBar foreground/text color.
        /// </summary>
        public const string StatusBarForeground = "StatusBarForeground";

        #endregion

        #region Accent Colors

        /// <summary>
        /// Primary accent color (typically blue).
        /// </summary>
        public const string AccentColor = "AccentColor";

        /// <summary>
        /// Focus indicator color.
        /// </summary>
        public const string FocusStyleBrush = "FocusStyle.Brush";

        #endregion

        #region GroupBox

        /// <summary>
        /// GroupBox border color.
        /// </summary>
        public const string GroupBoxBorder = "GroupBoxBorder";

        #endregion

        #region Separator

        /// <summary>
        /// Separator border/line color.
        /// </summary>
        public const string SeparatorBorder = "SeparatorBorder";

        #endregion

        #region StackPanel

        /// <summary>
        /// StackPanel background color.
        /// </summary>
        public const string StackPanelBackground = "StackPanelBackground";

        #endregion
    }
}
