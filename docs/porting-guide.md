# Porting guide

How XGoodFit ports upstream's WPF code to Avalonia 12. [The migration plan](migration-plan.md) explains
why; this guide is the how, file by file. Follow it so every ported file reads the same way and can be
compared with its upstream original.

## 1. Where things go

- Upstream code is vendored, byte for byte, in `upstream/` and is **never edited**
  (`scripts/vendor-upstream.sh` checks it).
- Each ported project lives in `src/` and overlays its upstream project (root `Directory.Build.targets`):
  - Set `UpstreamProjectDir` to the upstream project folder. Upstream `.cs` files then compile
    unchanged, unless the port has a file at the same relative path, which replaces it.
  - Projects that are nearly all WPF code set `<UpstreamCompileMode>OptIn</UpstreamCompileMode>`. Then
    only the ported files and the upstream files listed in `UpstreamCompileInclude` compile, so the
    project grows one port at a time (see `src/Framework/GenericControls`).
  - `UpstreamCompileRemove` (with a `Reason` attribute) leaves an upstream file out on purpose.
- AvalonDock (Ms-PL, vendored inside WPF-Framework) is reference only: the dock themes take its
  colours, key names and glyph positions, but none of its files are ported (`THIRD_PARTY_NOTICES.md`).
- XAML is always ported: `Foo.xaml` becomes `Foo.axaml` at the same relative path, and `Foo.xaml.cs`
  becomes `Foo.axaml.cs`. Upstream `*.xaml.cs` files never compile.
- Keep namespaces, class names, `x:Class`, element names, event-handler names and resource keys
  exactly as upstream, so the rest of upstream's code compiles against the port.
  - Example: the plot library keeps the `OxyPlot.Wpf` namespace even though it's Avalonia code. Plot
    XML in `.bestfit` files records type names such as `OxyPlot.Wpf.LinearAxis`, so a different
    namespace would break file compatibility.
- New projects go in `XGoodFit.slnx`, and tests in `tests/` (MSTest.Sdk, headless; see below).
- Upstream's test projects overlay the same way, so their tests run unchanged against the port (see
  `tests/BestFit/RMC.BestFit.UI.Tests`). A test project can replace an MSTest attribute that a test
  file uses with its own of the same name, declared in the namespace that contains the test files'
  namespaces: C# finds it before the files' usings. That's how upstream's `[STATestMethod]` tests run
  on Avalonia's UI thread.
- `scripts/porting/xgf_port.py` ports a file by exact, checked replacements: it copies the upstream
  file and fails if a replacement doesn't match, so everything else stays as upstream wrote it.
- The application's startup (RMC.BestFit.App) follows `packaging/README.md`: the first line of
  `Main` is `PlatformStartup.BuildVelopackApp(args)?.Run();`, `OnFrameworkInitializationCompleted`
  creates `FileOpenRequests.For(this)` and handles `FilesOpened`, and the main window's
  `UpdateService` is a `VelopackUpdateService` when the app is installed (`XGoodFit.Platform`).
- Native libraries are build outputs (`native/build-hecdss.sh` writes `artifacts/native`), never
  committed. The project that wraps one ships it as `runtimes/<rid>/native` assets, which reach every
  project that references it (see `src/Hec.Dss`).

## 2. Every ported file

- Start with the upstream file and change only what the port needs. Don't restyle, refactor, rename or
  "fix" upstream code. Behaviour must stay the same, including upstream's quirks: note them, don't
  change them.
- Keep upstream's comments and doc comments. Explain each change in an `XGoodFit port:` comment, in
  a `.axaml` header or a `<remarks>` paragraph: what changed and why, briefly. Say what wasn't ported
  and why, such as WPF pen or touch settings.
- XGoodFit's own new files say so (`XGoodFit addition:`), with a summary of why they exist.
- Build with no warnings. Don't suppress a warning unless there's no better fix, and say why at the
  suppression.

## 3. XAML

