---
layout: default
title: Features
nav_order: 1
---

# Features

Convert workout data from Peloton into a format that can be uploaded to Garmin.

## Feature List

1. Syncs workout data from Peloton to Garmin Connect
1. Syncs on demand or on a cadence
1. Highly Configurable
1. Docker-ized
1. Syncs all available metric data from Peloton over to Garmin Connect
1. Syncs laps and target cadence when available
1. Synced workouts count towards Garmin Badges and Challenges
1. OpenTelemetry for the data nerds

### Data Synced

1. HR
1. Cadence
1. Target Cadence
1. Distance
1. Power

## Typical Usage

1. You either manually start the application or have it running in the background polling Peloton for new workouts regularly
1. In either scenario, when a new workout is found, the application fetches all of the relevant workout data from Peloton
1. Based on your configuration, P2G can save the raw data from Peloton as JSON, TCX, and/or FIT files on your file system
1. Based on your configuration, P2G can then automatically upload either the TCX or FIT file to Garmin Connect

## Screenshots

#### Converted and imported Cycling Workout

![Converted Cycling Workout](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/example_cycle.png?raw=true "Converted Cycling Workout")

#### Stats View

![Stats View](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/example_cycle02.png?raw=true "Stats View")

#### Cadence Targets

![Cadence Targets](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/cadence_target.png?raw=true "Cadence Targets")

### Laps

![Laps](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/example_laps.png?raw=true "Laps")
