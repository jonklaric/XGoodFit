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

- **Tests that need natives or downloads:** `tests/Hec.Dss.Tests` skips when the natives or sample
  data are missing, and fails instead under an environment variable that CI sets
  (`XGOODFIT_REQUIRE_HECDSS=1`). Upstream's own tests can't skip: RMC.BestFit.UI.Tests' DSS tests need
  the natives and two of its tests need the example projects. So before `dotnet test`, run
  `native/build-hecdss.sh` and `scripts/fetch-sample-data.sh RMC-BestFit dss-test-data`, as CI does. To compare with an upstream release, load its
  binaries in their own `AssemblyLoadContext` (see `tests/Hec.Dss.Tests`).
- **Tests:** each ported project gets a headless MSTest project in `tests/`, copying
  `HeadlessSession.cs` from an existing one. Test what a screenshot can't show: commands, state, layout
  rules.
- **Gallery:** `tools/ThemeGallery` shows controls in all three themes, and `capture <dir>` renders
  PNGs for review. Captures that need a test's setup are tests in category `Capture` (e.g.
  `FrameworkUI.Tests --filter "TestCategory=Capture"` for the docking chrome), writing to
  `artifacts/screenshots`.
- **Before committing:** `dotnet build XGoodFit.slnx -c Release` with no warnings, and
  `dotnet test XGoodFit.slnx -c Release --no-build`. CI runs both on Windows, macOS and Linux.
- **Commits:** clear messages, ending with the co-author trailer the repository uses.
