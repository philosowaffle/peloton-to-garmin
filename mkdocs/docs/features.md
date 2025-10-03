
# Features

Convert, Backup, and Sync your Peloton workouts to Garmin Connect locally and for free.

## Workout Data

1. Workout Types Supported
    1. Bike
    1. Tread
    1. Rower
    1. Meditation
    1. Strength
    1. Outdoor
    1. and more
1. Workout Data
    1. Heart Rate
    1. Cadence
    1. Target Cadence
    1. Distance
    1. Power
    1. Laps
    1. and more
1. Strength Data
    1. Exercise Name
    1. Rep count
    1. Weight

## Garmin Sync

1. Supports Garmin accounts protected by Two Step Verification
1. Synced workouts count towards Garmin Badges and Challenges
1. Synced workouts count towards VO2 Max [1](faq.md) and Training Stress Scores

## P2G

1. Syncs on-demand or on a schedule
1. Highly configurable
1. Docker-ized
1. OpenTelemetry for the data nerds

## Typical Usage

1. Do a Peloton Workout!
    1. If you use your Garmin device to send HR data to the Peloton Bike or Tread then at the end of your workout **do not save the workout on your watch, discard it.**
1. Sync your workout with P2G
    1. You can go to your computer and manually run P2G to sync recent workouts
    1. **OR** You can configure P2G to run in the background on your computer, syncing workouts once a day
1. P2G can be configured to download your workout data and save it to your computer **AND** it can automatically upload those workouts to Garmin Connect

## Screenshots

### Converted and imported Cycling Workout

![Converted Cycling Workout](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/example_cycle.png?raw=true "Converted Cycling Workout")

#### Stats View

![Stats View](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/example_cycle02.png?raw=true "Stats View")

#### Cadence Targets

![Cadence Targets](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/cadence_target.png?raw=true "Cadence Targets")

### Laps

![Laps](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/example_laps.png?raw=true "Laps")

### UI

![P2G UI Demo](img/p2g_demo.gif "P2G UI Demo")
