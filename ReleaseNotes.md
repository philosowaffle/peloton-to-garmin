<span class="badge-buymeacoffee"><a href="https://www.buymeacoffee.com/philosowaffle" title="Donate to this project using Buy Me A Coffee"><img src="https://img.shields.io/badge/buy%20me%20a%20coffee-donate-yellow.svg" alt="Buy Me A Coffee donate button" /></a></span>
---

## Features

- [#335] P2G can now check if a new version is available and let you know
- [#289] Add paging support to Sync UI page
	- API and WebUI must be updated together to atleast 3.1.0
- [#310] Add Arm32 docker containers

## Fixes

- [#331] Fixed - System.IO.IOException: The process cannot access the file 'deviceInfo.xml' because it is being used by another process.
- [#289] Fixed - Better logs and messaging when no convert Formats are configured in Settings
- [#342] Fixed - Failed to deserialzie UserData when `null` cycling_ftp_source
- [#343] Fixed - Back-Syncing hundreds of workouts on some computers could lead to resource exhaustion 
	- `System.IO.IOException: Unable to write data to the transport connection: An existing connection was forcibly closed by the remote host..`
- [#336] Fixed - Fixed OpenTelemetry Tracing not tracing cleanly, especially on the WebUI

## Changes

- [#316] Bump dependencies
- [#333] Refactor Converters, Settings, Configuration - general improvments
- [#318] Documentation improvements
	- Better install instructions for Mac + Docker
	- Update default configs to use new port binding to avoid permission issues on port 80