---
layout: default
title: Windows
parent: Install
nav_order: 3
---

# Windows

For convenience a compiled windows executable is provided. This can easily be downloaded and run on your machine.

1. Download and extract the [latest stable release](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. In the unzipped directory find the `configuration.local.json` and open it in a text editor of your choice
    1. Be sure to set your usernames and passwords in Garmin and Peloton config sections respectively.
    1. Save and close the file
1. Find the `PelotonToGarminConsole.exe`
1. Double click to run it!

You can learn more about customizing your configuration file over in the [Configuration Section]({{ site.baseurl }}{% link configuration/index.md %}).

## Available Versions

P2G provides two different versions of the executable you can choose between:

1. [The latest stable version](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. [The latest dev version](https://github.com/philosowaffle/peloton-to-garmin/actions/workflows/publish_distros_latest.yml)
    1. Click on the first item in the list with a green checkmark, this will be the latest successful build.
    1. On the summary page, at the bottom you will see a section called `Artifacts` with various builds attached. Click on one of these builds to download for your operating system.

## Updating

1. Download and extract the [latest stable release](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. In the unzipped directory replace the `configuration.local.json` with your previous `configuration.local.json` file
1. Find the `PelotonToGarminConsole.exe`
1. Double click to run it!