| WPF | Avalonia |
|---|---|
| `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` | `xmlns="https://github.com/avaloniaui"` |
| `clr-namespace:X;assembly=Y` | `using:X` |
| `Style`, `Style="{StaticResource K}"` | `ControlTheme`, `Theme="{DynamicResource K}"` (`TargetType` without `{x:Type}`). Shapes and text blocks take themes too |
| Implicit style `<Style TargetType="X">` | `<ControlTheme x:Key="{x:Type X}" TargetType="X">`, `BasedOn` the keyed theme. A theme with only setters keeps a user control's template (ContentControl's default) |
| `<Setter.Value>` | the value directly inside `<Setter>` |
| `ControlTemplate.Triggers` | nested styles: `^:pointerover /template/ Border#Name` |
| `IsMouseOver` / `IsPressed` / `IsChecked` / `IsEnabled=False` / `IsSelected` / `IsKeyboardFocusWithin` / `IsFocused` | `:pointerover` / `:pressed` / `:checked` / `:disabled` / `:selected` / `:focus-within` / `:focus` |
| Property trigger on another property | property selector `^[Prop=Value]` |
| `MultiTrigger` | combined selector `^:a:b`. A false condition is `:not(...)`, but not right before `>` inside `/template/` |
| Overlapping triggers | the same order: later styles win where they overlap |
| `DataTrigger` | a bound class and a style in the element's `Styles`: `Classes.invalid="{Binding ElementName=c, Path=!IsValid}"` with `<Style Selector="Border.invalid">` (see `NameTextBox.axaml`); or a converter, or `Xaml.Behaviors` |
| `Visibility="Collapsed"` | `IsVisible="False"` |
| A tool tip in a `ComboBox.ItemContainerStyle` | `<ComboBox.Styles><Style Selector="ComboBoxItem">` setting `ToolTip.Tip` |
| `Popup Placement="Right"` | `Placement="RightEdgeAlignedTop"`; `PopupAnimation` and `AllowsTransparency` aren't ported |
| Combo box items that are numbers | an item template whose binding formats with `FormattingCulture` (D8) |
| An enum `DataTrigger` | `Classes.x="{Binding P, Converter={x:Static ObjectConverters.Equal}, ConverterParameter={x:Static local:E.V}}"` with styles in the element's `Styles` |
| A `DataTrigger`'s storyboard | a style animation on that class: it stops when the class goes, as `StopStoryboard` does |
| A cell's `DataTrigger` setting `IsEnabled` from its row | `<Setter Property="IsEnabled" Value="{Binding Enabled}"/>` in the cell's theme |
| A `MultiDataTrigger` | a nested style on two property selectors, the second condition bound through NumericControls' `DataTrigger.Binding`: `^[IsEnabled=True][(numcntrls|DataTrigger.Binding)=False]` (see `B17CAnalysisPropertiesControl.axaml`) |
| An `[Obsolete]` property (`IncludeBounds="True"`) | its same-meaning replacement (`BoundsAreExclusive="False"`): Avalonia's compiler reports AVLN5001, WPF's doesn't |
| `DataGrid MinRowHeight="24"` | ProDataGrid has none and sets each row's `MinHeight` itself, so a row theme's setter loses: put `MinHeight` in the cells' themes |
| A grid's `RowHeaderStyle` | `<cntrls:CopyPasteDataGrid.Styles><Style Selector="DataGridRowHeader"><Setter Property="Theme" Value="{DynamicResource K}"/></Style>`: ProDataGrid has no RowHeaderTheme (see `CoincidentFrequencyControl.axaml`) |
| A format with commas, `StringFormat={}{0:#,##0.####}` | quote it: `StringFormat='{}{0:#,##0.####}'` |
| A `ToolTip` setter holding a `<ToolTip>` element (cells, column headers) | `<Setter Property="ToolTip.Tip"><Template>…</Template></Setter>`; in code `new Setter(ToolTip.TipProperty, new FuncTemplate<TextBlock>(…))`: one control can't be shared between cells |
| `DisplayMemberPath` / `SelectedValuePath` | `DisplayMemberBinding="{Binding X}"` / `SelectedValueBinding="{Binding Y}"` |
| `ListView` without a `GridView` | `ListBox` with `Theme="{DynamicResource ListViewStyle}"` |
| `Visibility="Hidden"` | `Opacity="0"` with `IsHitTestVisible="False"` (keeps its space) |
| `Visibility="{Binding …, Converter=…}"` | `wpf:WpfElement.Visibility="{Binding …}"` (`xmlns:wpf="using:WpfCompat"`): the converters produce WpfCompat's `Visibility`. Never bind one to `IsVisible`, which reads Visible (0) as false |
| `{Binding IsEnabled, …}` (reading it) | `IsEffectivelyEnabled`: WPF's `IsEnabled` includes the parents', Avalonia's doesn't |
| `pack://application:,,,/Asm;component/Path` | `avares://Asm/Path` |
| `{Binding X, RelativeSource={RelativeSource FindAncestor, AncestorType=T}}` | `{Binding $parent[T].X}` |
| `{Binding X, ElementName=n}` | unchanged (Avalonia has `ElementName`), or `{Binding #n.X}` |
| `ActualWidth` / `ActualHeight` | `Bounds.Width` / `Bounds.Height` |
| `ContentPresenter` with `ContentSource` | `Content="{TemplateBinding Content}"` and `ContentTemplate` bound explicitly. A ContentControl's presenter is named `PART_ContentPresenter` |
| `ToolTip="…"` | `ToolTip.Tip="…"` |
| `PreviewKeyDown="H"`, `PreviewMouse…="H"` | no attribute: `AddHandler(KeyDownEvent, H, RoutingStrategies.Tunnel)` in the code-behind |
| `TextChanged="H"` | `TextChanging` in the code-behind: Avalonia raises `TextChanged` after rendering, WPF as the text changes (see `NameTextBox.axaml.cs`) |
| `x:Array` | `<wpfm:ArrayExtension Type="{x:Type sys:Double}">` (`xmlns:wpfm="using:WpfCompat.Markup"`, `xmlns:sys="using:System"`) |
| `x:FieldModifier="public"` | `internal` unless another assembly uses the field: Avalonia's generated fields have no doc comments (a warning). If one does, rename the element and add a documented property with upstream's name (see `NameTextPropertyControl.NameTextBox`) |
| `x:Name` on a brush, gradient stop, drawing or grid column | not allowed: reach it through the control that owns it (`grid.Columns[0]`) |
| A binding that names no culture | `ConverterCulture={x:Static wpf:FormattingCulture.Current}` (en-US, as WPF's bindings, plan §5.2). Keep upstream's explicit `CurrentCulture` bindings |
| `Line` with a `Stretch` | `wpfs:Line` (`xmlns:wpfs="using:WpfCompat.Shapes"`), which stretches as WPF's does |
| `Slider` `IsMoveToPointEnabled`, track presses | `wpf:WpfSlider.IsMoveToPointEnabled`: WPF's paging with repeat, or move to the point |
| `Popup Placement="Left"` / `"Bottom"`, `StaysOpen="False"` | `LeftEdgeAlignedTop` / `BottomEdgeAlignedLeft`, `IsLightDismissEnabled="True"` |
| `DrawingBrush` with a drawing as content | an explicit `<DrawingBrush.Drawing>` |
| `MouseUp="H"` | `PointerReleased="H"` (every button, as WPF's `MouseUp`) |
| `StrokeStartLineCap` / `StrokeEndLineCap` | `StrokeLineCap` (one for both) |
| `RenderOptions.BitmapScalingMode` | `RenderOptions.BitmapInterpolationMode` |
| `Line X1 Y1 X2 Y2` | `StartPoint="x1,y1"` `EndPoint="x2,y2"` |
| `LayoutTransform` | wrap in `LayoutTransformControl` |
| `RenderTransformOrigin="0.5,0.5"` | `RenderTransformOrigin="50%,50%"`: Avalonia reads numbers without `%` as pixels |
| `StrokeDashArray="1 2"` | `StrokeDashArray="1,2"` |
| `Command="Save"` (ApplicationCommands) | `Command="{wpfi:Command Save}"`, with `xmlns:wpfi="using:WpfCompat.Input"` |
| `x:Static SystemParameters.…` / `SystemColors.…` | the fixed resources in Themes' `Merged.axaml` (`SystemParameters.VerticalScrollBarWidth`, …) |
| `DataGrid` `CellStyle` / `RowStyle` / `ColumnHeaderStyle`; a column's `CellStyle` / `HeaderStyle` | `CellTheme` / `RowTheme` / `ColumnHeaderTheme`; the column's `CellTheme` / `HeaderTheme` (ProDataGrid) |
| A column's `ElementStyle` / `EditingElementStyle` | `gc:DataGridColumnStyles.ElementStyle` / `.EditingElementStyle` |
| A header style's `ContentTemplate` | `themes:ColumnHeaderContent.Template` |
| `Selector.IsSelectionActive` | `wpf:WpfSelector.IsSelectionActive` |
| `AlternationCount="2"` with an `AlternationIndex` trigger | `:nth-child(2n)`. ProDataGrid has no `RowHeaderStyle` or `AlternatingRowBackground` |
| `ScrollViewer.PanningMode`, `Stylus.*`, `SnapsToDevicePixels`, `TextOptions.*` | not ported (note it) |

Templates and themes:
- A ControlTheme replaces the whole theme. WPF still applied the system (Aero2) style underneath an
  app style, so a ported theme also sets the Aero2 defaults the template relies on. Examples: Button
  `BorderThickness=1`; Control content alignment Left/Top. Check `PresentationFramework.Aero2`'s XAML
  in the WPF sources for the values.
- Avalonia finds template parts by name, where WPF often uses commands or `IsItemsHost`. Use Avalonia's
  part names (`PART_ItemsPresenter`, `PART_Popup`, `PART_LineUpButton`, …). The Avalonia 12 Simple
  theme sources show what each control needs.
- WPF's `ScrollViewer` and `Track` pick up their templated parent's settings by themselves; in
  Avalonia, bind them (see `ListBox.axaml`).
- Grid row and column definitions can't be styled: give each trigger state its own template
  (see `TabControl.axaml`).
- A BasedOn ControlTheme also inherits its base's nested (trigger) styles, unlike WPF.

## 4. C#

| WPF | Avalonia |
|---|---|
| `System.Windows.*` usings | `Avalonia`, `Avalonia.Controls`, `Avalonia.Input`, `Avalonia.Media`, … |
| `DependencyProperty.Register` | `AvaloniaProperty.Register<TOwner, T>` (StyledProperty), or `RegisterDirect` for field-backed ones |
| `PropertyChangedCallback` | `OnPropertyChanged` override, or `Property.Changed.AddClassHandler<T>(…)` in a static constructor (put it before the callback's doc comment) |
| `CoerceValueCallback` | the `coerce:` parameter of `Register` |
| `RoutedEvent` / `EventManager.RegisterRoutedEvent` | `RoutedEvent.Register<TOwner, TArgs>` |
| `PreviewMouseLeftButtonDown += H` and the other WPF mouse events | `this.AddPreviewMouseLeftButtonDownHandler(H)` (WpfCompat.Input's `MouseEvents`: WPF's button filter and routing). `H` takes Avalonia's pointer arguments, with WPF's `ChangedButton`, `LeftButton`, `RightButton` and `OriginalSource` as extension properties. Wheel deltas are notches (WPF: 120 per notch) |
| `Keyboard.Modifiers`, `ModifierKeys` | WpfCompat.Input's (Command counts as Control on macOS) |
| `Checked="H"` / `Unchecked="H"` on toggle buttons and check boxes | `toggle.AddCheckedHandler(H)` / `AddUncheckedHandler(H)` in the code-behind (WpfCompat): Avalonia 12 only has `IsCheckedChanged` |
| `RichTextBox` (editable) | AvaloniaEdit's `TextEditor` (see ExpressionParserControls' `ExpressionControl`) |
| `RichTextBox` (read-only), `FlowDocument`, `Paragraph`, `Run`, `Hyperlink`, `TextRange` | WpfCompat's `Controls.RichTextBox` and `Documents` (`xmlns:wpfc="using:WpfCompat.Controls"`), which keep the document-building code as it is |
| `Dispatcher.HasShutdownStarted` / `HasShutdownFinished` | unchanged, with `using WpfCompat;` |
| `x.SetResourceReference(P, key)` | unchanged, with `using WpfCompat;` |
| `Dispatcher.Invoke` / `BeginInvoke` | `Dispatcher.UIThread.Invoke` / `Post` |
| `VisualTreeHelper` | `GetVisualParent`, `GetVisualChildren`, `FindAncestorOfType<T>` (`Avalonia.VisualTree`) |
| `x.Visibility = Visibility.Hidden` | unchanged, with `using WpfCompat;` (an extension property that agrees with `IsVisible`) |
| `CommandBindings.Add(…)`, `RoutedCommand`, `ApplicationCommands`, `SystemCommands`, `ExecutedRoutedEventArgs` | WpfCompat's (`using WpfCompat.Input;`) |
| `Window.ResizeMode` | `WpfWindow.ResizeMode` (WpfCompat). Metro windows have it as a property |
| `Brush`, `Color`, `ImageSource` | `IBrush`, `Color`, `IImage`. Solid colour brushes are `ISolidColorBrush`: `Brushes.*` and brushes parsed from text are immutable ones, not `SolidColorBrush` |
| `Color.AreClose(a, b)` | `a == b` (the same for colours made of bytes) |
| `FontFamily.Source` | `WpfCompat.Media.FontFamilyConverter.ToString(family)`, WPF's text form. Not `ToString()`, which adds the font collection's key to some families (`fonts:SystemFonts#Segoe UI`) |
| `FindResource(k)` / `TryFindResource(k)` | `this.FindResource(k)` (Avalonia's, returns `UnsetValue` where WPF throws) / `this.TryFindResource(k)` (WpfCompat's) |
| `control.ToolTip = …` | unchanged, with `using WpfCompat;` |
| `TextBox.CaretIndex` (reading it with a selection) | `Math.Min(SelectionStart, SelectionEnd)`: WPF's is the selection's start. `SelectionLength` is WpfCompat's |
| `TextBox.Text` | may be null in Avalonia (WPF's never is) |
| `ContentRendered` | `Opened`. Avalonia raises it before `Loaded`, so post work that needs layout at `DispatcherPriority.Background` |
| `SizeChanged += H` | `element.AddSizeChangedHandler(H)` (WpfCompat): raised after the layout pass, as WPF's is; Avalonia's runs during it |
| `MouseDoubleClick` | `AddMouseDoubleClickHandler` (WpfCompat): left or right button, handled presses included, as WPF's `Control` raises it |
| `WebBrowser.Navigate(html file)` | Avalonia.HtmlRenderer's `HtmlPanel`: `Text` from the file, relative pictures in `ImageLoad` (see `CalculatorHelpWindow`) |
| `comboBoxItem.IsSelected = true` | the combo box's `SelectedItem`: Avalonia drops `IsSelected` on items its drop-down hasn't shown |
| A binding made in code with `ElementName` | `Source = element`: `ElementName` fails in code |
| `Cursors.Wait`, `Mouse.OverrideCursor` | WpfCompat.Input's |
| `Application.Current.MainWindow` / `Windows` / `Shutdown()` | unchanged, with `using WpfCompat;` (the desktop lifetime's) |
| `SystemParameters.VerticalScrollBarWidth`, drag distances | WpfCompat's `SystemParameters` |
| `BindingOperations.GetBindingExpression` | `BindingOperations.GetBindingExpressionBase` |
| `BindingOperations.ClearBinding(o, P)` | `BindingOperations.GetBindingExpressionBase(o, P)?.Dispose()`: Avalonia's `ClearValue` leaves a binding in place |
| `BindingOperations.SetBinding(o, P, b)`, `new Binding(path)` | `o.Bind(P, b)`; `Binding` (namespace `Avalonia.Data`) keeps `Source`, `Mode`, `StringFormat`, `UpdateSourceTrigger` |
| `GradientStopCollection` | `GradientStops` |
| WPF's `DataGrid` | `GenericControls.CopyPasteDataGrid` (ProDataGrid), never a bare ProDataGrid: it adds WPF's `Items`, `GetRow`/`GetCell`, keyboard, clipboard and validation, and keeps generated columns and their themes across refreshes |
| `ItemContainerGenerator.ContainerFromIndex` (grid rows) | `CopyPasteDataGrid.GetRow(index)` |
| `c.Content.GetType() == typeof(TextBlock)` on a grid cell | `c.Content is TextBlock`: ProDataGrid's text cells are a TextBlock subclass |
| `MoveFocus(new TraversalRequest(…))` | WpfCompat.Input's `FocusNavigation` |
| `System.Windows.Media.Media3D.Point3D` | `WpfCompat.Media.Media3D.Point3D` |
| `FlowDocument.PageWidth` | WpfCompat's `FlowDocument.PageWidth`: the read-only `RichTextBox` lays the document out at that width and scrolls sideways |
| `IsVisible` (visible and in a window) / `IsVisibleChanged` | `IsEffectivelyVisible && TopLevel.GetTopLevel(this) != null`, and watch `IsVisibleProperty` on the control and its ancestors while attached: Avalonia 12's `IsEffectivelyVisibleChanged` is internal (see `MarkovChainTraceControl`) |
| Non-readonly `DependencyProperty` fields | `static readonly` styled properties (Avalonia's analyzer, AVP1000) |
| `Window.GetWindow(this)` | `TopLevel.GetTopLevel(this) as Window` |
| `w.Owner = o; w.Show()` | `w.Show(o)`: Avalonia's `Show()` ignores an owner set beforehand |
| `FrameworkElementFactory` + `ControlTemplate` (code-built templates) | `FuncControlTemplate<T>` that builds the element |
| `FindName("x")` | `FindControl<T>("x")` |
| `DispatcherOperation.Task` | `DispatcherOperation.GetTask()` |
| `Mouse.UpdateCursor()` | nothing: WpfCompat's `Mouse.OverrideCursor` applies at once |
| `DataGrid.UnselectAll()` | `SelectedItem = null`: ProDataGrid's `SelectedItems.Clear()` raises no `SelectionChanged` |
| `VisualTreeHelper.HitTest(v, p).VisualHit` | `v.GetVisualAt(p)` |
| A generated column's `((Binding)c.Binding).StringFormat` / `.Converter` | `((ReflectionBinding)c.Binding)…`: Avalonia 12's `BindingBase` has neither |
| A `CollectionViewSource` with `SortDescriptions` as a combo box's items | `WpfCompat.Data.CollectionViewSource` (its `View` is a `ListCollectionView`), set with `combo.SetItemsSourceKeepingSelection(view)`: a plain set lets a two-way `SelectedValue` write null back. The helper takes the selector's data context away while the items change; don't hold the value at animation priority instead, which pins it |
| `column.Binding.StringFormat = …` in code | cast to `ReflectionBinding`; GenericControls' display copy then applies the format |
| `Application.Current.MainWindow = w` without a desktop lifetime (tests) | WpfCompat keeps it, as WPF's property does |
| A static brush field (`new SolidColorBrush(c)`, frozen in WPF) | an immutable brush (`ImmutableSolidColorBrush`, `Brushes.*`): a mutable one belongs to the thread that first touches the class |
| `host.Content = element.Plot`, for every element-owned plot a view shows (also through the shared diagnostic controls' `SetPlot`) | `host.TakeContent(element.Plot)` (WpfCompat): a document opened again finds the closed view still holding the plot, and Avalonia's `Content` throws while it has a parent |
| `System.Windows.Point` as plot items | Avalonia's `Point` (the same X and Y) |
| A grid column's `Visibility` | `IsVisible`: a column isn't a visual, so WpfCompat's `Visibility` doesn't reach it |
| A code-built `Style` with `new DataTrigger { Binding, Value }` | a `ControlTheme` with a setter of NumericControls' `DataTrigger.BindingProperty` and a nested `new Style(x => x.Nesting().PropertyEquals(DataTrigger.BindingProperty, value))` (see `CompositeAnalysisPropertiesControl.SetDataGridStyle`) |
| `SetResourceReference(P, k)`, then `ClearValue(P)` | dispose the reference's binding first: `BindingOperations.GetBindingExpressionBase(o, P)?.Dispose()` |
| `LiveSortingProperties.Add(p)`, where p is the sort property | drop it: WpfCompat's view sorts live by its sort descriptions' properties |
| Grid rows found with `ItemContainerGenerator.ContainerFromIndex`, without scrolling | the realized `DataGridRow` with that `Index` among the visual descendants (`CopyPasteDataGrid.GetRow` scrolls) |

- Dialogs wait, as in WPF (plan §5.3): `ShowDialog()` and `DialogResult` (WpfCompat's), WpfCompat's
  file and folder dialogs and clipboard, and `GenericControls.MessageBox.Show`. Keep upstream's order
  of operations; don't make handlers async.
- C# reaches extension members only through a receiver: inside a class write `this.CommandBindings`,
  `this.DialogResult`, `this.FindResource(…)`, `this.ToolTip`.
- Metro windows (`GenericControls`) behave as WPF windows when closing: `Close()` after the window has
  closed does nothing, and setting `DialogResult` in a Closing handler doesn't close it again.
- `TextBox` bindings: WPF updates the source on `LostFocus` by default; Avalonia on every change. Add
  `UpdateSourceTrigger=LostFocus` where upstream relied on the default (plan §5.2).

### Code that uses the ported OxyPlot.Wpf

`src/Framework/OxyPlot` keeps upstream's namespace, type and member names, `Serialization` API and saved
XML. Code that uses it changes only where Avalonia's types differ:

| WPF fork | Port |
|---|---|
| `Brush`, `FontWeight`, `Cursor`, `ControlTemplate` properties | Avalonia's `IBrush`, `FontWeight` (an enum), `Cursor`, `IControlTemplate` |
| `ToBitmap()`, `ExportToBitmap()` return a `BitmapSource` | They return an Avalonia `Bitmap`; dispose it |
| `Visibility` on every plot element | `Series` and the tracker lines keep `Visibility` (on series it agrees with `IsVisible`); axes and annotations have only `IsVisible` |
| `PropertyChanged` (`INotifyPropertyChanged`) | The same event, declared `new` over Avalonia's; cast to `AvaloniaObject` for Avalonia's |
| `OnMouse*`, `OnManipulation*` overrides | `OnPointerPressed`, `Moved`, `Released`, `WheelChanged`, `Entered`, `Exited`; no touch gestures |
| `public override OnApplyTemplate()` | `protected override OnApplyTemplate(TemplateAppliedEventArgs)` |
| Clipboard copy | `SetTextAsync` / `SetBitmapAsync`, not awaited |
| `PlotCommands`, `ApplicationCommands.Copy` | WpfCompat's routed commands: `((RoutedCommand)command).Execute(parameter, plot)` |
| `DrawingVisualHost.Visual` (WPF's `DrawingVisual`) | The port's `DrawingVisual`; `RenderOpen()` returns a `DrawingRecorder` |
| `TextFormattingMode` on render contexts | Removed |
| The view's and tracker's styles from `generic.xaml` | Merged into the application's resources when the first plot is made. `Plot.Background` defaults to white, as WPF's style made it |
| XPS and XAML export (`SaveXaml`, `ToXaml`) | Not ported; BestFit doesn't use them |

## 5. Defaults that differ

Already handled for every ported control (Themes' `ThemeService.Initialize`):
- **Font:** Segoe UI 12, falling back to Selawik (`Defaults.axaml`). Every application's builder, the
  test sessions' and tools' included, also calls `.UseThemeFonts()` (`Themes.ThemeFonts`): outside
  Windows, upstream's Segoe UI, Consolas, Cambria and Calibri then draw in their embedded substitutes.
- **Shape stroke:** WPF shapes draw a 1-pixel stroke by default (`WpfDefaults`).
- **Menu separators:** they get WPF's menu style (`Defaults.axaml`).
- **System values:** WPF's colours and sizes, as resources (`Merged.axaml`).

Watch for others when porting:
- **Range controls:** Slider's range is 0 to 10 in WPF, 0 to 100 in Avalonia. Its SmallChange and
  LargeChange are 0.1 and 1 in WPF, 1 and 10 in Avalonia.
- **DrawingBrush:** stretches Fill in WPF, Uniform in Avalonia.
- **Gradient points:** relative points need `%` in Avalonia (`StartPoint="0%,0%"`).
- **Alignment enums:** Avalonia lists Stretch first, so their numbers differ from WPF's.
- **Repeat buttons:** WPF repeats after 500 ms, every 33 ms; Avalonia after 300 ms, every 100 ms.
- **Content alignment:** a WPF Control's content defaults to Left and Top; Avalonia's ContentControl
  stretches it.
- **Hover:** WPF keeps `IsMouseOver` true while the mouse is captured; Avalonia doesn't.

## 6. Avalonia 12 pitfalls

- **Subclasses:** type selectors don't match them. Use `:is(TopLevel)`.
- **Compiled bindings** are off (`AvaloniaUseCompiledBindingsByDefault=false`): reflection bindings, as
  in WPF.
- **Resource lookup:** `{StaticResource}` across `ResourceInclude`d files may not resolve; use
  `{DynamicResource}` for keys in other files. `FindResource` on an element that isn't in a tree yet
  doesn't reach the application's resources.
- **Shared elements:** an element resource used in two places at once (an icon in two menus) needs
  `x:Shared="False"`: WPF quietly moves it, Avalonia throws ("already has a visual parent").
- **`avares://X`** resolves to the first loaded assembly whose name starts with X, so a test assembly
  named `X.Tests` can win if X isn't loaded yet.
- **Scroll bars:** WPF's disables itself when Maximum ≤ Minimum; Avalonia's doesn't. Avalonia's
  ScrollViewer scrolls sideways on Shift+wheel (kept).
- **Handlers named in XAML** must match the event's signature exactly (e.g. `WindowClosingEventArgs`).
  AVLN3001 can only be silenced through `MSBuildWarningsAsMessages`.
- **Attached-property selectors:** they end a compound selector. Write `^[A=x][(ns|T.P)=y]`, not the
  reverse.
- **Windows:** each sits under a TopLevelHost; stop walking up the tree at the window. A window's
  implicit theme is keyed by `Window`, not its own type.
- **Custom chrome:** see `MetroWindowStyle.axaml`.
  - The client area is extended, and the title bar carries `WindowDecorationProperties.ElementRole`.
  - `WindowDecorationsTheme` stops Avalonia drawing its own title bar.
- **Headless tests:**
  - Use `AvaloniaTestIsolationLevel.PerAssembly`. Don't touch Avalonia types from a
    `[ModuleInitializer]`.
  - `dotnet test` hides MSTest's messages; run the test executable to see them.
  - Timers only fire inside a nested dispatcher frame. Type one character per `KeyTextInput`.
  - `Visibility.Hidden` keeps `IsVisible` true: assert WpfCompat's `Visibility`.
  - WpfCompat's clipboard needs a focused window: call `CommandManager.InvalidateRequerySuggested()` and
    `window.Activate()` first.
  - A mouse-down and move shows Dock's docking targets in the window's adorner layer, and
    `NavigatorWindow` can be shown directly.
  - Tests can't implement Avalonia 12's `IActivatableLifetime` or `IStorageFile` (they're marked not
    implementable by user code): test what's built on them through the code's own seams.
- **Popups in captures:** set `Popup.ShouldUseOverlayLayer`. The headless render timer uses real time.
- **Bindings in templates:** a `{Binding}` inside a ControlTemplate applies at LocalValue priority, so
  no style can override it. Use `TemplateBinding`, or a style, where a theme must win.
- **Markup:** `x:Static` can't reach internal members (AVLN2000). A `StaticResource` used for `BasedOn`
  in a dictionary loaded on its own has no application fallback: merge the base theme's dictionary
  first. `$parent[prefix:Type]` in a reflection binding inside a dictionary failed at run time;
  `CompiledBinding` works.
- **Pointer capture:** Avalonia captures the pointer on every press; release it where upstream relies on
  WPF not capturing (the colour picker, the clock).
- **Two-way bindings:** a target set from the source property's own change handler isn't written back
  when it equals the source's old value. Write it back explicitly (see `NumericUpDown`).
- **Lossy two-way bindings:** Avalonia writes a two-way binding's formatted value back when the data
  context changes, so a value shown rounded loses its digits. Grid cells display through one-way
  bindings (`DataGridDisplayBinding`); use one-way bindings wherever the display format loses
  information.
- **A local value doesn't clear a one-way binding:** in WPF, setting a property that has a one-way
  binding clears the binding; Avalonia keeps it, and the next source change overwrites the value.
  The value set is held by the binding, so disposing the binding afterwards resets it to the default.
  Where upstream relies on an edit detaching a binding (axis titles bound to data labels), let the
  binding's converter return `BindingOperations.DoNothing` once something else has set the target
  (see `PlotAxisTitleDefaults.TitleBinding` in RMC.BestFit.UI).
- **Singletons that hold Avalonia objects:** an Avalonia object belongs to the thread that makes it,
  where WPF's frozen brushes belonged to none. Make such objects on first use by the UI rather than in
  a static initializer that any thread may run first (see FrameworkInterfaces' `Messenger`).
- **MSTest 3.6 with parallel tests** can run the assembly cleanup more than once: make it idempotent
  (`Interlocked.Exchange(ref _session, null)?.Dispose()`).
- **`CacheMode="BitmapCache"`** keeps the first bitmap of content that draws itself: don't put it on
  content that changes (OxyPlot.Wpf's `DrawingVisualHost.UseBitmapCache` keeps the preference but sets
  no cache).
- **A column's `HeaderTheme="{DynamicResource …}"`** is null until the grid is in a window, and a local
  value doesn't clear the resource binding (see `ParameterPriorsControl.HeaderThemeOf`).
- **`ElementName` inside a DataTemplate** can find names in the enclosing XAML: Avalonia's template name
  scope falls back to the control's.
- **`Environment.NewLine` is CR LF only on Windows.** RMC-BestFit builds its plots' tracker formats
  with it, and its projects were saved on Windows, so on macOS and Linux a view setting its format
  would change the saved plot and mark the project unsaved. OxyPlot.Wpf's `Series` keeps a saved CR LF
  format when the new one differs only in bare line feeds (`CoerceTrackerFormatString`). Any other
  string that's saved and built with `Environment.NewLine` needs the same care.
- **Resource elements handed out by code** (`new ContentControl { Content = TryFindResource(k) }`) need
  `x:Shared="False"`, or the first parent's whole tree (a document, its element, its project) stays
  alive through the shared element.
- **WpfCompat's `CollectionViewSource`** sorts by property paths (`"Alternative.Name"`), live and through
  weak events; a path that doesn't resolve keeps the collection's order, as in WPF.
- **DataTable views in grids:** Avalonia's reflection binding can't choose among `DataRowView`'s or
  `DataRow`'s overloaded indexers, so `[column]` paths show nothing, and ProDataGrid's own generated
  columns for a `DataView` are affected too. Bind each column with
  `CompiledBinding.Create<DataRowView, object>(row => row[name], …)` and set its `IsReadOnly` (left unset,
  ProDataGrid's writability check on "Item" throws `AmbiguousMatchException`). The compiled binding gets
  no change notifications, so refresh the grid after writing to rows, as CopyPasteDataGrid's paste does.
- **A check box's change event comes before its binding writes back:** Avalonia's toggle button raises
  `IsCheckedChanged` before a two-way binding writes the source; WPF writes first, then raises `Checked`.
  GenericControls' `BooleanPropertyControl` takes the state into `IsSelected` first, so upstream's
  handlers read the new value from the element. A plain `CheckBox`'s handlers still run before the
  write-back.
- **Undo can replace a model:** the Point Process, Mixture and other elements restore their model from an
  XML snapshot, a new object, and announce it under the model's name (`PointProcess`,
  `MixtureDistribution`). Re-read the element's model after an undo.
- **Inherited properties change when a control joins or leaves the tree** (`FontFamily`, say): handlers
  that set a dirty flag or record undo must ignore changes that don't change what's saved (see
  OxyPlot.Wpf's `Series`).
- **Avalonia's selectors drop a moved item from their selection:** WpfCompat's live-sorted view
  announces the moves of the items the moved one passes instead.
- **A two-way target that can't keep a null it's shown gives back its default** (a value type's default,
  a brush of the default colour): `TwoWayConverter` treats that as an echo.
- **Closing a test window brings `SimpleTheme`'s window font back,** and plots on hidden tabs inherit it:
  take the content out first, as the shell does.
- **A plain ProDataGrid** shows no rows when given unlimited height (in a ScrollViewer or an Auto row),
  and its generated columns ignore a `StringFormat` set in `AutoGeneratingColumn`: `CopyPasteDataGrid`
  handles both.
- **Headless clicks in the shell:** move the pointer before pressing, as a mouse does. MainWindow notes
  the pane under the pointer on mouse moves, and an explorer click relies on it to keep its selection
  when it activates the node's document (see `BestFitShell.Click`).
- **`[STATestMethod]` has no synchronization context,** so `async void` handlers continue on the thread
  pool: drive such windows with `HeadlessSession.Run` and pump the dispatcher. Two quick clicks on one
  spot are a double click (which opens an explorer node's document). Start WpfCompat's
  `CommandManager` before focusing anything in a test.
- **`x:Shared="False"` resources** give a new instance per lookup: compare icons by their drawing, not
  by reference.
- **Without a desktop lifetime (headless tests),** WpfCompat's `Modal.ActiveWindow` can return a window
  an earlier test closed, so a dialog fails with "Cannot show a window with a closed owner": click into
  the test's own window first.
- **Grid themes:** set scroll bar visibility to `Auto`, WPF's default (ProDataGrid's is `Disabled`). A
  bare `DataGrid` gets the header-height fix only once a `CopyPasteDataGrid` exists: another reason to
  always use `CopyPasteDataGrid`. Grids laid out with unbounded height log layout-cycle warnings; that
  is ProDataGrid's, with or without the port's code.
- **ProDataGrid:** it moves the current cell after your KeyDown handlers have run, so react in
  `CurrentCellChanged`. It commits an edit in progress when its items source changes, so don't reset
  the items from `CellEditEnding` (see `StringListPropertyControl`).
- **Dock's AvalonDock adapter:** it sets some templates (captions, pane content, side tabs) through
  application and window Styles, which a ControlTheme can't replace: restyle their parts, and where a
  template sets a value itself, override it with a nested style that has a condition. Its themes go on
  `DockingManager.Theme`, which reaches floating, auto-hide and navigator windows (see
  `FrameworkUI/Themes/VS2013/Dock`).
- **Dispatchers:** as in WPF, each object belongs to the dispatcher of the thread that made it, and the
  first thread to create a dispatcher becomes the UI thread. A test project whose tests run in parallel
  must start its headless application in `[AssemblyInitialize]`, before any test makes an Avalonia
  object on a worker thread.

## 7. Tests and checks

- **Agents sharing a machine:** `taskkill /IM` by image name kills every agent's test run, since every
  worktree's test executable has the same name. Run tests from a renamed copy of the test executable
  (`cp RMC.BestFit.App.Tests.exe mine-apptests.exe`), which still loads the right assemblies. Close
  windows and projects between tests: a full App run should stay within a few GB.
- **CI runs the App tests with `scripts/run-app-tests.sh`:** each of XGoodFit's test classes
  (`XGoodFit/*Tests.cs` and `*Captures.cs`, a class per file of the same name) in its own process, then
  upstream's tests together; `dotnet test` leaves the project out with `-p:XGoodFitAppTestsInScript=true`.
  Run a class as CI does with `bash scripts/run-app-tests.sh Release <Class>`.
- **View tests** merge Themes' `Defaults.axaml` and `Controls/Merged.axaml` as ThemeService does, and
  use the examples' en-US culture.
- **MSTest runs `[TestCleanup]` on its own thread** for `[TestMethod]` tests (which use
  `HeadlessSession.Run`), so a cleanup that touches Avalonia hangs there: don't mix those with
  `[STATestMethod]` tests in a class whose cleanup does UI work.
- **In the main window,** moving the headless pointer over a plot toolbar re-lays the document out, so a
  press can land on another button: raise `Click` on shell buttons, test pointer clicks on a view hosted
  directly, and guard dialogs with `BestFitShell.WhenOpened`.
- **Memory:** close what a test opens through the helpers, in `finally` or `[TestCleanup]`. The whole App
  project in one process peaks around 1.7 GB, and each class (CI's unit) well under that.
  - `BestFitShell.Close(window)` releases the main window (`BestFitShell.Release`: the window, its explorer,
    its menus and any control still listening to the singleton project) and collects. To open several
    examples in one window, use `BestFitShell.OpenExample(window, example)`.
  - Dispose `ViewTesting.Open`'s project, and call `SharedControlsTesting.Close(project)` and
    `ReleaseAppResources()`: they clear the Messenger, which keeps every message's source (an element, so
    its project and views), and collect (`ViewTesting.Collect`).
  - Show views with `ViewTesting.Show` or `SharedControlsTesting.Show`. For a window you make yourself, call
    `ViewTesting.ReleaseContentOnClose(window)`: a closed window still in memory keeps re-resolving its
    resources on every resource change (it made the Input Data class take 212 s instead of 37 s).
  - Clear fields that hold windows, projects or views in `[TestCleanup]`: MSTest keeps the test class's
    instance. Merge resource dictionaries fresh for each test; don't cache them across tests.
  - A view costs about 1 KB per observation, so keep the largest records out of default runs
    (`[TestCategory("Large")]`). FrameworkInterfaces' Messenger forgets a source once it has no messages
    (upstream kept it, and with it the element and its project).
- **Leak checks:** follow `ReleaseTests`. Make the objects in a `[MethodImpl(MethodImplOptions.NoInlining)]`
  method, sleep about 150 ms, tick the render timer, run the dispatcher's jobs, collect, and check the weak
  references.
- **The headless UI thread is a thread-pool thread,** so `Task.Run(…).GetAwaiter().GetResult()` on it runs
  inline: use `new Thread`. Click async handlers in `[STATestMethod]`s with
  `AvaloniaSynchronizationContext.InstallIfNeeded()`. In tests, set a combo box's `SelectedItem`, not its
  `SelectedValue` (which raises SelectionChanged before the two-way binding writes back).
- **The examples' native screenshots are factory-reset plot exports:** compare their data (`.json.gz`),
  not their styling. Exports and walkthrough figures can relabel series (their `displayCorrections`):
  map the labels back. A walkthrough figure can disagree with the saved results that the app shows;
  the native export is then the reference.
- **Open each view twice:** upstream's MainProjectNode leaves a closed document's view holding its
  element, so a document opened again must take the plots from it.
- **In `WeakReference` leak checks,** a control closed right after a state change stays referenced by the
  animation clock until a render tick with real time elapsed: sleep and tick before collecting.
- **A model setter with side effects can record several undos** (`IsNonstationary` also sets
  `ParameterTimeIndex`): expect upstream's undo steps.
- **Waiting for reprocessed results:** the shared diagnostic controls clear the wait cursor too, and an
  earlier queued notification can clear it first. Wait for the view's own update (a table row, a series
  title), not only for `Mouse.OverrideCursor`.
- **Double-clicking the same spot again** within 500 ms continues the click count (headless input uses real
  time), and a third click opens nothing: wait before double-clicking an explorer node again.
- **Shell test classes:** open each example once per class where you can (see
  `PointProcessMixtureTesting.SharedExample`), and declare the main-window captures first: their theme
  switches restyle whatever earlier shell tests left alive.
- **Is a figure stale?** A walkthrough figure's `source.runId` and a native export's `sourceSha256` are the
  SHA-256 of the project file they were made from. When they differ from the shipped file's hash, the figure
  shows an earlier fit: check that before calling a difference a port bug.
- **Rating curve and time series figures:** horizontal bands are `xLower`/`xUpper` at each `y`; bars are `x`
  (the centre), `width` and `y` (the height, OxyPlot's `HistogramItem.Value`); two-point lines are the plot's
  line annotations (a vertical line's X, a horizontal line's Y, or a sloped line's equation); dates are ISO
  strings (compare their OLE dates); bands are titled "Prediction Intervals" where the plots say "Credible
  Intervals". `RatingCurveTimeSeriesTesting.AssertWalkthroughFigure` reads all of this.
- **Undo and the fit:** a model or prior change clears the fit, and undo restores the model but not its
  results, so test result-dependent edits first.
- **Unload and reload:** taking a window's content out and back in unloads and reloads it, as a docked
  panel's theme change does (`RatingCurveTimeSeriesTesting.Reload`).
- **Headless time:** input is stamped with the real time, and the dispatcher's timers don't run during the
  tests' job runs. `BestFitShell.DoubleClick` therefore selects an explorer node before double-clicking it
  (a slow first press would outlast the double-click time) and lets the node's slow-click rename timer run
  out (a click while it runs starts renaming the node, which then opens nothing). The capture waits
  (150-300 ms) are needed, and captures aren't byte-comparable between runs: window titles show temporary
  paths, the batch window shows elapsed time, and the bivariate contours race.
- **Factory-reset exports** lack the annotations users saved (Waimea's levee line), and the mixture
  walkthrough's figures use logarithmic value axes where the saved plots are linear: compare series data.
- **Standalone captures** have no value format set: set `UserSettings.DefaultValueDigits = 2`, and restore
  it afterwards.
- **The example tour** (`XGoodFit/ExampleTour*Tests.cs`, `scripts/run-app-tests.sh Release Tour`) opens
  every element of every example through the main window and checks that looking changes nothing. It runs
  in the machine's culture, so on en-AU it also meets the stored dates that don't parse there.
- **Git Bash's `timeout` doesn't stop a test `.exe`** (hung runs kept going at several GB): stop it by its
  renamed image name.

- **Tests that need natives or downloads:** `tests/Hec.Dss.Tests` skips when the natives or sample
  data are missing, and fails instead under an environment variable that CI sets
  (`XGOODFIT_REQUIRE_HECDSS=1`). Upstream's own tests can't skip: RMC.BestFit.UI.Tests' DSS tests need
  the natives and two of its tests need the example projects. So before `dotnet test`, run
  `native/build-hecdss.sh` and `scripts/fetch-sample-data.sh RMC-BestFit dss-test-data`, as CI does. To compare with an upstream release, load its
  binaries in their own `AssemblyLoadContext` (see `tests/Hec.Dss.Tests`).
- **Tests:** each ported project gets a headless MSTest project in `tests/`, copying
  `HeadlessSession.cs` from an existing one. Test what a screenshot can't show: commands, state, layout
  rules.
- **File compatibility:** `tests/BestFit/RMC.BestFit.FileCompat.Tests` opens and saves every example
  project with the port and compares each saved file, cell by cell, with what upstream's WPF
  RMC-BestFit saves (golden data from `tools/BestFitFileCompat.Wpf`, Windows only; see its
  `Golden/README.md` to regenerate). XML that RMC-BestFit stores has the OS's line ends, so cross-OS
  comparisons normalise them. To build a WPF reference tool, compile shared source into both it and the
  tests, build the upstream WPF project in place with `AdditionalProperties` and
  `DisableTransitiveProjectReferences`, and give builds with other global properties their own
  `--artifacts-path`.
- **Gallery:** `tools/ThemeGallery` shows controls in all three themes, and `capture <dir>` renders
  PNGs for review. Captures that need a test's setup are tests in category `Capture` (e.g.
  `FrameworkUI.Tests --filter "TestCategory=Capture"` for the docking chrome), writing to
  `artifacts/screenshots`.
- **Before committing:** `dotnet build XGoodFit.slnx -c Release` with no warnings, and
  `dotnet test XGoodFit.slnx -c Release --no-build`. CI runs both on Windows, macOS and Linux.
- **Commits:** clear messages, ending with the co-author trailer the repository uses.
