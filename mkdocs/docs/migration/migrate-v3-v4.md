
# Migrating from V3 to V4

Version 3 only includes one breaking change that some users will need to account for. Based on your install type you can find what changes need to be made below.

## Windows Exe

The P2G windows exe now provides a proper user interface. You can migrate to this new version simply by following the [install steps](../install/windows.md).  You will need to re-configure P2G using the user interface as your settings will not migrate over.

There is no risk installing v4 and trying it out. Your previous install will continue to work while you test out v4.  When you're satisfied with v4 you can delete your previous version of P2G.

## GitHub Action

Follow the [updating instructions for GitHub Actions](../install/github-action.md#updating).  A couple notable changes that will be pulled in:

1. [Container Image tag](https://github.com/philosowaffle/peloton-to-garmin/blob/v4.0.0/.github/workflows/sync_peloton_to_garmin.yml#L23) has changed to `console-stable`, you may wish to edit this to be `console-v4`
1. The [configuration options](https://github.com/philosowaffle/peloton-to-garmin/blob/v4.0.0/.github/workflows/sync_peloton_to_garmin.yml#L40) have changed slightly with some fields being deprecated and removed
1. The [command to run p2g](https://github.com/philosowaffle/peloton-to-garmin/blob/v4.0.0/.github/workflows/sync_peloton_to_garmin.yml#L75) has changed to `/app/ConsoleClient`.

## Docker Headless

No specific migration steps are needed, however please take note of the [breaking changes](https://github.com/philosowaffle/peloton-to-garmin/releases/tag/v3.6.0) in case any of these impact your setup.

## Docker WebUI

No specific migration steps are needed, however please take note of the [breaking changes](https://github.com/philosowaffle/peloton-to-garmin/releases/tag/v3.6.0) in case any of these impact your setup.

## Docker API

No specific migration steps are needed, however please take note of the [breaking changes](https://github.com/philosowaffle/peloton-to-garmin/releases/tag/v3.6.0) in case any of these impact your setup.

## Source

1. Install the latest [dotnet 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)

Please take note of the [breaking changes](https://github.com/philosowaffle/peloton-to-garmin/releases/tag/v3.6.0) in case any of these impact your setup.
