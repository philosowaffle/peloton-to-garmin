---
layout: default
title: Docker - Web UI
parent: Install
nav_order: 1
---

# Docker - Web UI

With version 3, P2G now ships with an optional User Interface. Some key features include:

1. Configure your settings via a user interface
1. Trigger a sync from any browser (your computer, your phone, etc.)
1. Sync service can still run in the background, syncing periodically
1. OpenApi for custom scripts and workflows

## docker-compose

With the below docker configuration you can launch the containers and visit `http://localhost:8002` to reach the `p2g` web UI.

```yaml
version: "3.9"

services:
  p2g-api:
    container_name: p2g-api
    image: philosowaffle/peloton-to-garmin:api-stable
    ports:
      - 8001:80
    environment:
      - TZ=America/Chicago
    volumes:
      - ./api.local.json:/app/configuration.local.json # see sample below
      - ./data:/app/data # recommended for saving settings across restarts
      - ./output:/app/output # optional, if you want access to the generated workout and log files
  
  p2g-webui:
    container_name: p2g-webui
    image: philosowaffle/peloton-to-garmin:webui-stable
    ports:
      - 8002:80
    environment:
      - TZ=America/Chicago
    volumes:
      - ./webui.local.json:/app/configuration.local.json # see sample below
```

## Configuration

If you are migrating to the Web UI for the first time you will need to reconfigure most of your settings using the user interface.  The only settings that are carried over and still configured via the configuration file are the ones related to `Observability` and `Api`.

You can find examples of how the new configuration file should look below.

### Sample webui.local.json

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

### OpenApi

The web version also exposes an API that can be interacted with. You can find documentation for the api at `http://192.18.1.94:8001/swagger`.