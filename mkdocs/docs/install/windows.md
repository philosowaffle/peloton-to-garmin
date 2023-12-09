
# Windows

With P2G v4, there is now a Windows GUI application available for download. P2G does not install anything to your computer, everything it needs to run is self-contained in the folder you downloaded.  This includes all of your settings and other configuration files.  For this reason,

1. You can always download and run a newer version of P2G without any risk of breaking your existing version.
1. P2G does not yet support multiple users, but you can have two instances of P2G on the same computer, each in a different folder, setup for a different user
1. To uninstall P2G simply delete its folder
1. You may wish to create a Desktop shortcut to the application for convenience

![P2G UI Demo](../img/p2g_demo.gif "P2G UI Demo")

## Install

1. Download and unzip the [latest stable release](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. Find the `ClientUI.exe`
1. Double click to run it!

You can learn more about customizing your configuration over in the [Configuration Section](../configuration/index.md).

## Updating

1. Download and unzip the [latest stable release](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. In the unzipped directory replace the `configuration.local.json` with your previous `configuration.local.json` file
1. In the unzipped directory copy over the `data` folder from your previous P2G install folder, this will preserve your settings
1. Find the `ClientUI.exe`
1. Double click to run it!

## Rolling back to a previous version

1. Find the release you want from the [releases page](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. Download and unzip the Windows exe from that relase (found in the `Assets` section)
1. In the unzipped directory replace the `configuration.local.json` with your previous `configuration.local.json` file
1. In the unzipped directory copy over th `data` folder from your previous P2G install folder, this will preserve your settings
1. Find the `ClientUI.exe`
1. Double click to run it!

!!! warning

    Attempting to use configuration or data from a later version of P2G with an older version is not guaranteed to work. You may need to reconfigure your instance.

## Available Versions

P2G provides two different versions of the executable you can choose between:

1. [The latest stable version](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. [The latest dev version](https://github.com/philosowaffle/peloton-to-garmin/actions/workflows/publish-latest.yaml)
    1. Click on the first item in the list with a green checkmark, this will be the latest successful build.
    1. On the summary page, at the bottom you will see a section called `Artifacts` with various builds attached. Click on one of these builds to download for your operating system.

## Limitations

1. Does not truly run in the background, the program must be minimized to the the task bar if using it to automatically sync, and you must manually restart it if your computer reboots
