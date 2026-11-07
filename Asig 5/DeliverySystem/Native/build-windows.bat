@echo off
where gcc >nul 2>nul || (echo GCC not found. Install MinGW-w64 and add gcc to PATH.& exit /b 1)
gcc -shared -O2 -o deliverycalc.dll deliverycalc.c
