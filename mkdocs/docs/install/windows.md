
# Windows

P2G provides a Windows application available for download. When you download the application, everything P2G needs to run is self-contained in the folder you downloaded.  This includes all of your settings and other configuration files.  

For this reason,

1. You can always download and run a newer version of P2G without any risk of breaking your existing version.
1. P2G does not yet support multiple users, but you can have two instances of P2G on the same computer, each in a different folder, setup for a different user
1. To uninstall P2G simply delete its folder
1. You may wish to create a Desktop shortcut to the application for convenience

![P2G UI Demo](../img/p2g_demo.gif "P2G UI Demo")

## ⬇️ Install

1. Download and unzip the [latest stable release](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. Find the `ClientUI.exe`
1. Double click to run it!

You can learn more about customizing your configuration over in the [Configuration Section](../configuration/index.md).

!!! tip Create a Shortcut
    Right click on the `ClientUI.exe` and choose `Create Shortcut`.  Place this created shortcut somewhere easy to find, such as on your Desktop.  If you have multiple people using P2G, consider renaming the shortcut to `<Your Name>-P2G`.

## ⬆️ Updating

1. Download and unzip the [latest stable release](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. In the unzipped directory replace the `configuration.local.json` with your previous `configuration.local.json` file
1. In the unzipped directory copy over the `data` folder from your previous P2G install folder, this will preserve your settings
1. Find the `ClientUI.exe`
1. Double click to run it!

!!! tip Update or create new Shortcuts
    If you previously had setup a Shortcut, you'll need to delete and re-create it.

## ❌ Uninstalling

Simply delete your P2G folder. Done.

!!! warning
    On most systems this will move P2G to your Trash folder, which means you can restore it from there if needed.  However, once it is deleted from your Trash, it will no longer be recoverable.  This includes all of your configuration and settings.

## #️⃣ Changing Versions

1. Find the release you want from the [releases page](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. Download and unzip the Windows exe from that relase (found in the `Assets` section)
1. In the unzipped directory replace the `configuration.local.json` with your previous `configuration.local.json` file
1. In the unzipped directory copy over th `data` folder from your previous P2G install folder, this will preserve your settings
1. Find the `ClientUI.exe`
1. Double click to run it!

!!! warning

    Attempting to use configuration or data from a later version of P2G with an older version is not guaranteed to work. You may need to reconfigure your instance.

### Available Versions

P2G provides two different versions of the executable you can choose between:

1. [The latest stable version](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. [The latest dev version](https://github.com/philosowaffle/peloton-to-garmin/actions/workflows/publish-latest.yaml)
    1. Click on the first item in the list with a green checkmark, this will be the latest successful build.
    1. On the summary page, at the bottom you will see a section called `Artifacts` with various builds attached. Click on one of these builds to download for your operating system.

## 👪 Multiple Users

To setup P2G for an additional user, simply download P2G again into a new Folder named `<PersonsName>-P2G`.  When `PersonsName` would like to launch P2G, they should do so by clicking on the `ClientUI.exe` from this folder.  Addtionally, for convenience you can create a unique shortcut for `PersonsName` such as `PersonsName-P2G.exe`.

!!! tip Create a Shortcut
    Right click on the `ClientUI.exe` and choose `Create Shortcut`.  Place this created shortcut somewhere easy to find, such as on your Desktop.  If you have multiple people using P2G, consider renaming the shortcut to `<Your Name>-P2G`.

## 🚧 Limitations

Due to technical limitations, automatic/background syncing is not currently supported when using the Windows application.  Instead you must manually launch the application and select the workouts you would like to sync each time.
