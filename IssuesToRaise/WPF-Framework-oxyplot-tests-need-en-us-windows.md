# OxyPlot.Tests pass only on en-US Windows: culture-dependent SVG baselines, a Windows baseline path and an unneeded STA requirement

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `tests/OxyPlot.Tests`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** test reliability · **Severity:** low

**Verification:** verified by reading the source. The three date-axis tests fail on an en-AU Windows machine (seen in XGoodFit's upstream-test runs). The run on macOS and Linux finished without running any tests (XGoodFit CI, Phase 0).

## Summary

- **Culture:** the SVG baselines were recorded under en-US, and OxyPlot formats axis labels in the current culture. On other cultures the three `DateTimeAxis` tests (C01-C03) fail: `1/1/2011` becomes `1/01/2011` on en-AU. The five baselines with decimal labels would fail on comma-decimal cultures; that was inferred, not run.
- **Paths:** `OxyAssert.AreEqual` builds the baseline path as `@"baseline\" + path`, which is only a folder separator on Windows.
- **STA:** `[assembly: Apartment(ApartmentState.STA)]` makes the whole assembly need an STA thread. Nothing in these OxyPlot core tests uses WPF. On macOS and Linux NUnit runs nothing and the run still succeeds.

## Details and how to reproduce

- On Windows with regional format English (Australia): `dotnet test tests/OxyPlot.Tests` fails `C01_DateTimeAxis`, `C02_DateTimeAxis_WithSomeUndefinedPoints` and `C03_DateTimeAxis_WithAllUndefinedPoints` ("Actual svg is not equal to baseline").
- On Linux or macOS: the test run reports success, with zero tests executed.

## Why this is an issue and not intended behaviour

Developers outside the US, and CI on other platforms, get false failures or a silent pass, which hides real regressions. The repository's own `CULTURE_SMOKE_TESTS.md` and `AxisCultureTests` show that cross-culture behaviour matters to the project, and `AxisCultureTests` already sets the culture explicitly.

## Where in the source

- [`tests/OxyPlot.Tests/Properties/AssemblyInfo.cs` line 10](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/OxyPlot.Tests/Properties/AssemblyInfo.cs#L10): `[assembly: Apartment(ApartmentState.STA)]`
- [`tests/OxyPlot.Tests/OxyAssert.cs` lines 22-55](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/OxyPlot.Tests/OxyAssert.cs#L22-L55), with `string baseline = @"baseline\" + path;` at line 33.
- [`tests/OxyPlot.Tests/Axes/AxisTests.cs` lines 293-323](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/OxyPlot.Tests/Axes/AxisTests.cs#L293-L323) (C01), with its baseline [`baseline/C01.svg`](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/OxyPlot.Tests/baseline/C01.svg) containing `1/1/2011`, `1/2/2011`, ….

## Suggested fix

- Pin the culture for the baseline tests: `PlotModel.Culture = CultureInfo.GetCultureInfo("en-US")` in the tests, or NUnit's `[SetCulture("en-US")]` on the fixture.
- Use `Path.Combine("baseline", path)`.
- Drop the assembly-wide STA attribute, or put `[Apartment(ApartmentState.STA)]` only on tests that need it. Consider `--fail-on-no-tests` or a minimum-test-count check in CI.

## How XGoodFit handles it

XGoodFit's test script (XGoodFit is a port of RMC-BestFit to Avalonia) runs this suite only on Windows, and skips the three date tests on non-en-US machines: [`scripts/run-upstream-tests.sh` lines 81-94](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/scripts/run-upstream-tests.sh#L81-L94). The script also fails any suite that runs zero tests.
