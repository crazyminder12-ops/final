#!/usr/bin/env sh
set -eu
gcc -shared -fPIC -O2 -o libdeliverycalc.so deliverycalc.c
