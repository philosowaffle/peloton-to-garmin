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

![Web UI Demo](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/p2g_webui_demo.gif?raw=true "Web UI Demo")

## docker-compose

*Pre-requisite:* You have either `docker-compose` or `Docker Desktop` installed

1. Create a folder named `p2g-webui`
    1. Inside this folder create [docker-compose.yaml](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/docker-compose-ui.yaml)
    1. Also create [api.local.json](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/api.local.json)
    1. Also create [webui.local.json](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/webui.local.json)
1. Open a terminal in this folder
1. Run: `docker-compose pull && docker-compose up -d`
    1. This will pull the containers and start them up running in the background
    1. You can close the terminal at this time
1. Open a browser and navigate to `http://localhost:8002`

Any logs or generated files will be available in the `output` directory.  Additionally, you can learn more about customizing your configuration over in the [Configuration Section]({{ site.baseurl }}{% link configuration/index.md %})

### To stop P2G

1. You can use Docker Desktop application to kill the containers
1. Or, you can open a terminal in the `p2g-webui` folder
    1. Run: `docker-compose down`

### To update P2G

1. Open a terminal in the `p2g-webui` folder
    1. Run: `docker-compose pull && docker-compose up -d`

## Configuration

If you are migrating to the Web UI for the first time you will need to reconfigure most of your settings using the user interface.  The only settings that are carried over and still configured via the configuration file are the ones related to `Observability` and `Api`.

## Open Api

To access the Open API spec  for P2G you will need to expose the below port on the Api docker container.  The open API spec will be available at `http://localhost:8001/swagger`.

```yaml
version: "3.9"

services:
  p2g-api:
    container_name: p2g-api
    image: philosowaffle/peloton-to-garmin:api-stable
    environment:
      - TZ=America/Chicago
    ports:
      - 8001:8080 # to access the api or swagger docs
    volumes:
      - ./api.local.json:/app/configuration.local.json
      - ./data:/app/data
      - ./output:/app/output
  
  p2g-webui:
    container_name: p2g-webui
    image: philosowaffle/peloton-to-garmin:webui-stable
    ports:
      - 8002:8080
    environment:
      - TZ=America/Chicago
    volumes:
      - ./webui.local.json:/app/configuration.local.json
    depends_on:
      - p2g-api
```
