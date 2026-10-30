# Resources/BestFitStyles.xaml is never merged and none of its keys are used

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/Resources/BestFitStyles.xaml`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-30.

**Type:** bug (cleanup) · **Severity:** low

**Verification:** verified by searching the source. No XAML, C# or project file mentions `BestFitStyles` apart from the file itself, and none of its six keys appears anywhere else.

## Summary

`BestFitStyles.xaml` (with `x:Class="RMC_BestFit.BestFitStyles"`) defines six keyed resources, but no `ResourceDictionary.MergedDictionaries` entry, pack URI or code creates it, and none of the keys is referenced. It's compiled into the application but has no effect.

## Details and how to reproduce

Search the repository for `BestFitStyles` and for each of the file's `x:Key` values: the only hits are in the file itself.

## Why this is an issue and not intended behaviour

It looks like a leftover from an earlier version of the app's resources. Either the styles were meant to be merged in `App.xaml` and were forgotten, or they're obsolete. Keeping unused XAML makes it harder to see which styles the app really uses.

## Where in the source

[`src/RMC.BestFit.App/Resources/BestFitStyles.xaml`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/Resources/BestFitStyles.xaml), and `App.xaml`, which doesn't merge it.

## Suggested fix

Delete the file, or merge it in `App.xaml` if its styles were meant to apply (and check that nothing changes unexpectedly). No file format is involved.

## How XGoodFit handles it

Reported by XGoodFit's port of the application (a port of RMC-BestFit to Avalonia), which doesn't need to port an unused dictionary.
