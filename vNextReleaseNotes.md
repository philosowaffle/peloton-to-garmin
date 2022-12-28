[![](https://img.shields.io/static/v1?label=Sponsor&message=%E2%9D%A4&logo=GitHub&color=%23fe8e86)](https://github.com/sponsors/philosowaffle) <span class="badge-buymeacoffee"><a href="https://www.buymeacoffee.com/philosowaffle" title="Donate to this project using Buy Me A Coffee"><img src="https://img.shields.io/badge/buy%20me%20a%20coffee-donate-yellow.svg" alt="Buy Me A Coffee donate button" /></a></span>
---

## Features

- [#381] Rowing - now captures AvgStrokeDistance
- [#374] Rowing - can now configure PreferredLapType for Row workouts
- [#366] WebUI - Can now Clear your Peloton or Garmin Password via the UI
- [#301] WebUI - Better feedback and error handling when saving Settings changes
- [#358] Docker - Begin publishing Docker Images on GitHub Packages
- [#358] Docker - Introduce new Docker major version Release tag - allows you to pin to all updates to a major version i.e. `v3`
- [#310] Docker - Arm 32bit image is now available

## Changes

- [#384] Bump dependency versions + Pull latest Garmin SDK (21.94)
- [#366] WebUI - Credentials are now stored encrypted
	- A one time migration step will happen on startup to encrypt your existing credentials
	- If a problem occurs you may have to re-configure your Peloton and Garmin credentials
- [#399] WebUI - Existing settings will be migrated to a new format associated with a UserId
	- A one time migration step will happen on startup to move existing settings to be associated with a UserId
	- If a problem occurs you may have to re-configure your settings