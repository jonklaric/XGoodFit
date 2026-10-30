# On Linux and macOS, squeezing and sorted catalogues write to a hard-coded /tmp

**Target:** [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), `heclib/heclib_c/src/Public/zsqueeze7.c`, `src/Internal/zcatInternalSort.c` and `src/Utilities/sortfiles.c`. Found in 7-JA-7 ([`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7)); these files are unchanged on `main` ([`f14554d`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c), 2026-08-07).

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source. XGoodFit saw the files appear in `/tmp` while running HEC's Dss-C tests on Linux and macOS; the cross-device rename failure was read, not run.

## Summary

On Windows these code paths use the user's temporary folder (`GetTempPath`, `_tempnam`). On every other platform they use a literal `/tmp`:

- **In-place squeeze**, which `hec_dss_squeeze` always does (`zsqueeze7(ifltab, 0, 1)`), builds `/tmp/<file name>_squ.dss`. It first *renames* the DSS file to that path to test that renaming works. If `/tmp` is a separate file system (tmpfs, the default on Fedora, Arch and others), `rename()` fails with `EXDEV`, and the squeeze fails with "Unable to rename file for squeezing".
- **Sorted catalogues** create `/tmp/cat_unsorted-XXXXXX` and `/tmp/cat_sorted-XXXXXX`.
- **The external sort** uses the prefix `"/tmp"`.

`TMPDIR` is ignored, which breaks sandboxed or hardened environments where `/tmp` isn't writable and per-user temporary directories (`TMPDIR` on macOS is under `/var/folders`). The squeeze name is also predictable and shared between users (`/tmp/<name>_squ.dss`, removed with `unlink` before use), so two users or processes squeezing files with the same name at once collide.

## Details and how to reproduce

On Linux with `/tmp` on tmpfs and the DSS file on another file system: `hec_dss_squeeze("/home/user/data/sample7.dss")` returns an error, with "Unable to rename file for squeezing, file: …" printed. With `TMPDIR` set and `/tmp` read-only (a sandbox), sorted catalogues fail.

## Why this is an issue and not intended behaviour

The Windows branches deliberately use the platform's temporary-directory API, so the intent is "the user's temporary folder". The POSIX branches hard-code one path instead of the POSIX equivalent (`TMPDIR`, falling back to `/tmp`).

## Where in the source

- [`src/Public/zsqueeze7.c` lines 180-215](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/heclib_c/src/Public/zsqueeze7.c#L180-L215) (`stringCopy(tempName, sizeof(tempName), "/tmp/", 5);`) and [lines 254-280](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/heclib_c/src/Public/zsqueeze7.c#L254-L280) (the test rename). In-place is the default: [`src/DssInterface/v6and7/zsqueeze.c` line 91](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/heclib_c/src/DssInterface/v6and7/zsqueeze.c#L91).
- [`src/Internal/zcatInternalSort.c` lines 233-262](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/heclib_c/src/Internal/zcatInternalSort.c#L233-L262) (`strcpy( tempUnsorted, "/tmp/cat_unsorted-XXXXXX");`).
- [`src/Utilities/sortfiles.c` lines 320-325 and 525-541](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/heclib_c/src/Utilities/sortfiles.c#L320-L325) (`static char *prefix = "/tmp";`).

## Suggested fix

Add one helper and use it in all three places:

```c
const char* hec_temp_dir(void)
{
    const char* d = getenv("TMPDIR");
    return (d && *d) ? d : "/tmp";
}
```

For the in-place squeeze, create the temporary file in the DSS file's own directory, which also avoids `EXDEV`, or copy instead of renaming. Use `mkstemp` for a unique name instead of `<name>_squ.dss`.

## How XGoodFit handles it

RMC-BestFit's unsorted catalogue never reaches these paths, and XGoodFit (a port of RMC-BestFit that builds hec-dss for Linux and macOS) doesn't squeeze. When it runs HEC's C test suite on Linux, it binds a private folder over `/tmp` (bubblewrap). Recorded in [`docs/migration-plan.md`, Phase 0](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L467).
