# hec_dss_tsRetrieve compares its date arguments with "\0" by pointer, so empty strings don't mean "all data"

**Target:** [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), `heclib/hecdss/hecdss.c` (`hec_dss_tsRetrieve`). Found in 7-JA-7 ([`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7)); still present on `main` ([`f14554d`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c), 2026-08-07).

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source. This came up while verifying another report; it isn't from XGoodFit's port.

## Summary

The "no time window given, retrieve all data" check tests each date argument with `startDate == NULL || startDate == "\0"`. The second test compares the caller's pointer with the address of a string literal inside the library, which is never equal for a caller's string. So only NULL selects "all data"; passing empty strings, the obvious "no value" for a `const char*` argument, goes down the time-window path with empty dates.

## Details and how to reproduce

```c
// Intended: retrieve the whole record set
hec_dss_tsRetrieve(dss, path, "", "", "", "", times, values, n, &read, ...);   // "" != "\0" as pointers
hec_dss_tsRetrieve(dss, path, NULL, NULL, NULL, NULL, ...);                    // only this takes the "all" branch
```

GCC and Clang warn about it: "comparison with string literal results in unspecified behavior" (`-Waddress`).

## Why this is an issue and not intended behaviour

The comment says "if no dates/times are given retrieve all data", and the `== "\0"` half shows the empty string was meant to count as "not given". Comparing a `char*` with a literal compares addresses, not content.

## Where in the source

- 7-JA-7 [`heclib/hecdss/hecdss.c` lines 321-337](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/hecdss/hecdss.c#L321-L337)
  ```c
  if (
       (startDate == NULL || startDate == "\0")
    && (startTime == NULL || startTime == "\0")
    && (endDate == NULL   || endDate == "\0")
    && (endTime == NULL   || endTime == "\0")
    && ! tss->boolPattern )
  ```
- `main` [lines 373-376](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c/heclib/hecdss/hecdss.c#L373-L376).

## Suggested fix

```c
static int isEmpty(const char* s) { return s == NULL || s[0] == '\0'; }
...
if (isEmpty(startDate) && isEmpty(startTime) && isEmpty(endDate) && isEmpty(endTime) && !tss->boolPattern)
```

## How XGoodFit handles it

Not affected: the .NET wrapper XGoodFit uses for its RMC-BestFit port always passes explicit dates (from `hec_dss_tsGetDateTimeRange`).
