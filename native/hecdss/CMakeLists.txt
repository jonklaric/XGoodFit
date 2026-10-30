# XGoodFit addition: builds HEC-DSS's native hecdss library from the vendored sources, for one
# runtime identifier per build tree. native/build-hecdss.sh drives it; native/README.md explains it.
#
# This project stands in for HEC's top-level CMakeLists.txt (upstream/hec-dss, tag 7-JA-7) and uses
# HEC's own heclib, hecdss and test CMake files unchanged. It differs from HEC's top level in four
# ways, so that the library depends on nothing beyond the operating system and the build needs no
# network access:
#   - zlib is the vendored copy (upstream/zlib, v1.3.1, the tag HEC's deps/CMakeLists.txt fetches),
#     not a download. HEC's FetchContent step is not used at all.
#   - zlib is linked statically on every OS. HEC's deps/zlib/CMakeLists.txt aliases ZLIB::ZLIBSTATIC
#     to zlib's *shared* library on Linux and macOS, so HEC's own builds there need a separate libz
#     (on macOS through an rpath that HEC doesn't ship). Here ZLIB::ZLIBSTATIC is the static library,
#     as HEC's MSVC branch already uses.
#   - MSVC links its C runtime statically (/MT), so hecdss.dll doesn't need the Visual C++
#     redistributable. HEC's /NODEFAULTLIB:MSVCRTD,LIBCMT executable flags don't apply here; they
#     would remove the static runtime from the test programs.
#   - On Linux, the symbols of the static heclib and zlib aren't exported from libhecdss.so
#     (--exclude-libs), so only hecdss.c's API is exported, as on Windows, and nothing else in the
#     process can interpose zlib or heclib functions.
# The Java library (javaHeclib) isn't built: find_package(JNI) is never called.

cmake_minimum_required(VERSION 3.16)
project(heclib LANGUAGES C)

get_filename_component(_xgf_repo_root "${CMAKE_CURRENT_SOURCE_DIR}/../.." ABSOLUTE)
set(XGF_HECDSS_SOURCE_DIR "${_xgf_repo_root}/upstream/hec-dss" CACHE PATH "HEC-DSS sources (7-JA-7)")
set(XGF_ZLIB_SOURCE_DIR "${_xgf_repo_root}/upstream/zlib" CACHE PATH "zlib sources (v1.3.1)")
option(XGF_HECDSS_TESTS "Build and register HEC's C tests (needs DSS_TEST_DATA_DIR)" OFF)

foreach(_xgf_file "${XGF_HECDSS_SOURCE_DIR}/heclib/hecdss/hecdss.h" "${XGF_ZLIB_SOURCE_DIR}/zlib.h")
  if(NOT EXISTS "${_xgf_file}")
    message(FATAL_ERROR "Missing ${_xgf_file}: is the vendored source in upstream/?")
  endif()
endforeach()

if(MSVC)
  set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()
# zlib is linked into a shared library.
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# zlib, built by its own CMakeLists.txt from a copy in the build tree. When it is built out of source,
# zlib's CMakeLists.txt renames zconf.h in its *source* folder (it generates its own zconf.h), which
# would modify upstream/zlib. The copy leaves out zconf.h, so nothing is renamed anywhere.
set(_xgf_zlib_copy "${CMAKE_CURRENT_BINARY_DIR}/zlib-src")
file(COPY "${XGF_ZLIB_SOURCE_DIR}/" DESTINATION "${_xgf_zlib_copy}" PATTERN "zconf.h" EXCLUDE)
set(ZLIB_BUILD_EXAMPLES OFF CACHE BOOL "zlib's example programs aren't needed" FORCE)
add_subdirectory("${_xgf_zlib_copy}" zlib EXCLUDE_FROM_ALL)
add_library(ZLIB::ZLIBSTATIC ALIAS zlibstatic)

# HEC's heclib (static C library), hecdss (the shared library the .NET wrapper calls) and, with no
# JDK looked up, no javaHeclib.
add_subdirectory("${XGF_HECDSS_SOURCE_DIR}/heclib" heclib)

if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  target_link_options(hecdss PRIVATE "LINKER:--exclude-libs,ALL")
endif()

# Where the library was built, per configuration, for native/build-hecdss.sh.
file(GENERATE OUTPUT "${CMAKE_BINARY_DIR}/hecdss-library-$<CONFIG>.txt" CONTENT "$<TARGET_FILE:hecdss>")

if(XGF_HECDSS_TESTS)
  if(NOT DSS_TEST_DATA_DIR OR NOT EXISTS "${DSS_TEST_DATA_DIR}/sample7.dss")
    message(FATAL_ERROR "XGF_HECDSS_TESTS needs DSS_TEST_DATA_DIR: HEC's dss-test-data "
      "(scripts/fetch-sample-data.sh dss-test-data puts it in sample-data/dss-test-data).")
  endif()
  enable_testing()
  # HEC's C tests (test/C and test/Dss-C), as HEC's top level registers them.
  add_subdirectory("${XGF_HECDSS_SOURCE_DIR}/test" test)

  # HEC's test of the shared library's own API (test/C/hecdss_test.c), which HEC builds only by hand
  # (test/test_hecdss_dll.bat). It checks that the library loads and exports work.
  add_executable(hecdss_test "${XGF_HECDSS_SOURCE_DIR}/test/C/hecdss_test.c")
  target_link_libraries(hecdss_test PRIVATE hecdss)
  target_include_directories(hecdss_test PRIVATE "${XGF_HECDSS_SOURCE_DIR}/heclib/hecdss")
  if(NOT MSVC)
    # It calls unlink without including <unistd.h>, which GCC 14+ and Clang 16+ reject.
    target_compile_options(hecdss_test PRIVATE -include unistd.h)
  endif()
  add_test(NAME hecdss_test COMMAND hecdss_test)
  if(WIN32)
    set_property(TEST hecdss_test PROPERTY ENVIRONMENT_MODIFICATION
      "PATH=path_list_prepend:$<TARGET_FILE_DIR:hecdss>")
  endif()
endif()
