# test/C/hecdss_test.c doesn't compile with GCC 14+ or Clang 16+ (unlink without <unistd.h>)

**Target:** [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), `test/C/hecdss_test.c`. Found in 7-JA-7 ([`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7)); unchanged on `main` ([`f14554d`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c), 2026-08-07).

**Type:** build · **Severity:** low

**Verification:** verified by reading the source. XGoodFit's build hit the error with GCC 15.2 on Linux, and its workaround (commit 785fef5, below) has since built in its CI on Linux and macOS. It wasn't re-run for this write-up.

## Summary

The test calls `unlink(filename)` but includes only `<stdio.h>`, `<string.h>`, `<stdlib.h>` and `hecdss.h`. `unlink` is declared in `<unistd.h>` on POSIX systems. GCC 14 and Clang 16 made implicit function declarations an error, so the file no longer compiles there. MSVC declares `unlink` in `<stdio.h>`/`<io.h>`, which is why the only build script for it (`test/test_hecdss_dll.bat`, using `cl`) works.

## Details and how to reproduce

```sh
gcc -I heclib/hecdss test/C/hecdss_test.c -L<build> -lhecdss
# error: implicit declaration of function 'unlink' [-Wimplicit-function-declaration]
```

## Why this is an issue and not intended behaviour

The test exercises the portable C API (`hec_dss_textStore` and friends) and has nothing Windows-specific apart from this missing include. The file isn't in `test/C/CMakeLists.txt`, so HEC's CMake CI never compiles it and can't catch this.

## Where in the source

[`test/C/hecdss_test.c` lines 1-18](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/test/C/hecdss_test.c#L1-L18)

```c
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "hecdss.h"
...
  unlink(filename);
```

## Suggested fix

```c
#ifdef _WIN32
#include <io.h>
#else
#include <unistd.h>
#endif
```

Or use `remove(filename)` from `<stdio.h>`, which is standard C. Consider adding the test to `test/C/CMakeLists.txt`, so CI builds it on every platform.

## How XGoodFit handles it

XGoodFit builds this test in its own CMake file and force-includes `<unistd.h>` off Windows: [`native/hecdss/CMakeLists.txt` lines 73-80](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/native/hecdss/CMakeLists.txt#L73-L80) (commit [785fef5](https://github.com/jonklaric/XGoodFit/commit/785fef5b80e95a83ad43122eaa175988f3318293)).
