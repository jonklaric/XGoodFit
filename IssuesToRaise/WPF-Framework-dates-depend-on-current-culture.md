# Project and element dates are written and read in the machine's culture, so they are lost or have day and month swapped on non-US machines

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkInterfaces/Utilities/Tools.cs` (`Tools.DateFromString`, `Tools.DateToUniversalString`). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30. **Also affects** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) 2.0.1 ([`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269)), which stores every project's and element's `CreationDate` and `LastModified` through these two methods. The fix belongs in WPF-Framework; RMC-BestFit then needs the updated package, or the same change in its own callers.

**Type:** data loss · **Severity:** high

**Verification:** reproduced. A console program compiled upstream's `Tools.cs` unchanged and round-tripped dates stored in RMC-BestFit's examples under en-US, en-AU (Windows' own en-AU settings) and de-DE. XGoodFit's file-compatibility reference tool, which runs RMC-BestFit 2.0.1's own `RMC.BestFit.UI` against the 1.0.5 packages, gives the same result when run with `--culture en-AU`.

## Summary

`Tools.DateToUniversalString` writes a `DateTime` with `ToString()`, and `Tools.DateFromString` reads it back with `DateTime.TryParse`, both in the current culture. A `.bestfit` file therefore stores dates in whatever culture the saving machine uses, and only a machine with a compatible culture reads them correctly. On a day-first machine (en-AU, en-GB, de-DE, fr-FR, …) the US dates in RMC-BestFit's own examples either fail to parse and are saved back as year 1, or are read with day and month swapped.

## Details and how to reproduce

```csharp
// Compiled with upstream src/FrameworkInterfaces/Utilities/Tools.cs. Mirrors what RMC-BestFit's
// Open and Save do: X = Tools.DateFromString(cell) ?? DateTime.MinValue; cell = Tools.DateToUniversalString(X).
string[] stored = { "2/19/2026 8:55:25 PM", "3/1/2026 2:00:00 PM" };   // from usgs-download-example.bestfit
foreach (var s in stored)
{
    DateTime? read = Tools.DateFromString(s);
    string saved = Tools.DateToUniversalString(read ?? DateTime.MinValue);
}
```

| Culture | Stored text | Read as | Saved back as |
|---|---|---|---|
| en-US | `2/19/2026 8:55:25 PM` | 19 Feb 2026 | `2/19/2026 8:55:25 PM` |
| en-AU (Windows settings) | `2/19/2026 8:55:25 PM` | **null** (parse fails) | **`1/01/0001 12:00:00 AM`** |
| en-AU (Windows settings) | `3/1/2026 2:00:00 PM` | **3 January** 2026 | `3/01/2026 2:00:00 PM` |
| de-DE | `2/19/2026 8:55:25 PM` | **null** | **`01.01.0001 00:00:00`** |
| de-DE | `3/1/2026 2:00:00 PM` | **3 January** 2026 | `03.01.2026 14:00:00` |

Moving files between machines fails the same way. A date saved on a de-DE machine as `26.03.2026 14:00:00` reads as `null` on an en-US machine, and `01.03.2026 14:00:00` reads as 3 January.

How much of RMC-BestFit's shipped data is affected: of the 730 `CreationDate`/`LastModified` values in the 32 example projects (`examples/`, read-only with SQLite), **487 have a day above 12**, so they can't be parsed day-first and are lost on the first save. **230 have a day of 12 or less** different from the month, so they are read with day and month swapped. Only 13 survive.

In RMC-BestFit the dates appear in each element's and the project's properties ("Created On", "Last Modified"). After a save on an en-AU machine, reopening shows `1/1/0001 12:00:00 AM` for most of them.

## Why this is an issue and not intended behaviour

- The method is named `DateToUniversalString` and converts to UTC first, so it's meant to be a portable storage format. Writing it in the current culture defeats that.
- The same files store other values culture-independently. In RMC-BestFit's `TimeSeriesElement`, `StartDateTime` is written with `ToString("o", CultureInfo.InvariantCulture)` and read with `TryParseExact(…, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, …)` ([line 1268](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L1268) and [line 1461](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L1461)), and numbers are written with `"G17"` and `CultureInfo.InvariantCulture`.
- WPF-Framework's own [`tests/CULTURE_SMOKE_TESTS.md`](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/CULTURE_SMOKE_TESTS.md) asks that saved files contain no culture-dependent separators "regardless of Windows region" (Test 1) and that a saved settings file "loads identically on a colleague's en-US machine" (Test 2). These two methods are the exception.
- The loss is silent: a failed parse becomes `DateTime.MinValue`, and the next save overwrites the original text.
- The stored text even depends on the platform within one culture. .NET on Windows writes en-US times with a plain space before AM/PM (checked, ICU mode, .NET 10). .NET on macOS and Linux (system ICU) writes U+202F NARROW NO-BREAK SPACE there, as XGoodFit's port saw on those systems. That only matters if these libraries run outside Windows, but it's another reason to store an invariant form.

## Where in the source

- [`src/FrameworkInterfaces/Utilities/Tools.cs` lines 23-44](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Utilities/Tools.cs#L23-L44)
  ```csharp
  public static DateTime? DateFromString(string dateString)
  {
      if (DateTime.TryParse(dateString, out DateTime result))   // current culture
      ...
  }

  public static string DateToUniversalString(DateTime dateTime)
  {
      return dateTime.ToUniversalTime().ToString();               // current culture
  }
  ```
- Callers in RMC-BestFit 2.0.1, all of the form `X = Tools.DateFromString(cell) ?? DateTime.MinValue` and `EditCell(…, Tools.DateToUniversalString(X))`:
  - the project: [`src/RMC.BestFit.UI/BestFitProject.cs` lines 701-702](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/BestFitProject.cs#L701-L702) and [848-849](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/BestFitProject.cs#L848-L849);
  - time series: [`Elements/TimeSeriesData/TimeSeriesElement.cs` lines 1211-1212](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L1211-L1212) and [1443-1444](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L1443-L1444);
  - every other element type in `Elements/` (input data, fitting, univariate, B17C, mixture, composite, point process, bivariate, coincident frequency, rating curve, time-series analysis), for example [`Elements/UnivariateAnalysis/B17CAnalysis.cs` lines 875-876](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/B17CAnalysis.cs#L875-L876) and [1086-1087](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/B17CAnalysis.cs#L1086-L1087).
- WPF-Framework's own demo uses the same pattern: [`src/FrameworkUI.Demo/Model/DemoProject.cs` lines 506-507 and 634-635](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI.Demo/Model/DemoProject.cs#L506-L507).

## Suggested fix

```csharp
public static string DateToUniversalString(DateTime dateTime)
{
    // ISO 8601 round-trip form, e.g. 2026-03-26T14:00:00.0000000Z: the same in every culture.
    return dateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
}

public static DateTime? DateFromString(string dateString)
{
    if (string.IsNullOrWhiteSpace(dateString)) return null;
    // New files: the round-trip form.
    if (DateTime.TryParseExact(dateString, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime iso))
        return iso.ToLocalTime();
    // Files written so far: try the US form the released examples use, then the machine's culture.
    if (DateTime.TryParse(dateString, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime us))
        return us.ToLocalTime();
    if (DateTime.TryParse(dateString, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime local))
        return local.ToLocalTime();
    return null;
}
```

Notes and compatibility:

- **Older readers:** files written in the ISO form open correctly in today's RMC-BestFit 2.0.1 on every culture. Its unchanged `DateFromString` parses `2026-03-26T14:00:00.0000000Z` correctly under en-US, en-AU, de-DE, fr-FR and ja-JP (checked).
- **Existing files are ambiguous.** A file saved on a day-first machine by 2.0.x holds day-first text, so `3/1/2026` can't be resolved for certain. Trying en-US first matches every shipped example. Files from non-US users will mostly be day-first, so an alternative is to try the current culture first and accept that the examples' ambiguous dates read swapped on those machines.
- **Don't save what failed to load.** When parsing fails, keep the original text (or leave the cell untouched) instead of writing `DateTime.MinValue`. This stops the irreversible part of the loss even before the format changes.
- The same change is needed wherever RMC-BestFit or other WPF-Framework consumers write dates with `ToString()`.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) compiles `FrameworkInterfaces` from WPF-Framework unchanged, so it behaves the same. Its file-compatibility suite pins en-US for that reason and documents the finding: [`tests/BestFit/RMC.BestFit.FileCompat.Tests/Golden/README.md`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/tests/BestFit/RMC.BestFit.FileCompat.Tests/Golden/README.md) ("Dates follow the current culture"). It's recorded as a decision still to make in [`docs/migration-plan.md`, Phase 6](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L596). XGoodFit would rather take an upstream fix than diverge in the file format.
