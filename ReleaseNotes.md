[![](https://img.shields.io/static/v1?label=Sponsor&message=%E2%9D%A4&logo=GitHub&color=%23fe8e86)](https://github.com/sponsors/philosowaffle) <span class="badge-buymeacoffee"><a href="https://www.buymeacoffee.com/philosowaffle" title="Donate to this project using Buy Me A Coffee"><img src="https://img.shields.io/badge/buy%20me%20a%20coffee-donate-yellow.svg" alt="Buy Me A Coffee donate button" /></a></span>
---

## Features

- [#370] Add support for Peloton Row workouts
- [#26] Web UI - Home page now lets you quick-sync all of todays workouts

## Fixes

- [#361] When multiple convert formats were specified (FIT and TCX) the incorrect format could get uploaded to Garmin Connect
- [#353] Fix New Release Check not handling release candidates correctly

## Changes

- [#26] API - `POST api/sync` - No longer supports syncing by NumWorkouts
- [#367] Bump dependency versions + switch to use ReleaseCheck nuget