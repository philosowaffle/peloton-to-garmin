
# Migrating from V4 to V5

Version 5 introduces a few breaking changes.  Not every change will require action from you.

First, identify your [install method](../install/index.md#-start-here-to-explore-install-options).  Then, use the table below to determine if you need to take any special action.  A ✔️ means there is something for you to do.  Follow the hyperlink to see what steps to take.

| Breaking Change | Build From Source | Docker Headless | Docker WebUI | GitHubAction | Windows Exe |
|:----------------|:------------------|:----------------|:-------------|:-------------|:------------|
| [.NET 9](#net-9) | ✔️ | ❌ | ❌ | ❌ | ❌ |
| [DeviceInfoPath replaced by DeviceInfoSettings](#deviceinfopath-replaced-by-deviceinfosettings) | ✔️ | ✔️ | ❌ | ✔️ | ❌ |
| [Fixed Non-Root Docker](#fixed-non-root-docker) | ❌ | ✔️ | ✔️ | ✔️ | ❌ |

## Breaking Changes

### .NET 9

1. Install the latest [dotnet 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### DeviceInfoPath replaced by DeviceInfoSettings

If you have not configured or made use of the [DeviceInfoPath setting](https://philosowaffle.github.io/peloton-to-garmin/v4.1.0/configuration/json/#custom-device-info) then you do not need to make any changes.

If you are running P2G v4.2.0 or later then it is possible you have already made the needed changes as the new settings format was [introduced with that version](https://github.com/philosowaffle/peloton-to-garmin/issues/606#issuecomment-1925869262).

If you are running P2G in a way that allows it to persist its settings across restarts, then you should not need to do anything specific, P2G will automatically migrate the old `DeviceInfoPath` setting into the new `DeviceInfoSettings` format.  For example if you are using the WebUI or the Windows Exe, then you likely do not need to do anything.

If you use GitHub Actions or if you are not persisting P2G settings across restarts then you will need to manually update your settings.  If you have made use of the `DeviceInfoPath` setting then you will need to update it to follow the new format of `DeviceInfoSettings`.

The new settings structure is explained [here](../configuration/format.md#customizing-the-garmin-device-associated-with-the-workout).

### Fixed Non-Root Docker

Previously running P2G as a rootless container did not [quite work](https://github.com/philosowaffle/peloton-to-garmin/issues/473).  Depending on how you worked around this in your personal setup you may encounter permission issues on the latest version of P2G.

If you encounter issues, start by trying the below steps:

1. Ensure you have created a Group `p2g` with GroupId of `1015`, see [Docker User](../install/docker.md#docker-user)
1. Ensure existing config and setting files are editable by the `p2g` Group
1. Update your containers to run with `user: :p2g`. See [docker-compose.yaml](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/docker-compose-ui.yaml)

#### GitHub Action Users

If you use GitHub Actions for running P2G then the fix for this has already been applied to the latest version of the workflow file.  You can follow the normal [Updating steps](../install/github-action.md#️updating) to get the latest version.