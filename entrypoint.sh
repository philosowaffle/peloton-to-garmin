#!/bin/bash
set -e

if [[ "$1" == "web" ]]; then
    ./WebApp
else
   ./PelotonToGarminConsole
fi