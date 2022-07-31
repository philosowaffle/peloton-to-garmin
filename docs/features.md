---
layout: default
title: Features
nav_order: 1
---

# Features

Convert, Backup, and Sync.

## Feature List

1. Syncs workout data from Peloton to Garmin Connect
1. Supports all Peloton workout types (Biking, Tread, Core, Meditation, etc.)
1. Syncs all available metric data from Peloton over to Garmin Connect
1. Syncs laps and target cadence
1. Synced workouts count towards Garmin Badges and Challenges
1. Synced workouts will count towards VO2 max calculations [1]({{ site.baseurl }}{% link configuration/providing-device-info.md %})
1. Syncs on demand or on a schedule
1. Highly Configurable
1. Docker-ized
1. OpenTelemetry for the data nerds

### Data Synced

1. HR
1. Cadence
1. Target Cadence
1. Distance
1. Power

## Typical Usage

1. Do a Peloton Workout!
    1. If you use your Garmin device to send HR data to the Peloton Bike or Tread then at the end of your workout **do not save the workout on your watch, discard it.**
1. Sync your workout with P2G
    1. You can go to your computer and manually run P2G to sync recent workouts
    1. **OR** You can configure P2G to run in the background on your computer, syncing workouts every hour
1. P2G can be configured to download your workout data and save it to your computer **AND** it can automatically upload those workouts to Garmin Connect

## Screenshots

#### Converted and imported Cycling Workout

![Converted Cycling Workout](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/example_cycle.png?raw=true "Converted Cycling Workout")

#### Stats View

![Stats View](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/example_cycle02.png?raw=true "Stats View")

#### Cadence Targets

![Cadence Targets](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/cadence_target.png?raw=true "Cadence Targets")

### Laps

![Laps](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/example_laps.png?raw=true "Laps")

### Web UI
![Web UI Demo](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/p2g_webui_demo.gif?raw=true "Web UI Demo")
