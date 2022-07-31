---
layout: default
title: Docker - Headless
parent: Install
nav_order: 2
---

# Docker Headless

This the original flavor of P2G. It runs without any user interface and relies on configuration from `configuration.local.json` file.

```
docker run  -v /full/path/to/configuration.local.json:/app/configuration.local.json -v /full/path/to/output:/app/output philosowaffle/peloton-to-garmin:stable
```

## docker-compose

A sample [docker-compose.yaml](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/docker-compose.yaml) file and [configuration.local.json](https://github.com/philosowaffle/peloton-to-garmin/blob/master/configuration.example.json) can be found in the project repo.

The Docker container expects a valid `configuration.local.json` file is mounted into the container. You can learn more about the configuration file over in the [Configuration Section]({{ site.baseurl }}{% link configuration/index.md %})

```yaml
version: "3.9"
services:
  p2g:
    container_name: p2g
    image: philosowaffle/peloton-to-garmin:stable
    environment:
      - TZ=America/Chicago
    volumes:
      - ./configuration.local.json:/app/configuration.local.json
      - ./data:/app/data
```

The generated `tcx`, `fit`, `json`, and log files can be found in `app/output`.  which can be mounted as seen below.

```yaml
version: "3.9"
services:
  p2g:
    container_name: p2g
    image: philosowaffle/peloton-to-garmin:stable
    environment:
      - TZ=America/Chicago
    volumes:
      - ./configuration.local.json:/app/configuration.local.json
      - ./data:/app/data
      - ./output:/app/output
```

## Prometheus

If you configure P2G to server Prometheus metrics then you will also need to map the corresponding port for your docker container. By default, Prometheus metrics will be served on port `4000`. You can learn more about P2G and Prometheus in the [Observability Configuration]({{ site.baseurl }}{% link configuration/index.md %}) section.

```yaml
version: "3.9"
services:
  p2g:
    container_name: p2g
    image: philosowaffle/peloton-to-garmin:stable
    environment:
      - TZ=America/Chicago
    ports:
        - 4000:4000
    volumes:
      - ./configuration.local.json:/app/configuration.local.json
```
