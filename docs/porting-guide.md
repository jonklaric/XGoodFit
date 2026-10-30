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
- XAML is always ported: `Foo.xaml` becomes `Foo.axaml` at the same relative path, and `Foo.xaml.cs`
  becomes `Foo.axaml.cs`. Upstream `*.xaml.cs` files never compile.
- Keep namespaces, class names, `x:Class`, element names, event-handler names and resource keys
  exactly as upstream, so the rest of upstream's code compiles against the port.
  - Example: the plot library keeps the `OxyPlot.Wpf` namespace even though it's Avalonia code. Plot
    XML in `.bestfit` files records type names such as `OxyPlot.Wpf.LinearAxis`, so a different
    namespace would break file compatibility.
- New projects go in `XGoodFit.slnx`, and tests in `tests/` (MSTest.Sdk, headless; see below).
- `scripts/porting/xgf_port.py` ports a file by exact, checked replacements: it copies the upstream
  file and fails if a replacement doesn't match, so everything else stays as upstream wrote it.

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
| `x:FieldModifier="public"` | `internal` unless another assembly uses the field: Avalonia's generated fields have no doc comments (a warning) |
| `StrokeStartLineCap` / `StrokeEndLineCap` | `StrokeLineCap` (one for both) |
| `Line X1 Y1 X2 Y2` | `StartPoint="x1,y1"` `EndPoint="x2,y2"` |
| `LayoutTransform` | wrap in `LayoutTransformControl` |
| `RenderTransformOrigin="0.5,0.5"` | `RenderTransformOrigin="50%,50%"`: Avalonia reads numbers without `%` as pixels |
| `StrokeDashArray="1 2"` | `StrokeDashArray="1,2"` |
| `Command="Save"` (ApplicationCommands) | `Command="{wpfi:Command Save}"`, with `xmlns:wpfi="using:WpfCompat.Input"` |
| `x:Static SystemParameters.…` / `SystemColors.…` | the fixed resources in Themes' `Merged.axaml` (`SystemParameters.VerticalScrollBarWidth`, …) |
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
| `x.SetResourceReference(P, key)` | unchanged, with `using WpfCompat;` |
| `Dispatcher.Invoke` / `BeginInvoke` | `Dispatcher.UIThread.Invoke` / `Post` |
| `VisualTreeHelper` | `GetVisualParent`, `GetVisualChildren`, `FindAncestorOfType<T>` (`Avalonia.VisualTree`) |
| `x.Visibility = Visibility.Hidden` | unchanged, with `using WpfCompat;` (an extension property that agrees with `IsVisible`) |
| `CommandBindings.Add(…)`, `RoutedCommand`, `ApplicationCommands`, `SystemCommands`, `ExecutedRoutedEventArgs` | WpfCompat's (`using WpfCompat.Input;`) |
| `Window.ResizeMode` | `WpfWindow.ResizeMode` (WpfCompat). Metro windows have it as a property |
| `Brush`, `Color`, `ImageSource` | `IBrush`, `Color`, `IImage`. Solid colour brushes are `ISolidColorBrush`: `Brushes.*` and brushes parsed from text are immutable ones, not `SolidColorBrush` |
| `Color.AreClose(a, b)` | `a == b` (the same for colours made of bytes) |
| `FontFamily.Source` | `ToString()` |
| `FindResource(k)` / `TryFindResource(k)` | `this.FindResource(k)` (Avalonia's, returns `UnsetValue` where WPF throws) / `this.TryFindResource(k)` (WpfCompat's) |
| `control.ToolTip = …` | unchanged, with `using WpfCompat;` |
| `TextBox.CaretIndex` (reading it with a selection) | `Math.Min(SelectionStart, SelectionEnd)`: WPF's is the selection's start. `SelectionLength` is WpfCompat's |
| `TextBox.Text` | may be null in Avalonia (WPF's never is) |
| `ContentRendered` | `Opened` |
| `Cursors.Wait`, `Mouse.OverrideCursor` | WpfCompat.Input's |
| `Application.Current.MainWindow` / `Windows` / `Shutdown()` | unchanged, with `using WpfCompat;` (the desktop lifetime's) |
| `SystemParameters.VerticalScrollBarWidth`, drag distances | WpfCompat's `SystemParameters` |
| `BindingOperations.GetBindingExpression` | `BindingOperations.GetBindingExpressionBase` |

- Dialogs wait, as in WPF (plan §5.3): `ShowDialog()` and `DialogResult` (WpfCompat's), WpfCompat's
  file and folder dialogs and clipboard, and `GenericControls.MessageBox.Show`. Keep upstream's order
  of operations; don't make handlers async.
- C# reaches extension members only through a receiver: inside a class write `this.CommandBindings`,
  `this.DialogResult`, `this.FindResource(…)`, `this.ToolTip`.
- Metro windows (`GenericControls`) behave as WPF windows when closing: `Close()` after the window has
  closed does nothing, and setting `DialogResult` in a Closing handler doesn't close it again.
- `TextBox` bindings: WPF updates the source on `LostFocus` by default; Avalonia on every change. Add
  `UpdateSourceTrigger=LostFocus` where upstream relied on the default (plan §5.2).

## 5. Defaults that differ

Already handled for every ported control (Themes' `ThemeService.Initialize`):
- **Font:** Segoe UI 12, falling back to Selawik (`Defaults.axaml`).
- **Shape stroke:** WPF shapes draw a 1-pixel stroke by default (`WpfDefaults`).
- **Menu separators:** they get WPF's menu style (`Defaults.axaml`).
- **System values:** WPF's colours and sizes, as resources (`Merged.axaml`).

Watch for others when porting:
- **Range controls:** Slider's range is 0 to 10 in WPF, 0 to 100 in Avalonia.
- **Repeat buttons:** WPF repeats after 500 ms, every 33 ms; Avalonia after 300 ms, every 100 ms.
- **Content alignment:** a WPF Control's content defaults to Left and Top; Avalonia's ContentControl
  stretches it.
- **Hover:** WPF keeps `IsMouseOver` true while the mouse is captured; Avalonia doesn't.

## 6. Avalonia 12 pitfalls

- **Subclasses:** type selectors don't match them. Use `:is(TopLevel)`.
- **Compiled bindings** are off (`AvaloniaUseCompiledBindingsByDefault=false`): reflection bindings, as
  in WPF.
- **Resource lookup:** `{StaticResource}` across `ResourceInclude`d files may not resolve; use
  `{DynamicResource}` for keys in other files.
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
- **Popups in captures:** set `Popup.ShouldUseOverlayLayer`. The headless render timer uses real time.

## 7. Tests and checks

- **Tests:** each ported project gets a headless MSTest project in `tests/`, copying
  `HeadlessSession.cs` from an existing one. Test what a screenshot can't show: commands, state, layout
  rules.
- **Gallery:** `tools/ThemeGallery` shows controls in all three themes, and `capture <dir>` renders
  PNGs for review.
- **Before committing:** `dotnet build XGoodFit.slnx -c Release` with no warnings, and
  `dotnet test XGoodFit.slnx -c Release --no-build`. CI runs both on Windows, macOS and Linux.
- **Commits:** clear messages, ending with the co-author trailer the repository uses.
