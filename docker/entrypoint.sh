#!/bin/bash
set -e

chown -R p2g:p2g /app

if [[ "$1" == "web" ]]; then
    # exec runuser -u p2g ./WebUI
elif [[ "$1" == "api" ]]; then
    exec runuser -u p2g ./Api
else
   exec runuser -u p2g ./PelotonToGarminConsole
fi