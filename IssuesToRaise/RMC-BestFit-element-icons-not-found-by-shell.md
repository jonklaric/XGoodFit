# The shell never sees the theme-aware icon key of Time Series, Input Data and Fitting Analysis elements

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.UI/Elements` (`TimeSeriesElement`, `InputData`, `FittingAnalysis`), with the interface in [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) `src/FrameworkInterfaces/Project/IElement.cs`. Found in RMC-BestFit 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269) and WPF-Framework 1.0.5 at [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), both still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** language rule reproduced; the effect in the application verified by reading. A small C# program with the same type shape (an interface with a default member, a base class that doesn't declare it, one class that lists only the base class and one that also lists a derived interface) printed `null` for the first through the interface and the key for the second. Not checked in the running application.

## Summary

`IElement.ElementImageResourceKey` is a default interface member returning `null`. `ElementBase` doesn't declare it. `TimeSeriesElement`, `InputData` and `FittingAnalysis` each declare a public `ElementImageResourceKey`, but list only `ElementBase` as their base, so C# doesn't treat that property as implementing `IElement`'s member. The shell reads the key through `IElement`, gets `null`, and falls back to the static `ElementImage`. The other nine element types work because they also list `IAnalysisElement` or `IUnivariate`, which re-implements `IElement`.

## Details and how to reproduce

```csharp
interface IElement { string? ElementImageResourceKey => null; }
abstract class ElementBase : IElement { }
interface IAnalysisElement : IElement { }
class InputData : ElementBase { public string ElementImageResourceKey => "InputDataIcon"; }
class UnivariateAnalysis : ElementBase, IAnalysisElement { public string ElementImageResourceKey => "UnivariateAnalysisIcon"; }

IElement a = new InputData(), b = new UnivariateAnalysis();
Console.WriteLine(a.ElementImageResourceKey ?? "null");   // null
Console.WriteLine(b.ElementImageResourceKey ?? "null");   // UnivariateAnalysisIcon
```

In the application, by reading:

- The project explorer gives these three element types a fixed image instead of a resource reference, which the property's documentation says is what makes the icon follow the theme.
- When the theme changes, the main window re-resolves the icons of open document tabs, but skips elements whose key is `null`, so open Time Series, Input Data and Fitting Analysis tabs keep the icon they were opened with.

The element tests don't catch it because they read the property on the concrete type, for example `Assert.AreEqual("InputDataIcon", id.ElementImageResourceKey)`, which calls the class's own property rather than `IElement`'s.

## Why this is an issue and not intended behaviour

Each of the three classes declares the key, documented as used by the project explorer "so the icon updates automatically when the theme changes". Only the interface call is used by the shell, and it doesn't reach the property.

## Where in the source

- The default member: [WPF-Framework `src/FrameworkInterfaces/Project/IElement.cs` lines 50-56](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Project/IElement.cs#L50-L56)
- The three classes, which list only `ElementBase`:
  - [`src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs` line 48](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L48) and [line 219](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L219)
  - [`src/RMC.BestFit.UI/Elements/InputData/InputData.cs` line 43](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/InputData/InputData.cs#L43) and [lines 207-212](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/InputData/InputData.cs#L207-L212)
  - [`src/RMC.BestFit.UI/Elements/DistributionFitting/FittingAnalysis.cs` line 46](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/DistributionFitting/FittingAnalysis.cs#L46) and [line 200](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/DistributionFitting/FittingAnalysis.cs#L200)
- For comparison, `public class UnivariateAnalysis : ElementBase, IUnivariate` ([`UnivariateAnalysis.cs` line 51](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysis.cs#L51)), where `IUnivariate : IAnalysisElement` and `IAnalysisElement : IElement` ([`Elements/Support/IAnalysisElement.cs` line 25](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/Support/IAnalysisElement.cs#L25)).
- Where the shell reads the key, all through `IElement`: [WPF-Framework `src/FrameworkUI/Project Explorer/View Models/ElementNode.cs` lines 53-63](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Project%20Explorer/View%20Models/ElementNode.cs#L53-L63), [`src/FrameworkUI/Main Window/MainWindow.xaml.cs` lines 161-166](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L161-L166) and [lines 577-582](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L577-L582), and `UnsavedElement.cs` and `OpenWindowItem.cs`.
- The tests that read the concrete type: for example [`src/RMC.BestFit.UI.Tests/Elements/InputData/InputDataTests.cs` lines 642-646](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI.Tests/Elements/InputData/InputDataTests.cs#L642-L646).

## Suggested fix

In RMC-BestFit, list `IElement` on the three classes, which makes their property the implementation:

```csharp
public class InputData : ElementBase, IElement
```

And make the tests read the key through the interface (`((IElement)id).ElementImageResourceKey`), so they would catch this.

A more robust fix in WPF-Framework: declare `public virtual string? ElementImageResourceKey => null;` on `ElementBase` and have the elements `override` it. Then the compiler links the members, and a missing `override` gives a warning (CS0114) instead of a silent `null`. That's a source-level change for any other application built on WPF-Framework whose elements declare the property without `override`, though the warning tells them where.

## How XGoodFit handles it

XGoodFit's Avalonia port compiles the same element classes and interface, so it has the same gap for now: [`src/BestFit/RMC.BestFit.UI/Elements/InputData/InputData.cs` line 46](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.UI/Elements/InputData/InputData.cs#L46) and [`src/Framework/FrameworkInterfaces/Project/IElement.cs` line 56](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/Framework/FrameworkInterfaces/Project/IElement.cs#L56).
