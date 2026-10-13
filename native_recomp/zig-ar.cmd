@echo off
setlocal
if not defined FUT_STREET2_ZIG (
    echo FUT_STREET2_ZIG must point to zig.exe 1>&2
    exit /b 2
)
"%FUT_STREET2_ZIG%" ar %*
exit /b %ERRORLEVEL%
