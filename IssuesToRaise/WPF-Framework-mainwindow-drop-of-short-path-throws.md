# Dragging a file with a short path onto the main window throws ArgumentOutOfRangeException

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkUI/Main Window/MainWindow.xaml.cs` (`MainWindow_DragEnter`). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source; `"D:\\data".Substring(7 - 8)` throws.

## Summary

`MainWindow_DragEnter` takes the file's extension with `fullFileName.Substring(fullFileName.Length - ShellPublicVariables.SoftwareExtension.Length)`. A dragged path shorter than the extension (8 characters for `.bestfit`, as in `D:\data` or `E:\a.txt`) gives a negative start index and throws while the user is dragging. The check is also case-sensitive, so `PROJECT.BESTFIT` is refused.

## Details and how to reproduce

Drag a file from the root of a drive with a short name (for example `D:\data`) over RMC-BestFit's main window. **Expected:** the drop is refused (no-drop cursor). **Actual:** `ArgumentOutOfRangeException` from `String.Substring` inside the drag handler, which reaches the application's unhandled-exception handling.

## Why this is an issue and not intended behaviour

The handler's purpose is to accept or refuse the drop. An exception for an ordinary file can't be intended, and Windows file names are case-insensitive.

## Where in the source

[`MainWindow.xaml.cs` lines 1422-1447](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L1422-L1447)

```csharp
string fullFileName = files[0].ToString();
string ext = fullFileName.Substring(fullFileName.Length - ShellPublicVariables.SoftwareExtension.Length);   // line 1437
if (ext == ShellPublicVariables.SoftwareExtension)
```

## Suggested fix

```csharp
string ext = Path.GetExtension(files[0]);
if (string.Equals(ext, ShellPublicVariables.SoftwareExtension, StringComparison.OrdinalIgnoreCase))
```

No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the check in its FrameworkUI shell port (branch `t3/port-frameworkui-shell`, not yet on `main`).
