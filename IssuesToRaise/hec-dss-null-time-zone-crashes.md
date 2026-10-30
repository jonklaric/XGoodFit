# Passing NULL as timeZoneName to the store functions crashes (strlen on NULL)

**Target:** [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), `heclib/hecdss/hecdss.c` (`hec_dss_tsStoreRegular`, `hec_dss_tsStoreIregular`, `hec_dss_pdStore`, `hec_dss_locationStore`) and `heclib/heclib_c/src/Utilities/mallocAndCopy.c`. Found in 7-JA-7 ([`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7)); still present on `main` ([`f14554d`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c), 2026-08-07).

**Type:** bug (crash) · **Severity:** medium

**Verification:** reproduced on Windows x64 with 7-JA-7 built from source. `hec_dss_tsStoreRegular(dss, "/A/B/FLOW/01JAN2020/1DAY/TEST/", "01JAN2020", "2400", values, 5, NULL, 0, 0, "CFS", "INST-VAL", "", 0)` returns 0. The same call with `timeZoneName = NULL` kills the process with an access violation (0xC0000005).

## Summary

The time-zone parameter added in #271 is copied with `mallocAndCopy(timeZoneName)`, and `mallocAndCopy` starts with `strlen(copyFrom)`. Passing NULL, the usual way to say "no value" in a C API and what callers updating from the pre-#271 signature naturally pass, dereferences NULL inside the library.

## Details and how to reproduce

```c
double values[] = {1, 2, 3, 4, 5};
int status = hec_dss_tsStoreRegular(dss, "/A/B/FLOW/01JAN2020/1DAY/TEST/", "01JAN2020", "2400",
                                    values, 5, NULL, 0, 0, "CFS", "INST-VAL",
                                    NULL /* timeZoneName */, 0);
// Expected: stored with no time zone (as with "") or an error code. Actual: access violation.
```

## Why this is an issue and not intended behaviour

`hecdss.h` describes `timeZoneName` as "time zone name (for the time-series), can be different from the location time-zone" and doesn't say it must be non-NULL. Other optional strings in the same header explicitly accept NULL ("pathFilter: Either null (for ignore) …", "startDateBase: defaults to 01Jan1990 if empty or null"). Before #271 there was no time zone at all, so "none" is a natural input. A library shouldn't crash the host process on it.

## Where in the source

- 7-JA-7 [`heclib/hecdss/hecdss.c` line 401](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/hecdss/hecdss.c#L401) (`tsStoreRegular`), [441](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/hecdss/hecdss.c#L441) (`tsStoreIregular`), [503](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/hecdss/hecdss.c#L503) (`locationStore`) and [675](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/hecdss/hecdss.c#L675) (`pdStore`): `… ->timeZoneName = mallocAndCopy(timeZoneName);`
- [`heclib/heclib_c/src/Utilities/mallocAndCopy.c` lines 35-41](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/heclib_c/src/Utilities/mallocAndCopy.c#L35-L41): `len = (int)strlen(copyFrom);`, with no NULL check.
- Still on `main`: [`hecdss.c` line 500](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c/heclib/hecdss/hecdss.c#L500).

## Suggested fix

Make `mallocAndCopy(NULL)` return `NULL` (fixes every caller), or treat NULL as "" in the API functions:

```c
char *mallocAndCopy(const char *copyFrom)
{
    if (copyFrom == NULL) return NULL;
    ...
```

Then document in `hecdss.h` that `timeZoneName` may be NULL or empty.

## How XGoodFit handles it

XGoodFit's ported `DssNative.cs` (for its RMC-BestFit port) always passes `""` for "no time zone", with a comment explaining why: [`src/Hec.Dss/DssNative.cs` lines 92-93 and 370](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Hec.Dss/DssNative.cs#L92-L93).
