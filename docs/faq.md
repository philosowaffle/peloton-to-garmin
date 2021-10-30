---
layout: default
title: FAQ
nav_order: 5
---

# F.A.Q.

Below are a list of commonly asked questions. For even more help head on over to the [discussion forum](https://github.com/philosowaffle/peloton-to-garmin/discussions).

1. TOC
{:toc}

## Garmin Upload Not Working

Sometimes, auth failures with Garmin are only temporary. For this reason, a failure to upload will not kill your sync job. Instead, P2G will stage the files and try to upload them again on the next sync interval. You can also always manually upload your files, you do not need to worry about duplicates.

If the problem persists head on over to the [discussion forum](https://github.com/philosowaffle/peloton-to-garmin/discussions) to see if others are experiencing the same issue and to track any open issues.

## Docker syncHistory.json missing

1. By default the `syncHistory.json` file is written to `/app/syncHistory.json` in the container.
1. If you wish to have access to this file so that you can modify it, then in your `configuration.local.json` [App section]({{ site.baseurl }}{% link configuration/json.md %}#app-config) set `"SyncHistoryDbPath": "./output/syncHistory.json"`. If the file does not exist in this location, it will be created on first run.

## My config file is not working

1. Windows Executable - Ensure you config is saved in a file named `configuration.local.json` that is in the same directory as the executable.
1. Docker - Ensure you are correctly passing in or mounting a config file named `configuration.local.json`
1. Verify your config file is in valid json format. You can copy paste the contents into this [online tool](https://jsonlint.com/?code=) to verify.

If the problem persists head on over to the [discussion forum](https://github.com/philosowaffle/peloton-to-garmin/discussions) to get help.

## My Zones are missing the range values in Garmin Connect

See [Understanding custom zones]({{ site.baseurl }}{% link configuration/json.md %}#understanding-custom-zones).
