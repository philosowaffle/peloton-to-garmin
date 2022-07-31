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

With the below docker configuration you can launch the containers and visit `http://localhost:8002` to reach the `p2g` web UI.  A sample `docker-compose.yaml` and config files can also be found [`in the project repo`](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/). 

```yaml
version: "3.9"

services:
  p2g-api:
    container_name: p2g-api
    image: philosowaffle/peloton-to-garmin:api-stable
    environment:
      - TZ=America/Chicago
    volumes:
      - ./api.local.json:/app/configuration.local.json
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
      - ./webui.local.json:/app/configuration.local.json
    depends_on:
      - p2g-api
```

## Configuration

If you are migrating to the Web UI for the first time you will need to reconfigure most of your settings using the user interface.  The only settings that are carried over and still configured via the configuration file are the ones related to `Observability` and `Api`.

You can find examples of these config files [in the project repo](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/).

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
      - 8001:80
    volumes:
      - ./api.local.json:/app/configuration.local.json
      - ./data:/app/data
      - ./output:/app/output
  
  p2g-webui:
    container_name: p2g-webui
    image: philosowaffle/peloton-to-garmin:webui-stable
    ports:
      - 8002:80
    environment:
      - TZ=America/Chicago
    volumes:
      - ./webui.local.json:/app/configuration.local.json
    depends_on:
      - p2g-api
```