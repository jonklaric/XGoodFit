# The Select by Attribute help page links two images that only exist in the Field Calculator's help

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/DatabaseControls/Resources/Help/SelectByAttribute/SelectByAttribute.html` and `src/DatabaseControls/FieldCalculator/CalculatorHelpWindow.xaml.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** documentation · **Severity:** low

**Verification:** verified by reading the source and listing the help folders.

## Summary

`SelectByAttribute.html` shows `AvailableFields.png` and `Functions.png`, but those files are only in `Resources/Help/FieldCalculator/`. The help window extracts only the resources under the page's own prefix into `%TEMP%\DatabaseControls_Help`, so the two images are broken. They appear only if the Field Calculator's help was opened earlier, because both pages share that temporary folder.

## Details and how to reproduce

Clear `%TEMP%\DatabaseControls_Help`, open a table, then Select by Attribute > Help. **Expected:** four pictures. **Actual:** two broken images (sections "Available fields" and "Functions"). Open the Field Calculator's help once and the images then show in the other page as well.

## Why this is an issue and not intended behaviour

The page refers to the images by relative name, which works only if they sit next to it, and the embedded-resource list for this page doesn't include them.

## Where in the source

- [`SelectByAttribute.html` lines 22-45](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/DatabaseControls/Resources/Help/SelectByAttribute/SelectByAttribute.html#L22-L45): `src="SelectByAttributeHelp.png"`, `src="AvailableFields.png"`, `src="Functions.png"`, `src="SelectByAttributeCalculationsArea.png"`.
- The folder contains only `SelectByAttribute.html`, `SelectByAttributeCalculationsArea.png` and `SelectByAttributeHelp.png` ([folder](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93/src/DatabaseControls/Resources/Help/SelectByAttribute)). `AvailableFields.png` and `Functions.png` are in [`Resources/Help/FieldCalculator`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93/src/DatabaseControls/Resources/Help/FieldCalculator).
- The extraction filters on the page's prefix: [`CalculatorHelpWindow.xaml.cs` lines 66-84](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/DatabaseControls/FieldCalculator/CalculatorHelpWindow.xaml.cs#L66-L84)
  ```csharp
  foreach (string name in assembly.GetManifestResourceNames())
  {
      if (!name.StartsWith(_helpResourcePrefix))
          continue;
  ```

## Suggested fix

Copy the two images into `Resources/Help/SelectByAttribute/` (they're already embedded by the folder's wildcard in `DatabaseControls.csproj`). Alternatively, have the window extract both help folders. No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) embeds the help pages from upstream unchanged, so the same two images are missing. Listed among the upstream quirks kept in [`docs/migration-plan.md`, Phase 5](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L580).
