---
layout: default
title: JSON Config File
parent: Configuration
nav_order: 0
---

# Json Config File

Based on your installation method, configuration may be provided via a `configuration.local.json` or it may be done via the user interface.  In the below documentation you will see the information for both the JSON config file, and the Web UI.

By default, P2G looks for a file named `configuration.local.json` in the same directory where the program is run.

The config file is written in JSON and supports hot-reload for all fields except the following:

1. `App.PollingintervalSeconds`
1. `Observability` Section

The config file is organized into the below sections.

| Section      | Platforms | Description       |
|:-------------|:----------|:------------------|
| [Api Config](#api-config) | Web UI | This section provides global settings for the P2G Api. |
| [WebUI Config](#webui-config) | Web UI | This section provides global settings for the P2G Web UI. |
| [App Config](#app-config) | Headless | This section provides global settings for the P2G application. |
| [Format Config](#format-config) | Headless | This section provides settings related to conversions and what formats should be created/saved.  |
| [Peloton Config](#peloton-config) | Headless | This section provides settings related to fetching workouts from Peloton.      |
| [Garmin Config](#garmin-config) | Headless | This section provides settings related to uploading workouts to Garmin. |
| [Observability Config](#observability-config) | All | This section provides settings related to Metrics, Logs, and Traces for monitoring purposes. |

## Api Config

If you aren't running the Web UI version of P2G you can ignore this section.  

This section lives in `webui.local.json`.

```json
 "Api": {
      "HostUrl": "http://p2g-api:8080"
    }
```

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| HostUrl | yes | `null` | none | The host and port for the Web UI to communicate with the Api. |

### Advanced usage

Typically this section is only needed in the `webui.local.json` so that the Web UI knows where to find the running Api.  However, if you have a unique setup and need to modify the Host and Port the Api binds to, then you can also provide this config section in the `api.local.json`.

```json
 "Api": {
      "HostUrl": "http://*:8080"
    }
```

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| HostUrl | no | `http://localhost:80` | none | The host and port the Api should bind to and listen on. |

## WebUI Config

If you aren't running the Web UI version of P2G you can ignore this section.

You can provide this config section in the `webui.local.json`.

```json
 "WebUI": {
      "HostUrl": "http://*:8080"
    }
```

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| HostUrl | no | `http://localhost:80` | none | The host and port the WebUI should bind to and listen on. |

## App Config

This section provides global settings for the P2G application.

```json
 "App": {
    "OutputDirectory": "./output",
    "EnablePolling": true,
    "PollingIntervalSeconds": 86400,
    "PythonAndGUploadInstalled": true,
    "CloseWindowOnFinish": false,
    "CheckForUpdates": true
  }
```

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| OutputDirectory | no | `$PWD/output` | `App > Advanced` | Where downloaded and converted files should be saved to. |
| EnablePolling  | no | `true` | `App Tab` | `true` if you wish P2G to run continuously and poll Peloton for new workouts. |
| PollingIntervalSeconds | no | 86400 | `App Tab` | The polling interval in seconds determines how frequently P2G should check for new workouts. Be warned, that setting this to a frequency of hourly or less may get you flagged by Peloton as a bad actor and they may reset your password. The default is set to Daily. |
| CloseWindowOnFinish | no | `false` | none | `true` if you wish the console window to close automatically when the program finishes. Not that if you have Polling enabled the program will never 'finish' as it remains active to poll regularly. |
| CheckForUpdates | no | `true` | `App Tab` | `true` if P2G should check for updates and write a log message if a new release is available. If using the UI this message will display there as well. |

## Format Config

This section provides settings related to conversions and what formats should be created/saved.  P2G supports converting Peloton workouts into a variety of different formats.  P2G also lets you choose whether or not you wish to save a local copy when the conversion is completed. This can be useful if you wish to backup your workouts or upload them manually to a different service other than Garmin.

```json
"Format": {
    "Fit": true,
    "Json": false,
    "Tcx": false,
    "SaveLocalCopy": false,
    "IncldudeTimeInHRZones": false,
    "IncludeTimeInPowerZones": false,
    "DeviceInfoPath": "./deviceInfo.xml",
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
    }
  }
```

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| Fit | no | `false` | `Conversion Tab` | `true` indicates you wish downloaded workouts to be converted to FIT |
| Json | no | `false` | `Conversion Tab` | `true` indicates you wish downloaded workouts to be converted to JSON. This will automatically save a local copy when enabled. |
| Tcx  | no | `false` | `Conversion Tab` | `true` indicates you wish downloaded workouts to be converted to TCX |
| SaveLocalCopy | no | `false` | `Conversion > Advanced` | `true` will save any converted workouts to your specified [OutputDirectory](#app-config) |
| IncludeTimeInHRZones | no | `false` | `Conversion > Advanced` | **Only use this if you are unable to configure your Max HR on Garmin Connect.** When set to True, P2G will attempt to capture the time spent in each HR Zone per the data returned by Peloton. See [understanding custom zones](#understanding-custom-zones).
| IncludePowerInHRZones  | no | `false` | `Conversion > Advanced` | **Only use this if you are unable to configure your FTP and Power Zones on Garmin Connect.** When set to True, P2G will attempt to capture the time spent in each Power Zone per the data returned by Peloton. See [understanding custom zones](#understanding-custom-zones). |
| DeviceInfoPath | no | `null` | `Conversion > Advanced` | The path to your `deviceInfo.xml` file. See [providing device info](#custom-device-info) |
| Cycling | no | `null` | none | Configuration specific to Cycling workouts. |
| Cycling.PreferredLapType | no | `Default` | `Conversion Tab` | The preferred [lap type to use](#lap-types). |
| Running | no | `null` | none | Configuration specific to Running workouts. |
| Running.PreferredLapType | no | `Default` | `Conversion Tab` | The preferred [lap type to use](#lap-types). |
| Rowing | no | `null` | none | Configuration specific to Rowing workouts. |
| Rowing.PreferredLapType | no | `Default` | `Conversion Tab` | The preferred [lap type to use](#lap-types). |
| Strength | no | `null` | `Conversion Tab` | Configuration specific to Strength workouts. |
| Strength.DefaultSecondsPerRep | no | `3` | `Conversion Tab` | For exercises that are done for time instead of reps, P2G can estimate how many reps you completed using this value. Ex. If `DefaultSecondsPerRep=3` and you do Curls for 15s, P2G will estimate you completed 5 reps. |

### Understanding Custom Zones

Garmin Connect expects that users have a registered device and they expect users have set up their HR and Power Zones on that device. However, this presents a problem if you either A) do not have a device capable of tracking Power or B) do not have a Garmin device at all.

The most common scenario for Peloton users is A, where they do not own a Power capable Garmin device and therefore are not able to configure their Power Zones in Garmin Connect.  If you do not have Power or HR zones configured in Garmin Connect then you are not able to view accurate `Time In Zones` charts for a given workout.

P2G provides a work around for this by optionally enriching the workout with the `Time In Zones` data with one caveat: the chart will not display the range value for the zone.

![Example Cycling Workout](https://github.com/philosowaffle/peloton-to-garmin/blob/master/images/missing_zone_values.png?raw=true "Example Missing Zone Values")

This is only available when generating and uploading the [FIT](#garmin-config) format.

### Custom Device Info

By default, P2G using a custom device when converting and upload workouts.  This device information is needed in order to count your Peloton workouts towards Challenges and Badges on Garmin. However, you may observe on Garmin Connect that your Peloton workouts will show a device image that does not match your personal device.

If you choose, you can provide P2G with your personal Device Info which will cause the Garmin workout to show the correct to device. Note, **this is completely optional and is only for cosmetic preference**, your workout will be converted, uploaded, and counted towards challenges regardless of whether this matches your personal device.

See [configuring device info]({{ site.baseurl }}{% link configuration/providing-device-info.md %}) for detailed steps on how to create your `deviceInfo.xml`.

### Lap Types

P2G supports several different strategies for creating Laps in Garmin Connect.  If a certain strategy is not available P2G will attempt to fallback to a different strategy.  You can override this behavior by specifying your preferred Lap type in the config. When `PreferredLapType` is set, P2G will first attempt to generate your preferred type and then fall back to the default behavior if it is unable to.  By default P2G will:

1. First try to create laps based on `Class_Targets`
1. Then try to create laps based on `Class_Segments`
1. Finally fallback to create laps based on `Distance`

| Strategy  | Config Value | Description |
|:----------|:-------------|:------------|
| Class Targets | `Class_Targets` | If the Peloton data includes Target Cadence information, then laps will be created to match any time the Target Cadence changed.  You must use this strategy if you want the Target Cadence to show up in Garmin on the Cadence chart. |
| Class Segments | `Class_Segments` | If the Peloton data includes Class Segment information, then laps will be created to match each segment: Warm Up, Cycling, Weights, Cool Down, etc. |
| Distance | `Distance` | P2G will caclulate Laps based on distance for each 1mi, 1km, or 500m (for Row only) based on your distance setting in Peloton. |

## Peloton Config

This section provides settings related to fetching workouts from Peloton.

```json
"Peloton": {
    "Email": "peloton@gmail.com",
    "Password": "peloton",
    "NumWorkoutsToDownload": 1,
    "ExcludeWorkoutTypes": [ "meditation" ]
  }
```

⚠️ Console or Docker Headless: Your username and password for Peloton and Garmin Connect are stored in clear text, which **is not secure**. Please be aware of the risks. ⚠️

⚠️ WebUI version 3.3.0: Credentials are stored **encrypted**.

⚠️ GitHub Actions: Credentials are stored **encrypted**.

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| Email | **yes** | `null` | `Peloton Tab` | Your Peloton email used to sign in |
| Password | **yes** | `null` | `Peloton Tab` | Your Peloton password used to sign in |
| NumWorkoutsToDownload | no | 5 | `Peloton Tab` | The default number of workouts to download. See [choosing number of workouts to download](#choosing-number-of-workouts-to-download).  Set this to `0` if you would like P2G to prompt you each time for a number to download. |
| ExcludeWorkoutTypes | no | none | `Peloton Tab` | A comma separated list of workout types that you do not want P2G to download/convert/upload. See [example use cases](#exclude-workout-types) below. |

### Choosing Number of Workouts To Download

When choosing the number of workouts P2G should download each polling cycle its important to keep your configured [PollingInterval](#app-config) in mind. If, for example, your polling interval is set to hourly, then you may want to set `NumWorkoutsToDownload` to 4 or greater. This ensures if you did four 15min workouts during that hour they would all be captured.

### Exclude Workout Types

Example use cases:

1. You take a wide variety of Peloton classes, including meditation and you want to skip uploading meditation classes.
1. You want to avoid double-counting activities you already track directly on a Garmin device, such as outdoor running workouts.

The available values are:

```json
  Cycling
  BikeBootcamp
  TreadmillRunning
  OutdoorRunning
  TreadmillWalking
  OutdoorWalking
  Cardio
  Circuit
  Strength
  Stretching
  Yoga
  Meditation
```

## Garmin Config

This section provides settings related to uploading workouts to Garmin.

```json
"Garmin": {
    "Email": "garmin@gmail.com",
    "Password": "garmin",
    "TwoStepVerificationEnabled": false,
    "Upload": false,
    "FormatToUpload": "fit",
    "UploadStrategy": 2
  }
```

⚠️ Console or Docker Headless: Your username and password for Peloton and Garmin Connect are stored in clear text, which **is not secure**. Please be aware of the risks. ⚠️

⚠️ WebUI version 3.3.0: Credentials are stored **encrypted**.

⚠️ GitHub Actions: Credentials are stored **encrypted**.

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| Email | **yes - if Upload=true** | `null` | `Garmin Tab` | Your Garmin email used to sign in |
| Password | **yes - if Upload=true** | `null` | `Garmin Tab` | Your Garmin password used to sign in |
| TwoStepVerificationEnabled | no | `false` | `Garmin Tab` | Whether or not your Garmin account is protected by Two Step Verification |
| Upload | no | `false` | `Garmin Tab` |  `true` indicates you wish downloaded workouts to be automatically uploaded to Garmin for you. |
| FormatToUpload | no | `fit` | `Garmin Tab > Advanced` | Valid values are `fit` or `tcx`. Ensure the format you specify here is also enabled in your [Format config](#format-config) |
| UploadStrategy | **yes if Upload=true** | `null` |  `Garmin Tab > Advanced` |  Allows configuring different upload strategies for syncing with Garmin. Valid values are `[0 - PythonAndGuploadInstalledLocally, 1 - WindowsExeBundledPython, 2 - NativeImplV1]`. See [upload strategies](#upload-strategies) for more info. |

### Upload Strategies

Because Garmin does not officially support 3rd party uploads by small projects like P2G, over time we have occassionally encountered upload issues.  This has caused P2G's upload strategy to evolve.  Based on your installation method and or geo location, different upload strategies have worked for different people at different times.

If you are just getting started with P2G, I recommend you start with upload strategy `2 - NativeImplV1`.  You can find more details about the strategies below.

| Strategy  | Config Value | Supports Garmin Two Step Verification| Description |
|:----------|:-------------|:-------------------------------------|:------------|
| PythonAndGuploadInstalledLocally | 0 | maybe | The very first strategy P2G used. This assumes you have Python 3 and the [garmin-uploader](https://github.com/La0/garmin-uploader) python library already installed on your computer.  This strategy uses the `garmin-uploader` python library for handling all uploads to Garmin. |
| WindowsExeBundledPython | 1 | no | If you are running the windows executable version of P2G and would like to use the [garmin-uploader](https://github.com/La0/garmin-uploader) python library for uploads then use this strategy. |
| NativeImplV1 | 2 | yes | **The most current and recommended upload strategy.** P2G preforms the upload to Garmin itself without relying on 3rd party libraries. |

## Observability Config

P2G supports publishing OpenTelemetry Metrics, Logs, and Trace. This section provides settings related to those pillars.

The Observability config section contains three main sub-sections:

1. [Prometheus](#prometheus-config) - Metrics
1. [Jaeger](#jaeger-config) - Traces
1. [Serilog](#serilog-config) - Logs

```json
"Observability": {

    "Prometheus": {
      "Enabled": false,
      "Port": 4000
    },

    "Jaeger": {
      "Enabled": false,
      "AgentHost": "localhost",
      "AgentPort": 6831
    },

    "Serilog": {
      "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
      "MinimumLevel": "Information",
      "WriteTo": [
        { "Name": "Console" },
        {
          "Name": "File",
          "Args": {
            "path": "./output/log.txt",
            "rollingInterval": "Day",
            "retainedFileCountLimit": 7
          }
        }
      ]
    }
  }
```

### Prometheus Config

```json
"Prometheus": {
      "Enabled": false,
      "Port": 4000
    }
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Enabled | no | `false` | Whether or not to expose metrics. Metrics will be available at `http://localhost:{port}/metrics` |
| Port | no | `80` | The port the metrics endpoint should be served on. Only valid for Console mode, not Api/WebUI |

If you are using Docker, ensure you have exposed the port from your container.

#### Example Prometheus scraper config

```yaml
- job_name: 'p2g'
    scrape_interval: 60s
    static_configs:
      - targets: [<p2gIPaddress>:<p2gPort>]
    tls_config:
      insecure_skip_verify: true
```

### Jaeger Config

```json
"Jaeger": {
      "Enabled": false,
      "AgentHost": "localhost",
      "AgentPort": 6831
    }
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Enabled | no | `false` | Whether or not to generate traces. |
| AgentHost | **yes - if Enalbed=true** | `null` | The host address for your trace collector. |
| AgentPort | **yes - if Enabled=true** | `null` | The port for your trace collector. |

### Serilog Config

```json
"Serilog": {
      "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Grafana.Loki" ],
      "MinimumLevel": {
        "Default": "Information",
        "Override": {
          "Microsoft": "Error",
          "System": "Error"
        }
      },
      "WriteTo": [
        { "Name": "Console" },
        {
          "Name": "File",
          "Args": {
            "path": "./output/log.txt",
            "rollingInterval": "Day",
            "retainedFileCountLimit": 7
          }
        },
        {
          "Name": "GrafanaLoki",
          "Args": {
            "uri": "http://192.168.1.95:3100",
            "textFormatter": "Serilog.Sinks.Grafana.Loki.LokiJsonTextFormatter, Serilog.Sinks.Grafana.Loki",
            "labels": [
              {
                "key": "app",
                "value": "p2g"
              }
            ]
          }
        }]
}
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Using | no | `null` | A list of sinks you would like use. The valid sinks are listed in the examplea above. |
| MinimumLevel | no | `null` | The minimum level to write. `[Verbose, Debug, Information, Warning, Error, Fatal]` |
| WriteTo | no | `null` | Additional config for various sinks you are writing to. |

More detailed information about configuring Logging can be found on the [Serilog Config Repo](https://github.com/serilog/serilog-settings-configuration#serilogsettingsconfiguration--).
