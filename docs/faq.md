---
layout: default
title: FAQ
nav_order: 6
---

# F.A.Q.

Below are a list of commonly asked questions. For even more help head on over to the [discussion forum](https://github.com/philosowaffle/peloton-to-garmin/discussions).

1. TOC
{:toc}

## VO2 Max is not displaying

Garmin will only generate a VO2 max for your workouts if all of the following criteria are met:

1. Your personal Garmin device already supports VO2 Max Calculations
1. You have not configured a [custom device info file]({{ site.baseurl }}{% link configuration/providing-device-info.md %}) (i.e. you are using the defaults)
1. You have met all of [Garmin's VO2 requirements](https://support.garmin.com/en-SG/?faq=MyIZ05OMpu6wSl95UVUjp7) for your workout type

## Garmin Upload Not Working

Sometimes, auth failures with Garmin are only temporary. For this reason, a failure to upload will not kill your sync job. Instead, P2G will stage the files and try to upload them again on the next sync interval. You can also always manually upload your files, you do not need to worry about duplicates.

If the problem persists head on over to the [discussion forum](https://github.com/philosowaffle/peloton-to-garmin/discussions) to see if others are experiencing the same issue and to track any open issues.

## My config file is not working

1. Windows Executable - Ensure you config is saved in a file named `configuration.local.json` that is in the same directory as the executable.
1. Docker - Ensure you are correctly passing in or mounting a config file named `configuration.local.json`
1. Verify your config file is in valid json format. You can copy paste the contents into this [online tool](https://jsonlint.com/?code=) to verify.

If the problem persists head on over to the [discussion forum](https://github.com/philosowaffle/peloton-to-garmin/discussions) to get help.

## My Zones are missing the range values in Garmin Connect

See [Understanding custom zones]({{ site.baseurl }}{% link configuration/json.md %}#understanding-custom-zones).
