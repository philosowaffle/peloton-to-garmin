---
layout: default
title: Windows
parent: Install
nav_order: 3
---

# Windows

For convenience a compiled windows executable is provided. This can easily be downloaded and run on your machine.

1. Download and unzip the [latest stable release](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. In the unzipped directory find the `configuration.local.json` and open it in a text editor of your choice
    1. Be sure to set your usernames and passwords in Garmin and Peloton config sections respectively.
    1. Save and close the file
1. Find the `PelotonToGarminConsole.exe`
1. Double click to run it!

You can learn more about customizing your configuration file over in the [Configuration Section]({{ site.baseurl }}{% link configuration/index.md %}).

## Updating

1. Download and unzip the [latest stable release](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. In the unzipped directory replace the `configuration.local.json` with your previous `configuration.local.json` file
1. Find the `PelotonToGarminConsole.exe`
1. Double click to run it!

## Rolling back to a previous version

1. Find the release you want from the [releases page](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. Download and unzip the Windows exe from that relase (found in the `Assets` section)
1. In the unzipped directory replace the `configuration.local.json` with your previous `configuration.local.json` file
1. Find the `PelotonToGarminConsole.exe`
1. Double click to run it!

## Available Versions

P2G provides two different versions of the executable you can choose between:

1. [The latest stable version](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. [The latest dev version](https://github.com/philosowaffle/peloton-to-garmin/actions/workflows/publish_distros_latest.yml)
    1. Click on the first item in the list with a green checkmark, this will be the latest successful build.
    1. On the summary page, at the bottom you will see a section called `Artifacts` with various builds attached. Click on one of these builds to download for your operating system.

## Limitations

1. Does not truly run in the background, the program must be minimized to the the task bar if using it to automatically sync, and you must manually restart it if your computer reboots
1. No GUI - yet ;)