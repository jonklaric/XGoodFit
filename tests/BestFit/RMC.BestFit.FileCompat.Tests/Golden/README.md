# File-compatibility golden data

What upstream's WPF RMC-BestFit 2.0.1 saves when it opens and saves each of RMC-BestFit's 32 example
projects (`sample-data/RMC-BestFit/examples`), described table by table and cell by cell. A `.bestfit`
file is a SQLite database, and the port must save exactly what the original saves
([migration plan](../../../../docs/migration-plan.md), Phase 6). `FileCompatibilityTests` does the same with the
port, on every OS, and compares.

`tools/BestFitFileCompat.Wpf` writes these files with upstream's own code: `upstream/RMC-BestFit/src/
RMC.BestFit.UI`, built as upstream builds it against the `RMC.Wpf.Framework.*` 1.0.5 packages from
nuget.org. The steps, the description and the comparison are the tests' `Shared/*.cs`, compiled into both.

## What each file holds

One file per example, at the example's path plus `.txt`:

- **The steps** (`ProjectRoundTrip`): copy the example into a new folder under its own name, open it as
  File > Open does, save every element and then the project, and close it. Saving each element makes
  RMC-BestFit rewrite all its cells, plots included. The culture is en-US, the one the examples were
  saved in: upstream writes dates in the current culture.
- **`pragma`, `object`, `table`, `column`**: the database settings, the schema, and each table's columns.
- **`row`**: one line per row, in rowid order, with a token per cell (see `FileDescription`): `n` (NULL),
  `i:` (integer), `r:` (a REAL as round-trip text, so equal text means equal bits), `t:` (short text in
  full), `t#bytes:sha` (longer text, such as plot XML, as its UTF-8 length and SHA-256), `p:` (text naming
  the save folder, shown as `{dir}`), `b#bytes:sha>z#bytes:sha` (a blob, and what it inflates to).
  Text with Windows line ends also has `/sha:r`, the hash of its form with "\n" line ends.
- **`reopen`**: what differed in memory after the saved file was opened again, property by property
  (`StateSnapshot`). Upstream itself has three differences, all in one example (below).
- **`state`**: a hash of each element's in-memory state after the example was opened: its public
  properties, plots as their saved XML, models as their XML, MCMC results as their bytes. A second hash
  leaves numbers out.
- A cell or state that changed between two saves of the same file would start with `~` (volatile). There
  are none: upstream saved every example identically twice.

The whole set is about 400 KB of text for 455 MB of examples; the saved databases aren't kept.

## What the tests accept

- **Everything compares exactly** on Windows: every cell, the in-memory state after opening and after
  reopening. The port matches upstream on all 32 examples (2026-10-30).
- **On macOS and Linux**, the tests report and accept differences that disappear in canonical form:
  - line ends: RMC-BestFit stores `XElement.ToString()`, which ends lines with `Environment.NewLine`, so
    XML written there has "\n" where Windows has "\r\n" (3,450 text cells and 60 compressed ones here
    have Windows line ends; those RMC-BestFit writes rather than copies change);
  - the narrow no-break space ICU may put before AM and PM in dates (none expected: .NET normalises it);
  - the path separator in `Project.FullFileName`, which holds the project's own path;
  - in-memory statistics that differ only in their numbers, as each platform's maths library rounds.
- **Compressed bytes** may differ everywhere if the runtime's zlib does, as long as what they hold doesn't.

Each run writes the port's descriptions and a `summary.txt` to `artifacts/filecompat/<rid>/`, which CI
uploads. When a test fails, the content of the port's differing cells is in `cells/<sha>.txt`, and the
port's in-memory state in `<example>.state.txt`.

## Regenerating

On Windows, from the repository root, with the sample data fetched (`scripts/fetch-sample-data.sh
RMC-BestFit`):

```sh
dotnet run -c Release --project tools/BestFitFileCompat.Wpf -- reference sample-data/RMC-BestFit/examples tests/BestFit/RMC.BestFit.FileCompat.Tests/Golden
```

It saves every example twice (about 3 minutes) and marks anything that differs between the two runs as
volatile. Regenerate when upstream's RMC.BestFit.UI or WPF-Framework packages change, when the examples
change, or when `Shared/*.cs` changes what a description holds, and commit the files with that change.

Other commands of the tool help when something differs:

- `reference ... --dump <folder>` also writes the content of every hashed cell to `<folder>/<sha>.txt`,
  so a hash in a description leads to its text; `state <examples> <folder>` writes the in-memory state
  each example opens to, to compare with the port's `.state.txt`.
- `compare <expected> <actual>` compares two folders of descriptions, such as this one and
  `artifacts/filecompat/<rid>` from a CI run.
- `describe <examples> <folder>` describes the examples as they are, without RMC-BestFit: comparing that
  with this folder shows what saving changes.

The tool builds RMC.Numerics from the vendored source, like the rest of XGoodFit. To build it with
upstream's NuGet package instead (the same output, checked on 2026-10-30):

```sh
dotnet build tools/BestFitFileCompat.Wpf -c Release -p:UseLocalRmcNumerics=false -restoreProperty:UseLocalRmcNumerics=false --artifacts-path artifacts/filecompat-nuget
artifacts/filecompat-nuget/bin/BestFitFileCompat.Wpf/release/BestFitFileCompat.Wpf.exe reference sample-data/RMC-BestFit/examples <folder>
```

## What upstream itself does to the examples

Saving rewrites 30 of the 32 examples beyond their own path (`Project.FullFileName`), in upstream as in
the port, because 2.0 betas saved them:
`SoftwareVersion` becomes 2.0.1, plot settings are rewritten in the current serializer's format (newer
attributes such as `Layer` and `ClipTitle`), data frames write numbers with 17 significant digits and
dates in ISO form, booleans stored as text become integers, and time series move into compressed columns.
`tools/BestFitFileCompat.Wpf describe` and `compare` show it cell by cell.

One example loses data. In `bulletin-17c-bayesian-examples`, the Bulletin 17C analysis "B17C Example #3"
opens with an empty distribution (no data, sample size 0, `IsValid` false), because its row, from a
2.0 beta, has no `AnalysisXml` or `Bulletin17CDistribution`. Saving writes both, so the reopened analysis
is valid with 84 observations, but it also clears the row's stored `AnalysisResults` and its 352 KB of
`MCMCResults`, so the analysis has to be estimated again.
