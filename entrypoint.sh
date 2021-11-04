#!/bin/bash
set -e

if [[ "$1" == "web" ]]; then
    ./app-web/WebApp
else
   ./app/PelotonToGarminConsole
fi