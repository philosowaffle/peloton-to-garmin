
# Migrating from V1 to V2

Version 2 of P2G is a complete rewrite of Version 1 into dotnet. Version 2 has feature parity with Version 1 and additional new features. One of the key benefits of Version 2 over Version 1 is the ability to generate and upload FIT files. FIT files are able to capture more data than the TCX format used in Version 1.

Version 2 will not interfere with your Version 1 install, so if anything doesn't work for you on V2, you can always return to using V1.

## Steps

### 1. Backup your current config

1. Navigate to where you currently have version 1 installed
1. Rename the folder to something like `p2g_v1`

### 2. Download v2

1. Download and install v2, see [Install](../install/index.md)

### 3. Migrate your config

1. Find your original v1 `config.ini` file and open it
1. In a new window, find the v2 `configuration.local.json` file and open it
1. Below walks through section by section how the old config maps to the new config values.

#### Peloton section

```bash
[PELOTON]
Email = pelotonEmail@example.com
Password = pelotonPassword
WorkoutTypes = cycling, strength 
```

| Property      | New Config       | Notes |
|:-------------|:------------------|-------|
| Email | [Peloton Config](../configuration/peloton.md).Email | |
| Password | [Peloton Config](../configuration/peloton.md).Password | |
| WorkoutTypes | [Peloton Config](../configuration/peloton.md).ExcludeWorkoutTypes | In v1 this was a list of workout types to **include**, in v2 this changes to a list of workout types to **exclude**. |

#### Garmin section

```bash
[GARMIN]
UploadEnabled = false
Email = garminEmail@example.com
Password = garminPassword
```

| Property      | New Config       | Notes |
|:-------------|:------------------|-------|
| Email | [Garmin Config](../configuration/garmin.md).Email | |
| Password | [Garmin Config](../configuration/garmin.md).Password | |
| UploadEnabled | [Garmin Config](../configuration/garmin.md).Upload | You will additionally need to specify `FormatToUpload` if you have this enabled. |

#### PTOG Section

```bash
[PTOG]
EnablePolling = false
PollingIntervalSeconds = 600
```

| Property      | New Config       | Notes |
|:-------------|:------------------|-------|
| EnablePolling | [App Config](../configuration/app.md).EnablePolling | |
| PollingIntervalSeconds | [App Config](../configuration/app.md).PollingIntervalSeconds | |

#### Output Section

```bash
[OUTPUT]
Directory = Output
WorkingDirectory = Working
ArchiveDirectory = Archive
RetainFiles = true
ArchiveFiles = true
SkipDownload = true
ArchiveByType = true
```

| Property      | New Config       | Notes |
|:-------------|:------------------|-------|
| Directory | none | |
| WorkingDirectory | [App Config](../configuration/app.md).WorkingDirectory | |
| ArchiveDirectory | [App Config](../configuration/app.md).OutputDirectory | |
| RetainFiles | [Format Config](../configuration/app.md).SaveLocalCopy | |
| ArchiveFiles | none | |
| SkipDownload | none | |
| ArchiveByType | [Format Config](../configuration/app.md).[Fit,Tcx,Json] | Set the formats you want to save to true and then set `SaveLocalCopy: true` |

#### Logger Section

```bash
[LOGGER]
LogFile = peloton-to-garmin.log
LogLevel = INFO
```

| Property      | New Config       | Notes |
|:-------------|:------------------|-------|
| LogFile | [Observability Config](../configuration/observability.md).Serilog.WriteTo.Args.Path | |
| LogLevel | [Observability Config](../configuration/observability.md).Serilog.MinimumLevel | |

For the general use case, the below config should be sufficient.

```json
"Observability": {

    "Serilog": {
      "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
      "MinimumLevel": "Information",
      "WriteTo": [
        { "Name": "Console" },
        {
          "Name": "File",
          "Args": {
            "path": "./peloton-to-garmin.txt",
            "rollingInterval": "Day",
            "retainedFileCountLimit": 7
          }
        }
      ]
    }
  }
```

#### Debug Section

```bash
[DEBUG]
PauseOnFinish = true
```

| Property      | New Config       | Notes |
|:-------------|:------------------|-------|
| PauseOnFinish | [App Config](../configuration/app.md).CloseWindowOnFinish | |
