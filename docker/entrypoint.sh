#!/bin/bash
set -e

chown -R p2g:p2g /app
chmod 770 -R /app

if [[ "$1" == "api" ]]; then
    exec runuser -u p2g ./Api
elif [[ "$1" == "webui" ]]; then
    exec runuser -u p2g ./WebUI
else
   exec runuser -u p2g ./ConsoleClient
fi