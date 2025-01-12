
# Migrating from V4 to V5

Version 4 introduces a few breaking changes.  Not every change will require action from you.  You can use the below table to see which changes may impact you based on your install method.

| Breaking Change | Build From Source | Docker Headless | Docker WebUI | GitHubAction | Windows Exe |
|:----------------|:------------------|:----------------|:-------------|:-------------|:------------|
| [.NET 9](#net-9) | ✔️ | ❌ | ❌ | ❌ | ❌ |
| [DeviceInfoPath replaced by DeviceInfoSettings](#deviceinfopath-replaced-by-deviceinfosettings) | ❌ | ✔️ | ❌ | ❌ | ✔️ |

## Breaking Changes

### .NET 9

1. Install the latest [dotnet 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### DeviceInfoPath replaced by DeviceInfoSettings

If you have not configured or made use of the [DeviceInfoPath setting](https://philosowaffle.github.io/peloton-to-garmin/v4.1.0/configuration/json/#custom-device-info) then you likely do not need to make any changes.

If you are running P2G in a way that allows it to persist its settings across restarts, then you should not need to do anything specific, P2G will automatically migrate the old `DeviceInfoPath` setting into the new `DeviceInfoSettings` format.  For example if you are using the WebUI or the Windows Exe, then you likely do not need to do anything.

Additionally, if you are running P2G v4.2.0 or later then it is possible you have already made the needed changes as the new settings format was [introduced with that version](https://github.com/philosowaffle/peloton-to-garmin/issues/606#issuecomment-1925869262).

If you are not persisting P2G settings across restarts then you will need to manually update your settings.  For example, if you use GitHub Actions to run P2G then your settings are defined in the action file and passed into P2G on each run.  If you have made use of the `DeviceInfoPath` setting then you will need to update it to follow the new format of `DeviceInfoSettings`.

The new settings structure is explained [here](https://philosowaffle.github.io/peloton-to-garmin/v5.0.0/configuration/format/#customizing-the-garmin-device-associated-with-the-workout).
