---
layout: default
title: Configuration
nav_order: 3
has_children: true
---

# Configuration

P2G supports configuration via [command line arguments]({{ site.baseurl }}{% link configuration/command-line.md %}), [environment variables]({{ site.baseurl }}{% link configuration/environment-variables.md %}), [json config file]({{ site.baseurl }}{% link configuration/json.md %}), and via the user interface. By default, P2G looks for a file named `configuration.local.json` in the same directory where it is run.

## Quick Start

Note: If you're running the new web user interface then your sample config can be found [here](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/)).

1. Copy the example below into your `configuration.local.json`
1. In the `Peloton` section set your peloton email and password
1. If you wish P2G to automatically upload to Garmin then in the `Garmin` section set your Garmin email and password and also set `"Upload": true,`

## Example Config

```json
{
  "App": {
    "OutputDirectory": "./output",
    "EnablePolling": false,
    "PollingIntervalSeconds": 86400,
    "CloseWindowOnFinish": false
  },

  "Format": {
    "Fit": true,
    "Json": false,
    "Tcx": false,
    "SaveLocalCopy": true,
    "IncludeTimeInHRZones": false,
    "IncludeTimeInPowerZones": false,
    "DeviceInfoPath": "./deviceInfo.xml"
  },

  "Peloton": {
    "Email": "peloton@gmail.com",
    "Password": "peloton",
    "NumWorkoutsToDownload": 1,
    "ExcludeWorkoutTypes": [ "meditation" ]
  },

  "Garmin": {
    "Email": "garmin@gmail.com",
    "Password": "garmin",
    "Upload": false,
    "FormatToUpload": "fit",
    "UploadStrategy": 2
  },

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
        }
      ]
    }
  }
}
```

## Config Precedence

The following defines the precedence in which config definitions are honored. With the first item overriding any below it.

1. Command Line
1. Environment Variables
1. Config File

For example, if you defined your Peloton credentials ONLY in the Config file, then the Config file credentials will be used.

If you defined your credentials in both the Config file AND the Environment variables, then the Environment variable credentials will be used.

If you defined credentials using all 3 methods (config file, env, and command line), then the credentials provided via the command line will be used.
