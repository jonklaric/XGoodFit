# A project's saved window layout is ignored unless the user's default layout file already exists

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkUI/Main Window/MainWindow.xaml.cs` (`LoadLayout`). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source; also seen in XGoodFit's port of the shell.

## Summary

`LoadLayout` restores the project's own layout (`Project.AvalonDockLayout`, stored in the `.bestfit` file) only inside `else if (File.Exists(ShellPublicVariables.DefaultAvalonDockLayoutFilePath))`. If the per-user default layout file doesn't exist, the embedded default layout is used and the project's layout is ignored. That happens on a fresh install, a new machine, or after the settings folder is cleared.

## Details and how to reproduce

1. Delete the user's `AvalonDock.xml` from the layout folder (`ShellPublicVariables.AvalonDockLayoutFolderPath`), or use a new Windows account.
2. Open a project that was saved with a customised layout, for example one received from a colleague.

**Expected:** the project's saved panes and documents. **Actual:** the built-in default layout. The project's layout is used only from the next open, after a save has written the default file.

## Why this is an issue and not intended behaviour

The project's layout doesn't depend on the default file. The file is only the fallback when a project has no layout, as the inner branch shows. The method even has a dual-buffer recovery path for the project's layout, which this condition skips entirely.

## Where in the source

[`MainWindow.xaml.cs` lines 650-748](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L650-L748)

```csharp
else if (File.Exists(ShellPublicVariables.DefaultAvalonDockLayoutFilePath))   // line 663
{
    if (ProjectNode == null || ProjectNode.Project == null || ProjectNode.Project.AvalonDockLayout == null)
    { /* load the default file */ }
    else
    { /* load the project's own layout, with dual-buffer recovery */ }
}
else
{
    // Load the Default Resource Avalon Dock layout   (lines 739-747)
}
```

## Suggested fix

Check the project's layout first:

```csharp
if (loadDefaultResource) { /* embedded */ }
else if (ProjectNode?.Project?.AvalonDockLayout is { Length: > 0 }) { /* project layout, with recovery */ }
else if (File.Exists(ShellPublicVariables.DefaultAvalonDockLayoutFilePath)) { /* user default */ }
else { /* embedded */ }
```

The layout column is read as before, so no file format changes.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps this in its FrameworkUI shell port (branch `t3/port-frameworkui-shell`, not yet on `main`).
