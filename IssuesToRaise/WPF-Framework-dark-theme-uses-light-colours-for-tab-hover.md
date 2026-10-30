# The Dark theme shows a light-blue tab hover: FrameworkUI's Dark palette carries four Light-theme values

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkUI/Themes/VS2013/DarkTheme.xaml` and `src/Themes/Resources/Colors/DarkColors.xaml`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source. A script compared every key the two Dark palettes share.

## Summary

`ThemeManager.SetTheme` merges FrameworkUI's VS2013 palette after Themes' palette, so FrameworkUI's values win. Of the 231 keys FrameworkUI's Dark palette shares with Themes' Dark palette, four differ, and all four equal Themes' **Light** values. One is used: `TabItem.MouseOver.Background` is #C9DEF5 (light blue) instead of Dark's #3E3E40, so hovered tabs in Dark flash light blue. Themes' own Dark palette has the opposite problem: its hover text #1E1E1E on #3E3E40 has a contrast ratio of only 1.6:1.

## Details and how to reproduce

Switch an application that uses FrameworkUI (RMC-BestFit, say) to Dark and hover over an unselected tab. **Expected:** a dark hover, as in Visual Studio 2013's Dark theme. **Actual:** a light-blue (#C9DEF5) tab with dark text (#1E1E1E). That's readable (12.1:1), but it's the Light theme's look.

| Key | FrameworkUI Dark | Themes Dark | Themes Light |
|---|---|---|---|
| `TabItem.MouseOver.Background` | **#C9DEF5** | #3E3E40 | #C9DEF5 |
| `PropertyGrid.Input.Background.Enabled` | **#FFFFFF** | #333337 | #FFFFFF |
| `PropertyGrid.Input.Background.Disabled` | **#F5F5F5** | #2D2D30 | #F5F5F5 |
| `PropertyGrid.Content.DividerLines` | **#EEEEF2** | #3F3F46 | #EEEEF2 |

The three `PropertyGrid.*` keys aren't used by any control in WPF-Framework or RMC-BestFit, so only the tab hover is visible today. The Light and Blue palettes have no Light-for-Dark mix-ups; Blue's two differing keys (`TextBox.*.Border` #E5C365) are VS2013 Blue's gold accent and look deliberate.

## Why this is an issue and not intended behaviour

The four values are exactly the Light palette's. That looks like a copy of Light that was never updated for Dark: every other overlapping Dark key matches Themes' Dark values. And if FrameworkUI's override were removed, Themes' Dark hover pair (#1E1E1E text on #3E3E40) would be unreadable (1.6:1). That suggests `TabItem.MouseOver.Foreground` in Themes' Dark palette is also a Light leftover. Dark's static tab text is #F1F1F1, which would give 9.4:1 on #3E3E40.

## Where in the source

- [`src/FrameworkUI/Themes/VS2013/DarkTheme.xaml` line 247](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Themes/VS2013/DarkTheme.xaml#L247): `<SolidColorBrush x:Key="TabItem.MouseOver.Background" Color="#C9DEF5" />`
- [`src/Themes/Resources/Colors/DarkColors.xaml` lines 351-352](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Colors/DarkColors.xaml#L351-L352): `TabItem.MouseOver.Background` #3E3E40, `TabItem.MouseOver.Foreground` #1E1E1E.
- The merge order: [`src/FrameworkUI/Themes/ThemeManager.cs` lines 140-148](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Themes/ThemeManager.cs#L140-L148).
- The hover trigger: [`src/Themes/Resources/Controls/TabControl.xaml` lines 176-182](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Controls/TabControl.xaml#L176-L182).

## Suggested fix

In FrameworkUI's `DarkTheme.xaml`, use the Dark values (or delete the four keys so Themes' values apply). In Themes' `DarkColors.xaml`, set `TabItem.MouseOver.Foreground` to a light colour such as #F1F1F1. Only colours change; nothing is saved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps both palettes and the merge order ([`DarkTheme.axaml` line 252](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/FrameworkUI/Themes/VS2013/DarkTheme.axaml#L252)). Its shell test notes the override as an upstream quirk ([`ShellTests.cs` line 69](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/tests/Framework/FrameworkUI.Tests/ShellTests.cs#L69)).
