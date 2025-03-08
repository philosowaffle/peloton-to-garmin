[![](https://img.shields.io/static/v1?label=Sponsor&message=%E2%9D%A4&logo=GitHub&color=%23fe8e86)](https://github.com/sponsors/philosowaffle) <span class="badge-buymeacoffee"><a href="https://www.buymeacoffee.com/philosowaffle" title="Donate to this project using Buy Me A Coffee"><img src="https://img.shields.io/badge/buy%20me%20a%20coffee-donate-yellow.svg" alt="Buy Me A Coffee donate button" /></a></span>
---

## Breaking Changes

Please see the [Migration Guide](https://philosowaffle.github.io/peloton-to-garmin/latest/migration/migrate-v4-v5/) for specific instructions on how to address these breaking changes.

- [#656] Uplift to .net 9
- [#704] `DeviceInfoPath` fully removed and replaced by `DeviceInfoSettings`
- [#473] Fix running rootless docker containers (@jpecora716)

## New Features

- [#556] Stacked Classes Support - P2G can now either automatically or on-demand combine Peloton workouts into a single final activity file that is uploaded to Garmin Connect
- [#724] Annual Challenge Page - Now shows current average minutes/day and minutes/week

## Fixes

- [#711] Friendlier error messages, especially on first start when nothing is configured yet
- [#711] Fixed issue where Windows exe would somtimes fail to start on very first run, but would launch on second attempt
- [#711] Prevent users from saving Passwords that use an unsupported character
- [#732] Fixed some broken links on documentation site

## Housekeeping

- [#672] Bump all dependencies

## Docker Tags

- Console
    - `console-stable`
    - `console-latest`
    - `console-v5.0.0`
    - `console-v5`

- Api
    - `api-stable`
    - `api-latest`
    - `api-v5.0.0`
    - `api-v5`
- WebUI
    - `webui-stable`
    - `webui-latest`
    - `webui-v5.0.0`
    - `webui-v5`
