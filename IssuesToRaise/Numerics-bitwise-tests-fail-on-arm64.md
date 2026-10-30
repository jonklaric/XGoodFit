# Three bit-for-bit tests fail on arm64: their reference values were recorded on x64

**Target:** [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics), `Test_Numerics` (`Sampling/MCMC/Test_NUTS_GradientReuse.cs`, `Distributions/Univariate/Test_LegacyParameterConstraints.cs`). Found in 2.2.0 at commit [`7e8e8d1`](https://github.com/USACE-RMC/Numerics/tree/v2.2.0) (tag `v2.2.0`), still `main` on 2026-10-30.

**Type:** test reliability · **Severity:** low

**Verification:** verified by reading the source. The failures were seen in XGoodFit's CI on GitHub's hosted macOS arm64 runner (.NET 10); not reproduced on another arm64 machine.

## Summary

`Test_NUTS_GradientReuse_AdaptedMetric_ReproducesReferenceDrawsBitwise`, `Test_NUTS_GradientReuse_FixedMetric_ReproducesReferenceDrawsBitwise` and `Weibull_ordinary_PreservesBaselineConstraints` compare against values recorded on x64 with exact equality. On arm64 (Apple silicon, Windows on Arm, Linux arm64) the results differ in the last digits, as floating-point library results do across architectures, and the tests fail. The library itself is fine: tolerance-based tests pass on arm64, including RMC-BestFit's 3,546 model tests.

## Details and how to reproduce

`dotnet test Test_Numerics` on an Apple silicon Mac:

- NUTS (both tests): a draw differs at the 13th significant digit, `0.06320733141719788` (reference) against `0.06320733141716985` (arm64).
- Weibull: the initial shape parameter differs at the 16th, `6.684491850934669` against `6.684491850934662`.

## Why this is an issue and not intended behaviour

The authors already treat these values as platform-dependent: the Weibull test keeps a separate baseline for .NET Framework ("Captured independently … retain exact equality", lines 727-731). Arm64 is a supported .NET platform, and on it the tests can never pass, which hides real regressions for developers on Apple silicon.

## Where in the source

- [`Test_NUTS_GradientReuse.cs` lines 130-155 and 175-187](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Test_Numerics/Sampling/MCMC/Test_NUTS_GradientReuse.cs#L130-L187)
  ```csharp
  long actual = BitConverter.DoubleToInt64Bits(values[j]);
  Assert.AreEqual(expected[k], actual, $"Chain {c} draw {i} coordinate {j} moved: ...");
  ```
- [`Test_LegacyParameterConstraints.cs` lines 724-735](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Test_Numerics/Distributions/Univariate/Test_LegacyParameterConstraints.cs#L724-L735), using exact `Assert.AreEqual` in `AssertArray` ([lines 892-897](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Test_Numerics/Distributions/Univariate/Test_LegacyParameterConstraints.cs#L892-L897)).

## Suggested fix

Keep the strict check where the reference was recorded and add a tolerance elsewhere. For example, assert bitwise equality when `RuntimeInformation.ProcessArchitecture == Architecture.X64`, and otherwise compare with a relative tolerance of about 1e-10. Alternatively record per-architecture references, as the `NETFRAMEWORK` branch already does. The gradient-count test that depends on the trajectory may need the same treatment.

## How XGoodFit handles it

XGoodFit (which builds RMC.Numerics from this source for its RMC-BestFit port) skips these three tests on arm64 only: [`scripts/run-upstream-tests.sh` lines 40-50](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/scripts/run-upstream-tests.sh#L40-L50), recorded in [`docs/migration-plan.md`, Phase 0](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L433-L436).
