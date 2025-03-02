# Format Settings

The Format Settings provide settings related to how workouts should be converted from Peloton.

## Settings location

| Run Method | Location |
|------------|----------|
| Web UI     |  UI > Settings > Conversion Tab  |
| Windows Exe | UI > Settings > Conversion Tab |
| GitHubAction | Config Section in Workflow |
| Headless (Docker or Console) | Config section in `configuration.local.json` |

## File Configuration

```json
"Format": {
    "Fit": true,
    "Json": false,
    "Tcx": false,
    "SaveLocalCopy": false,
    "IncldudeTimeInHRZones": false,
    "IncludeTimeInPowerZones": false,
    "DeviceInfoSettings": { /**(1)!**/ }
    "Cycling": {
      "PreferredLapType": "Class_Targets"
    },
    "Running": {
      "PreferredLapType": "Distance"
    },
    "Rowing": {
      "PreferredLapType": "Class_Segments"
    },
    "Strength": {
      "DefaultSecondsPerRep": 3
    },
    "WorkoutTitleTemplate": "{{PelotonWorkoutTitle}} with {{PelotonInstructorName}}",
    "StackedWorkouts": { /**(2)!**/  }
  }
```

1. Jump to [Device Info Settings Documentation](#customizing-the-garmin-device-associated-with-the-workout)
1. Jump to [StackedWorkouts Documentation](#stacked-workouts)

## Settings Overview

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Fit | no | `false` | `true` indicates you wish downloaded workouts to be converted to FIT |
| Json | no | `false` | `true` indicates you wish downloaded workouts to be converted to JSON. This will automatically save a local copy when enabled. |
| Tcx  | no | `false` | `true` indicates you wish downloaded workouts to be converted to TCX |
| SaveLocalCopy | no | `false` | `true` will save any converted workouts to the output directory. |
| IncludeTimeInHRZones | no | `false` | **Only use this if you are unable to configure your Max HR on Garmin Connect.** When set to True, P2G will attempt to capture the time spent in each HR Zone per the data returned by Peloton. See [understanding P2G provided zones](#understanding-p2g-provided-zones). |
| IncludePowerInHRZones  | no | `false` | **Only use this if you are unable to configure your FTP and Power Zones on Garmin Connect.** When set to True, P2G will attempt to capture the time spent in each Power Zone per the data returned by Peloton. See [understanding P2G provided zones](#understanding-p2g-provided-zones). |
| DeviceInfoSettings | no | `null` | See [customizing the Garmin device associated with the workout](#customizing-the-garmin-device-associated-with-the-workout). |
| Cycling | no | `null` | Configuration specific to Cycling workouts. |
| Cycling.PreferredLapType | no | `Default` | The preferred [lap type to use](#lap-types). |
| Running | no | `null` | Configuration specific to Running workouts. |
| Running.PreferredLapType | no | `Default` | The preferred [lap type to use](#lap-types). |
| Rowing | no | `null` | Configuration specific to Rowing workouts. |
| Rowing.PreferredLapType | no | `Default` | The preferred [lap type to use](#lap-types). |
| Strength | no | `null` | Configuration specific to Strength workouts. |
| Strength.DefaultSecondsPerRep | no | `3` | For exercises that are done for time instead of reps, P2G can estimate how many reps you completed using this value. Ex. If `DefaultSecondsPerRep=3` and you do Curls for 15s, P2G will estimate you completed 5 reps. |
| WorkoutTitleTemplate | no | `{{PelotonWorkoutTitle}} with {{PelotonInstructorName}}` | Customize the workout title shown in Garmin Connect. [Read More...](#workout-title-templating) |
| StackedWorkouts | no | `disabled` | Enable/disable workout stacking. [Read More...](#stacked-workouts) |

## Understanding P2G Provided Zones

!!! danger
    If either custom zone setting is enable it is possible Garmin will **not** calculate Training Load, Effect, V02 Max, or related fields.  Use these settings with caution.

Garmin Connect expects that users have a registered device and they expect users have set up their HR and Power Zones on that device. However, this presents a problem if you either:

* A) do not have a device capable of tracking Power
* B) do not have a Garmin device at all.

The most common scenario for Peloton users is scenario `A`, where they do not own a Power capable Garmin device and therefore are not able to configure their Power Zones in Garmin Connect.  If you do not have Power or HR zones configured in Garmin Connect then you are not able to view accurate `Time In Zones` charts for a given workout.

P2G provides a work around for this by optionally enriching the workout with the `Time In Zones` data with one caveat: the chart will not display the range value for the zone.

![Example Cycling Workout](https://github.com/philosowaffle/peloton-to-garmin/blob/master/images/missing_zone_values.png?raw=true "Example Missing Zone Values")

This is only available when generating and uploading the [FIT](garmin.md) format.

## Customizing the Garmin Device Associated with the workout

Workouts uploaded to Garmin Connect must report what device they were recorded on.  The device chosen impacts what additional data fields are calculated and shown by Garmin.

For example, the device a workout is recorded on can impact:

1. Whether or not the workout will count towards Challenges and Badges
1. Whether or not Garmin will calculate things like TSS, TE, Load, and VO2 Max

For this reason, P2G provides [reasonable defaults](#p2g-default-devices) to ensure users get the most data possible on their workouts out of the box.

If you choose to customize what devices are used by P2G you can do that either via the [ui](#configuring-device-info-via-the-ui) or via [config file](#configuring-device-info-via-config-file).

### P2G Default Devices

| Exercise Type | Default Device Used |
|---------------|---------------------|
| Default | Forerunner 945 |
| Cycling | Taxc Training App Windows |
| Rowing | Epix |

### Configuring Device Info via the UI

Under `Settings > Conversion > Advanced > Device Info Settings` you can see which devices will be used for each [Exercise Type](exercise-types.md).  You can modify this list to suit your needs.

The `None` [Exercise Type](exercise-types.md) serves as a global default used for any [Exercise Type](exercise-types.md) not configured.

[Learn more about finding Device Info to use.](#discovering-garmin-devices)

### Configuring Device Info via Config File

This config section allows you to specificy a Device per [Exercise Type](exercise-types.md).  The `None` [Exercise Type](exercise-types.md) serves as a global default used for any [Exercise Type](exercise-types.md) not configured.

[Learn more about finding Device Info to use.](#discovering-garmin-devices)

```json
"DeviceInfoSettings": {
        "none": {
          "name": "Forerunner 945",
          "unitId": 1,
          "productID": 3113,
          "manufacturerId": 1,
          "version": {
            "versionMajor": 19,
            "versionMinor": 2.0,
            "buildMajor": 0,
            "buildMinor": 0.0
          }
        },
        "cycling": {
          "name": "TacxTrainingAppWin",
          "unitId": 1,
          "productID": 20533,
          "manufacturerId": 89,
          "version": {
            "versionMajor": 1,
            "versionMinor": 30.0,
            "buildMajor": 0,
            "buildMinor": 0.0
          }
        },
        "rowing": {
          "name": "Epix",
          "unitId": 3413684246,
          "productID": 3943,
          "manufacturerId": 1,
          "version": {
            "versionMajor": 10,
            "versionMinor": 43.0,
            "buildMajor": 0,
            "buildMinor": 0.0
          }
        }
      }
```

### Discovering Garmin Devices

You can find the Device Information for any previous workouts you have uploaded by following the below steps:

1. Get your Garmin current device info
    1. Log on to Garmin connect and find an activity you recorded with your device
    1. In the upper right hand corner of the activity, click the gear icon and choose `Export to TCX`
    1. A TCX file will be downloaded to your computer
1. Find the TCX file you downloaded in part 1 and open it in any text editor.
    1. Use `ctrl-f` (`cmd-f` on mac) to find the `<Creator` section in the file, it will look similar to the sample device info below.
1. Use the values found in this section to Configure your custom device in P2G either via [config file](#configuring-device-info-via-config-file) or the [ui](#configuring-device-info-via-the-ui)

```xml
<Creator>
  <Name>Garmin Sample Device - please create from exported TCX file</Name>
  <UnitId>00000000000</UnitId>
  <ProductID>0000</ProductID>
  <Version>
    <VersionMajor>0</VersionMajor>
    <VersionMinor>0</VersionMinor>
    <BuildMajor>0</BuildMajor>
    <BuildMinor>0</BuildMinor>
  </Version>
</Creator>
```

## Lap Types

P2G supports several different strategies for creating Laps in Garmin Connect.  If a certain strategy is not available P2G will attempt to fallback to a different strategy.  You can override this behavior by specifying your preferred Lap type in the config. When `PreferredLapType` is set, P2G will first attempt to generate your preferred type and then fall back to the default behavior if it is unable to.  By default P2G will:

1. First try to create laps based on `Class_Targets`
1. Then try to create laps based on `Class_Segments`
1. Finally fallback to create laps based on `Distance`

| Strategy  | Config Value | Description |
|:----------|:-------------|:------------|
| Class Targets | `Class_Targets` | If the Peloton data includes Target Cadence information, then laps will be created to match any time the Target Cadence changed.  You must use this strategy if you want the Target Cadence to show up in Garmin on the Cadence chart. |
| Class Segments | `Class_Segments` | If the Peloton data includes Class Segment information, then laps will be created to match each segment: Warm Up, Cycling, Weights, Cool Down, etc. |
| Distance | `Distance` | P2G will caclulate Laps based on distance for each 1mi, 1km, or 500m (for Row only) based on your distance setting in Peloton. |

## Workout Title Templating

P2G allows some limited customization of the title that will be used on the workout imported to Garmin.

By default the title is structured like:

```
10min HITT Ride with Ally Love
```

### Customizing the Title

Title customization is provided via "templating", which allows you to provide a template that P2G should follow when constructing a workout title.  The specific templating syntax P2G supports is [Handlebars](https://github.com/Handlebars-Net/Handlebars.Net).

**The below data fields are available for use in the template:**

* `PelotonWorkoutTitle` - Peloton provides this usually in the form of "10 min HITT Ride"
* `PelotonInstructorName` - Peloton provides this as the full instructors name: "Ally Love"

These can be used to build a template like so:

```
{{PelotonWorkoutTitle}}{{#if PelotonInstructorName}} with {{PelotonInstructorName}}{{/if}}
```

The above template will always start with the Peloton workout title.  **IF** the workout has Instructor information, then the template will add `with Instructor` after the workout title.

Some characters are not allowed to be used in the workout titles. If you use an unsupported character then it will automatically be replaced with a dash (`-`).

Additionally, Garmin has a limit on how long a title will be. If the title exceeds this limit (~45 characters) then the title will be truncated.

**Note:**

For this setting to take effect, your Garmin Connect account must be set to allow custom workout names.  In the Garmin Connect web interface click on the user icon in the top right, select `Account Settings` then `Display Preferences` ([shortcut](https://connect.garmin.com/modern/settings/displayPreferences)).
Change the `Activity Name` setting to `Workout Name (when available)`.  This will allow the custom workout name to sync, and should still allow the standard behavior when syncing non-P2G activities directly.

## Stacked Workouts

On Peloton, you can build "workout stacks" which allow you to seamlessly ride straight through several back to back classes for a longer overall workout.  By default, P2G will upload each of the stacked classes as an individual workout to Garmin.  There are a few drawbacks to this approach:

1. Badges - You won't be able to earn certain Badges for duration since Garmin expects you to hit the goal duration within a single activity
1. Workout Plans - You may not get credit towards your Workout Plan also because Garmin expects the goal to be met within a single activity file
1. TE/TSS/VO2 - These metrics may not be accurately reflected because the overall combined duration is not accounted for by Garmin

In order to address the above, P2G provides the option to combine stacked workouts into a single activity file that will be uploaded to Garmin.

There are two main ways to use this feature:

1. [Manual Stacking](#manual-stacking)
1. [Automatic Stacking](#automatic-stacking)

### Manual Stacking

Manual Stacking refers to you explicitly choosing a list of workouts that you would like combined.  You can only do this from the UI on the `Sync` page.

1. Select the workouts you would like combined, ensure they are all of the same Workout Type
1. Enable the `Stack Workouts` toggle at the top of the page
1. Click Sync

It is important to note that when manually stacking workouts P2G does not honor any configured `StackedWorkouts` settings, this means that if you selected two workouts that were several hours a part, P2G will stack them.

### Automatic Stacking

Automatic stacking is where P2G always attempts to detect if workouts should be combined and then combines them for you.

In order for a set of workouts to be automatically stacked the following must be true:

1. Automatic Stacking is enabled in P2G Settings
1. All the workouts are of the same exercise type (i.e. all Cycling or all Strength)
1. Each workout must start within X seconds of the previous workouts end time (configured in settings)

#### File Configuration

```json
"StackedWorkouts": {
        "AutomaticallyStackWorkouts": true,
        "MaxAllowedGapSeconds": 300
      }
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| AutomaticallyStackWorkouts | no | `false` | `true` indicates P2G should automatically detect stacked workouts and combine them|
| MaxAllowedGapSeconds | no | `300` | When `AutomaticallyStackWorkouts` is enabled, P2G will use this value to detect workouts that should be combined.  For example, a value of 300 means that any workout starting within 300s (or 5min) of the previous workout should be considered "stacked". |

If you use the UI, then you should see similar options available to you on the Conversion Settings page.
