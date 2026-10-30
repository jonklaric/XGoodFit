# The Terms and Conditions "I agree" box unlocks before the terms are even loaded, so users can agree without scrolling

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkUI/Help Menu/TermsAndConditionsWindow.xaml.cs` (used by `TermsAndConditionsWindow.CheckTermsAndConditions`). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** bug · **Severity:** medium

**Verification:** reproduced in WPF with the published `RMC.Wpf.Framework.Controls` 1.0.5 package. The window was shown off-screen with `ShowButtons = true`, as `CheckTermsAndConditions` shows it. The first `ScrollChanged` arrived before `Loaded`, with the empty default document (extent 16, viewport 298), and enabled the check box. The terms were then loaded (extent 950), and the box stayed enabled at scroll offset 0.

## Summary

The handler enables "I agree" when the text fits in the viewport. The terms document is assigned in `Loaded`, but the rich text box's first layout, with its default empty document, raises `ScrollChanged` before that. The empty document always fits, so the box unlocks at once, and nothing disables it again when the long terms arrive. The "scroll to the end before agreeing" requirement never applies.

## Details and how to reproduce

Call `TermsAndConditionsWindow.CheckTermsAndConditions()` with no prior agreement (or show the window with `ShowButtons = true`). **Expected:** "I agree" is disabled until the user scrolls to the end of the terms. **Actual:** it's enabled as soon as the window appears.

Observed sequence:

```
ScrollChanged: loaded=False extent=16 viewport=298 offset=0 -> IAgreeCheckbox.IsEnabled=True
Loaded
ScrollChanged: loaded=True extent=950 viewport=298 offset=0 -> IAgreeCheckbox.IsEnabled=True
```

## Why this is an issue and not intended behaviour

The handler's summary is "Enables agreement once the user has scrolled to the end of the terms", and the check box starts with `IsEnabled="False"`. For a terms-of-use acceptance the gate matters. RMC-BestFit 2.0.1 itself opens this window only from Help, with `ShowButtons = false`, so it isn't affected. Other hosts that call `CheckTermsAndConditions` are.

## Where in the source

- The document is set in `Loaded`: [`TermsAndConditionsWindow.xaml.cs` lines 28-51](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Help%20Menu/TermsAndConditionsWindow.xaml.cs#L28-L51).
- The handler: [lines 102-127](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Help%20Menu/TermsAndConditionsWindow.xaml.cs#L102-L127)
  ```csharp
  // Enable checkbox when content fits entirely in the viewport (no scrolling needed)
  if (extentHeight <= viewportHeight)
  {
      if (_showButtons) IAgreeCheckbox.IsEnabled = true;
      return;
  }
  ```
- The static entry point: [lines 258-279](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Help%20Menu/TermsAndConditionsWindow.xaml.cs#L258-L279).

## Suggested fix

Ignore scroll events until the real document is in place:

```csharp
private bool _termsLoaded;
// in Loaded, after setting TCURichTextBox.Document:
_termsLoaded = true;
// in RichTextBox_ScrollChanged:
if (!_termsLoaded) return;
```

Alternatively, set the document in the constructor, after `ShowButtons`/`TermsDocument` are known. Agreement is stored in user settings as before, so no format changes.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) ported this window with the same logic, [`TermsAndConditionsWindow.axaml.cs`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/FrameworkUI/Help%20Menu/TermsAndConditionsWindow.axaml.cs). Like RMC-BestFit, it shows the terms only from Help, without the agreement buttons.
