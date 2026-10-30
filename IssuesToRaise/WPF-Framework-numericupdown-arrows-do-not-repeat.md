# NumericUpDown's arrow buttons step once per click and don't repeat when held

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/GenericControls/General Controls/NumericUpDown.xaml`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source, and the WPF harness confirmed that the arrows (`cmdUp`, `cmdDown`) are `System.Windows.Controls.Button`.

## Summary

The up and down arrows are plain `Button`s, so holding one down changes the value once. The class says it mimics the WinForms `NumericUpDown`, whose arrows keep stepping while held, and WPF has `RepeatButton` for exactly this.

## Details and how to reproduce

Put a `NumericUpDown` with `Minimum="0" Maximum="1000"` in a window and hold the mouse on the up arrow. **Expected:** the value keeps increasing until release, as in WinForms and in WPF's own `ScrollBar` arrows. **Actual:** it increases by `Increment` once, on release (a `Button` raises `Click` when the mouse button goes up).

## Why this is an issue and not intended behaviour

The class's remarks say: "This control is meant to mimic the behavior of the Winforms NumericUpDown control" ([`NumericUpDown.xaml.cs` lines 20-23](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericUpDown.xaml.cs#L20-L23)). WinForms' `NumericUpDown` repeats while an arrow is held, and stepping a value over a large range one click at a time is tedious.

## Where in the source

[`src/GenericControls/General Controls/NumericUpDown.xaml` lines 70-85](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericUpDown.xaml#L70-L85)

```xml
<Button x:Name="cmdUp" Grid.Row="0" Style="{StaticResource SpinnerButtonStyle}" Margin="0,0,0,0.5" Click="cmdUp_Click">
...
<Button x:Name="cmdDown" Grid.Row="1" Style="{StaticResource SpinnerButtonStyle}" Margin="0,0.5,0,0" Click="cmdDown_Click">
```

## Suggested fix

Use `RepeatButton` (it derives from `ButtonBase`, so the `Click` handlers stay the same) and retarget `SpinnerButtonStyle` to `ButtonBase` or `RepeatButton`:

```xml
<RepeatButton x:Name="cmdUp" Grid.Row="0" Style="{StaticResource SpinnerButtonStyle}" Margin="0,0,0,0.5" Click="cmdUp_Click">
```

`RepeatButton`'s default `Delay` and `Interval` already follow the user's keyboard repeat settings.

Any code that refers to `cmdUp` or `cmdDown` as `Button` would need updating. The fields are generated and internal, so this only affects the assembly itself.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps plain buttons, as upstream: [`NumericUpDown.axaml`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/GenericControls/General%20Controls/NumericUpDown.axaml), header comment ("each click steps once, with no repeat").
