# The MCMC trace plot without warm-up still shows the last warm-up draw

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.xaml.cs`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface (off by one) · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

With "Include Warm Up" unchecked, the trace plot starts at draw `WarmupIterations - 1`. Draws are numbered from 0, so that is the last warm-up draw, and the plot shows one more draw than the retained sample. Numerics treats draw `WarmupIterations` as the first retained one.

## Details and how to reproduce

With 1,750 warm-up iterations, the plot without warm-up should start at draw 1,750 and show `Iterations - 1750` draws per chain. It starts at draw 1,749 and shows `Iterations - 1749`. The effect on the picture is small, but the plot then doesn't match the retained sample, the one Numerics' diagnostics use.

## Why this is an issue and not intended behaviour

Numerics' own diagnostics drop exactly `warmupIterations` draws: `GelmanRubin` reads `markovChains[chainIndex][warmupIterations + iteration]` ([`Numerics/Sampling/MCMC/Support/MCMCDiagnostics.cs` lines 147-164](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Numerics/Sampling/MCMC/Support/MCMCDiagnostics.cs#L147-L164)). The `- 1` looks like a 1-based count used as a 0-based index.

## Where in the source

[`src/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.xaml.cs` line 579](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.xaml.cs#L579) (the cache check) and [lines 612-613](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.xaml.cs#L612-L613) (building the trace):

```csharp
int startE = IncludeWarmUpCheckBox.IsChecked == true ? 0 : Analysis.WarmupIterations - 1;
int count = Analysis.Iterations - startE;
```

## Suggested fix

Use `Analysis.WarmupIterations` in both places (line 579 has to match line 612, or the trace cache is never reused):

```csharp
int startE = IncludeWarmUpCheckBox.IsChecked == true ? 0 : Analysis.WarmupIterations;
```

No compatibility risk; only the plot changes.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's start index for now ([`src/BestFit/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.axaml.cs` lines 613 and 646](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.axaml.cs#L613)).
