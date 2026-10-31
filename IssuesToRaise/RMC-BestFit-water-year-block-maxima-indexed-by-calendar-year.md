# Water-year block maxima are indexed by the event's calendar year, so years repeat and go missing

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit/Models/DataFrame/DataFrame.cs` (`CreateBlockSeries`) and `src/RMC.BestFit/Models/DataFrame/DataTypes/ExactData.cs`, with the example `examples/2-input-data/1-block-maximum`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** wrong result · **Severity:** medium

**Verification:** reproduced. A throwaway MSTest test against upstream's `RMC.BestFit` and Numerics 2.2.0, compiled unchanged, read the example's daily series (decompressed from the shipped project file) and called `DataFrame.CreateBlockSeries(series, TimeBlockWindow.WaterYear)`. The result matches the water-year element saved in the shipped project, which was read separately.

## Summary

For a water-year (or custom-year) block, Numerics' `CustomYearSeries` returns each block's maximum with the date of the day it occurred. `CreateBlockSeries` turns each one into `new ExactData(date, value)`, and that constructor sets the index to `date.Year`. A maximum that falls in October, November or December belongs to the water year that ends the next September, but it's indexed by the calendar year it occurred in. That year usually has its own maximum already, so the index appears twice, and the water year the maximum belongs to is missing.

## Details and how to reproduce

The example "Annual maxima: choose the block and the measured quantity" (`examples/2-input-data/1-block-maximum/usgs-block-max-example.bestfit`, Moose River at Victory, VT, USGS 01134500) has a water-year element, "USGS - 01134500 - Block Max - Water Year". Its walkthrough describes the blocks as "October–September, named by ending year", and says "an October event belongs to the following water year".

The test's output, which matches the saved element:

```
daily ordinates 29050, 1947-01-01 to 2026-07-14
water-year maxima 80, indexes 1947-2026
duplicated: 1959, 1973, 1990, 1996, 2003, 2005, 2021
missing: 1960, 1974, 1991, 1997, 2004, 2006, 2022
  index 1959  event 1959-11-29  value 2100  (water year 1960)
  index 1973  event 1973-12-22  value 2250  (water year 1974)
  index 1990  event 1990-12-24  value 1820  (water year 1991)
  index 1996  event 1996-12-03  value 1200  (water year 1997)
  index 2003  event 2003-10-28  value 1650  (water year 2004)
  index 2005  event 2005-10-17  value 2400  (water year 2006)
  index 2021  event 2021-11-01  value 921  (water year 2022)
```

The test's core:

```csharp
var ts = new Numerics.Data.TimeSeries(XElement.Load(dailySeriesXml));
var df = new DataFrame();
df.CreateBlockSeries(ts, TimeBlockWindow.WaterYear);
var idx = df.ExactSeries.Select(d => d.Index).ToList();
```

What it affects: the values are right, so a stationary univariate fit, which uses only the values, is unchanged. Anything that uses the year index gets the wrong year for these maxima:

- nonstationary fits, whose trend models are evaluated at each observation's index (`TrendModel.Predict(data.Index)`), and whose time axis then has two points in some years and none in others;
- bivariate analyses, which pair the two marginals' observations by index, so a duplicated year can pair with the wrong event and the missing year pairs with nothing;
- the chronology plot, threshold and perception data entered by year, and alternatives compared by year.

## Why this is an issue and not intended behaviour

- A water year is named by the year it ends, as the example's walkthrough says and as USGS uses it. Indexing a block by the date of its maximum mixes two conventions in one series: blocks whose maximum falls in January-September get their water year, those in October-December the year before.
- An annual block series should have one value per block. Two values for 1959 and none for 1960, from complete data, can't be what the user asked for.
- For calendar-year blocks the two conventions agree, which is probably why it wasn't noticed.

## Where in the source

- The block series: [`src/RMC.BestFit/Models/DataFrame/DataFrame.cs` lines 2603-2640](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/DataFrame/DataFrame.cs#L2603-L2640), at [line 2636](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/DataFrame/DataFrame.cs#L2636):
  ```csharp
  ExactSeries.Add(new ExactData(_timeSeries[i].Index, _timeSeries[i].Value));
  ```
- The constructor: [`src/RMC.BestFit/Models/DataFrame/DataTypes/ExactData.cs` lines 32-39](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/DataFrame/DataTypes/ExactData.cs#L32-L39):
  ```csharp
  public ExactData(DateTime dateTime, double value) : base(dateTime.Year, value, 0)
  {
      _dateTime = dateTime;
      _index = dateTime.Year;
  ```
- Numerics, which returns the event's own date: [`Numerics/Data/Time Series/TimeSeries.cs` lines 1693-1787](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Numerics/Data/Time%20Series/TimeSeries.cs#L1693-L1787): it shifts the dates forward so each water year becomes a calendar year (lines 1718-1719), takes each block's maximum with its date (lines 1740-1752), and shifts the dates back (line 1785). The custom-year overload from line 1797 works the same way.
- The nonstationary likelihood's use of the index, for example [`src/RMC.BestFit/Models/UnivariateDistribution/UnivariateDistribution.cs` line 1482](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/UnivariateDistribution/UnivariateDistribution.cs#L1482); the bivariate pairing, [`BivariateDistribution.cs` lines 523-543](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/BivariateDistribution/BivariateDistribution.cs#L523-L543).
- The walkthrough: [`examples/2-input-data/1-block-maximum/usgs-block-max-example.md` lines 30 and 37](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/2-input-data/1-block-maximum/usgs-block-max-example.md#L30-L37).

## Suggested fix

Index each block maximum by its block, and keep the event's date:

```csharp
// in CreateBlockSeries, for WaterYear and CustomYear blocks starting in month m > 1:
int blockYear = date.Month >= startMonth ? date.Year + 1 : date.Year;   // the year the block ends
var data = new ExactData(blockYear, value) { /* keep DateTime = date */ };
```

`ExactData` would need a constructor (or settable `DateTime`) that takes both the index and the date, so the saved `DateTime` attribute still records the event. The same applies to the `Sum` and `Average` block functions, which date a block by its last day; for a water year that's in September and already gives the right year, but taking the index from the block rather than the date makes all of them consistent.

Existing projects store the indexes, so they keep the old ones until the block series is processed again. A note in the release, or offering to reprocess block series on open, would let users with nonstationary or bivariate water-year analyses check their results. Upstream's `ExactDataProcessTests.Test_WaterYearSeries` checks only the maxima's values ([`src/RMC.BestFit.Tests/DataFrame/ExactDataProcessTests.cs` lines 64-77](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.Tests/DataFrame/ExactDataProcessTests.cs#L64-L77)); checking their indexes too would have caught this.

## How XGoodFit handles it

XGoodFit compiles upstream's model library unchanged ([`src/BestFit/RMC.BestFit.App/RMC.BestFit.App.csproj` line 63](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/RMC.BestFit.App.csproj#L63)), so it has the same behaviour. Its walkthrough coverage notes record the discrepancy: [`docs/walkthrough-coverage/time-series-input-bivariate.md` line 234](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/docs/walkthrough-coverage/time-series-input-bivariate.md#L234).
