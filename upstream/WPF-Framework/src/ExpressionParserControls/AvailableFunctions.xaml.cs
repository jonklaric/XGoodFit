using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExpressionParserControls
{
    /// <summary>
    /// Displays available expression parser functions grouped by category with
    /// a searchable TreeView and a detail panel showing syntax, description, and examples.
    /// </summary>
    public partial class AvailableFunctions
    {
        /// <summary>
        /// The expression control to insert function text into.
        /// </summary>
        public static readonly DependencyProperty ExpressionTextProperty = DependencyProperty.Register(
            nameof(ExpressionText), typeof(ExpressionControl), typeof(AvailableFunctions));

        /// <summary>
        /// Gets or sets the target ExpressionControl for function insertion.
        /// </summary>
        public ExpressionControl ExpressionText
        {
            get => (ExpressionControl)GetValue(ExpressionTextProperty);
            set => SetValue(ExpressionTextProperty, value);
        }

        /// <summary>
        /// Identifies the CompactMode dependency property.
        /// When true, hides the detail panel and splitter so the control shows only the tree + search + insert.
        /// </summary>
        public static readonly DependencyProperty CompactModeProperty = DependencyProperty.Register(
            nameof(CompactMode), typeof(bool), typeof(AvailableFunctions),
            new PropertyMetadata(false, OnCompactModeChanged));

        /// <summary>
        /// Gets or sets whether the control is in compact mode (tree only, no detail panel).
        /// </summary>
        public bool CompactMode
        {
            get => (bool)GetValue(CompactModeProperty);
            set => SetValue(CompactModeProperty, value);
        }

        /// <summary>
        /// Raised when the selected function changes. Provides the FunctionDescriptor of the newly selected function.
        /// </summary>
        public event Action<FunctionDescriptor?>? SelectedFunctionChanged;

        /// <summary>
        /// Handles changes to the CompactMode property by showing or hiding the detail panel.
        /// </summary>
        private static void OnCompactModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AvailableFunctions af)
                af.ApplyCompactMode((bool)e.NewValue);
        }

        /// <summary>
        /// Shows or hides the detail panel and splitter based on compact mode.
        /// In compact mode, the tree column fills the entire width.
        /// </summary>
        private void ApplyCompactMode(bool compact)
        {
            if (compact)
            {
                DetailSplitter.Visibility = Visibility.Collapsed;
                DetailBorder.Visibility = Visibility.Collapsed;
                InsertFunctionButton.Visibility = Visibility.Collapsed;
                TreeColumn.Width = new GridLength(1, GridUnitType.Star);
                DetailColumn.Width = new GridLength(0);
            }
            else
            {
                DetailSplitter.Visibility = Visibility.Visible;
                DetailBorder.Visibility = Visibility.Visible;
                InsertFunctionButton.Visibility = Visibility.Visible;
                TreeColumn.Width = new GridLength(200);
                DetailColumn.Width = new GridLength(1, GridUnitType.Star);
            }
        }

        /// <summary>
        /// Maps TreeViewItem to its FunctionDescriptor for quick lookup.
        /// </summary>
        private readonly Dictionary<TreeViewItem, FunctionDescriptor> _itemToFunction = new Dictionary<TreeViewItem, FunctionDescriptor>();

        /// <summary>
        /// All category TreeViewItems for search filtering.
        /// </summary>
        private readonly List<TreeViewItem> _categoryItems = new List<TreeViewItem>();

        /// <summary>
        /// All function TreeViewItems for search filtering.
        /// </summary>
        private readonly List<TreeViewItem> _functionItems = new List<TreeViewItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AvailableFunctions"/> class.
        /// </summary>
        public AvailableFunctions()
        {
            InitializeComponent();
            PopulateTreeView();
            Loaded += (_, _) => ApplyCompactMode(CompactMode);
        }

        /// <summary>
        /// Populates the TreeView with functions grouped by category.
        /// </summary>
        private void PopulateTreeView()
        {
            var grouped = FunctionInfo.GetGroupedFunctions();

            foreach (var category in FunctionInfo.Categories)
            {
                if (!grouped.ContainsKey(category) || grouped[category].Count == 0)
                    continue;

                var categoryItem = new TreeViewItem
                {
                    Header = new TextBlock { Text = category, FontWeight = FontWeights.Bold },
                    IsExpanded = true,
                    Style = TryFindResource("TreeViewItemStyle") as Style
                };

                foreach (var func in grouped[category])
                {
                    var funcItem = new TreeViewItem
                    {
                        Header = new TextBlock { Text = func.Name },
                        Tag = func,
                        Style = TryFindResource("TreeViewItemStyle") as Style
                    };

                    funcItem.MouseDoubleClick += FunctionItem_MouseDoubleClick;
                    _itemToFunction[funcItem] = func;
                    _functionItems.Add(funcItem);
                    categoryItem.Items.Add(funcItem);
                }

                _categoryItems.Add(categoryItem);
                AvailableFunctionsProp.Items.Add(categoryItem);
            }
        }

        /// <summary>
        /// Handles selection changes in the TreeView to update the detail panel and raise SelectedFunctionChanged.
        /// </summary>
        private void AvailableFunctions_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && _itemToFunction.TryGetValue(item, out var func))
            {
                ShowFunctionDetail(func);
                InsertFunctionButton.IsEnabled = true;
                SelectedFunctionChanged?.Invoke(func);
            }
            else
            {
                DetailPanel.Visibility = Visibility.Collapsed;
                InsertFunctionButton.IsEnabled = false;
                SelectedFunctionChanged?.Invoke(null);
            }
        }

        /// <summary>
        /// Displays the detail panel for a given function.
        /// </summary>
        private void ShowFunctionDetail(FunctionDescriptor func)
        {
            DetailFunctionName.Text = func.Name;
            DetailSyntax.Text = func.Syntax;
            DetailReturns.Text = func.Returns;
            DetailDescription.Text = func.Description;
            DetailExample.Text = func.Example;

            if (func.Aliases.Length > 0)
            {
                DetailAliases.Text = string.Join(", ", func.Aliases);
                AliasesPanel.Visibility = Visibility.Visible;
            }
            else
            {
                AliasesPanel.Visibility = Visibility.Collapsed;
            }

            DetailPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Inserts the selected function into the expression on double-click.
        /// </summary>
        private void FunctionItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item && _itemToFunction.TryGetValue(item, out var func))
            {
                InsertFunction(func);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the Insert button click.
        /// </summary>
        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (AvailableFunctionsProp.SelectedItem is TreeViewItem item && _itemToFunction.TryGetValue(item, out var func))
            {
                InsertFunction(func);
            }
        }

        /// <summary>
        /// Inserts a function's text into the bound ExpressionControl.
        /// </summary>
        private void InsertFunction(FunctionDescriptor func)
        {
            ExpressionText?.InsertText(func.InsertText);
        }

        /// <summary>
        /// Inserts the currently selected function into the bound ExpressionControl.
        /// Used by external hosts that have their own Insert button for the functions panel.
        /// </summary>
        public void InsertSelectedFunction()
        {
            if (AvailableFunctionsProp.SelectedItem is TreeViewItem item && _itemToFunction.TryGetValue(item, out var func))
            {
                InsertFunction(func);
            }
        }


        /// <summary>
        /// Selects a function item that matches the given help document path.
        /// Used by CalculatorControl when a function hyperlink is clicked.
        /// </summary>
        public void SelectFunctionByHelpPath(string helpDocumentPath)
        {
            // Match by comparing the help path from Lexer.Keywords against function names
            // The helpDocumentPath contains the full absolute path, so we extract the function name
            foreach (var kvp in _itemToFunction)
            {
                var func = kvp.Value;
                // Check if the help path ends with a pattern matching this function
                string expectedPath = $"Parser Help/{func.Name}Help.html";
                if (helpDocumentPath?.EndsWith(expectedPath, StringComparison.OrdinalIgnoreCase) == true)
                {
                    kvp.Key.IsSelected = true;
                    kvp.Key.BringIntoView();
                    return;
                }
            }

            // Fallback: try matching any function whose help path is in the helpDocumentPath
            foreach (var keyword in ExpressionParser.Lexer.Keywords)
            {
                if (keyword.Value.Item4 != ExpressionParser.TokenClass.Function)
                    continue;

                string helpPath = keyword.Value.Item1;
                if (string.IsNullOrEmpty(helpPath))
                    continue;

                string fullPath = Environment.CurrentDirectory + "/" + helpPath;
                if (!string.Equals(new Uri(fullPath).AbsolutePath, helpDocumentPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Found the keyword, now find the matching function item
                foreach (var kvp in _itemToFunction)
                {
                    if (string.Equals(kvp.Value.Name, keyword.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        kvp.Key.IsSelected = true;
                        kvp.Key.BringIntoView();
                        return;
                    }

                    // Check aliases
                    foreach (var alias in kvp.Value.Aliases)
                    {
                        if (string.Equals(alias, keyword.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            kvp.Key.IsSelected = true;
                            kvp.Key.BringIntoView();
                            return;
                        }
                    }
                }
            }

            // No matching function found — don't select anything (e.g., for operators)
        }
    }
}
