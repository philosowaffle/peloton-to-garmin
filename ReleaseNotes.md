## Features

- [#320] Add support for Distance based cycling workouts

## Fixes

- [#305] Fix logging and error handling when a Deserialization error occurrs
- [#304] Fix Deserialization error when FTP source is `ftp_estimated_source`
- [#309] Fix `Found unknown distance unit m` and `kph`
- [#323] Fix scenario where Fire TV workout had null End Time and failed to deserialize

## Changes

- [#311] Sanitize Verbose logging - Prometheus HTTP labels also modified to better strip dynamic values