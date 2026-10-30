# Themes' TabControl: TabStripPlacement Bottom and Right leave the tabs over the content

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/Themes/Resources/Controls/TabControl.xaml`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source. XGoodFit's port of the template reproduces the same layout.

## Summary

The template wraps the tab panel (`headerPanel`) in a `Border` (`headerPanelBorder`) that is the grid's child. The `TabStripPlacement` triggers move `headerPanel` with `Grid.Row`/`Grid.Column`, but those attached properties only affect the grid's direct children, so the border never moves. With Bottom, the content is moved to row 0, where the tabs also stay. With Right, the content goes to column 1 and the tabs stay in column 0, the left. Top and Left work only because they match the border's default cell.

## Details and how to reproduce

```xml
<TabControl TabStripPlacement="Bottom"> <TabItem Header="One"/> <TabItem Header="Two"/> </TabControl>
```

**Expected:** tabs below the content. **Actual:** tabs drawn over the top of the content, and the bottom row empty. With `Right` the tabs appear on the left.

## Why this is an issue and not intended behaviour

The triggers set every value needed to move the strip (rows, columns, margins), clearly to support all four placements. They just target the inner panel instead of the border.

## Where in the source

[`TabControl.xaml` lines 14-66](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Controls/TabControl.xaml#L14-L66)

```xml
<Border x:Name="headerPanelBorder" ...>                         <!-- the grid's child: never moved -->
    <TabPanel x:Name="headerPanel" Grid.Column="0" Grid.Row="0" .../>
</Border>
...
<Trigger Property="TabStripPlacement" Value="Bottom">
    <Setter Property="Grid.Row" TargetName="headerPanel" Value="1"/>   <!-- no effect: not a grid child -->
    <Setter Property="Grid.Row" TargetName="contentPanel" Value="0"/>
```

## Suggested fix

Point the `Grid.Row`/`Grid.Column` setters (lines 40, 47, 49 and the Right trigger from line 58) at `headerPanelBorder` instead of `headerPanel`, or move the attached properties to the border. RMC-BestFit uses only Top and Left, which keep working.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) reproduces the same placement in its port, [`TabControl.axaml`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/Themes/Resources/Controls/TabControl.axaml) (header comment, lines 7-11).
