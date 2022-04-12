---
layout: default
title: Experimental
nav_order: 6
---

# Experimental

New enhancements not quite ready for the prime time can be found here. Use at own risk.

## Web UI

Still in the very early alpha stages, P2G can now be run with a UI in the browser.  Some key features

1. Configure your settings via a user interface
1. Trigger a sync from the browser (your computer, your phone, etc.)
1. Sync service can still run in the background, syncing periodically
1. OpenApi for custom scripts and workflows

```yaml
version: "3.9"

services:
  p2g-api:
    container_name: p2g-api
    image: philosowaffle/peloton-to-garmin:api-latest
    ports:
      - 8001:80 # only need to do this if you want /metrics or to use the api directly
    environment:
      - TZ=America/Chicago
    volumes:
      - ./configuration.local.json:/app/api.local.json # see sample below
      - ./data:/app/data # recommended for saving settings across restarts
      - ./output:/app/output # optional, if you want access to the generated workout and log files
  
  p2g-webui:
    container_name: p2g-webui
    image: philosowaffle/peloton-to-garmin:webui-latest
    ports:
      - 8002:80
    environment:
      - TZ=America/Chicago
    volumes:
      - ./configuration.local.json:/app/webui.local.json # see sample below
```

With the above docker configuration you can launch the containers and visit `http://localhost:8002` to reach the `p2g` web UI.

### Sample api.local.json

The Api can be configured with its own configuration if desired, otherwise it is fine for it to share the webui config file.

```json
{
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
}
```

### Sample webui.local.json

In the Web UI application, all common settings are configured via the UI. Your previous settings will not be carried forward, you will need to re-configure P2G using the Settings page on the UI.

The only exception to this is the Observability configuration section and a new `Api` section, which is still configred via the file.

```json
{
  "Api": {
    "HostUrl": "http://p2g-api"
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
}
```

### Polling Service

If configured, the polling service will run as normal in the background even when the Web UI is enabled.

### OpenApi

The web version also exposes an API that can be interacted with. You can find documentation for the api at `http://192.18.1.94:8001/swagger`.  Expect breaking changes will be made to this api until this reaches a more final release state.
