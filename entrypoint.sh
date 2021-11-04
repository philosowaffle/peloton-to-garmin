#!/bin/sh

if [[ "$1" == "web" ]]; then
    dotnet /app-web/WebApp
else
   dotnet /app/PelotonToGarminConsole
fi