---
layout: default
title: Docker - Headless
parent: Install
nav_order: 2
---

# Docker Headless

This the original flavor of P2G. It runs without any user interface and relies on configuration from `configuration.local.json` file.

### [DockerHub](https://hub.docker.com/r/philosowaffle/peloton-to-garmin)

```bash
docker run -v /full/path/to/configuration.local.json:/app/configuration.local.json -v /full/path/to/output:/app/output philosowaffle/peloton-to-garmin:stable
```

### [GitHub Package](https://github.com/philosowaffle/peloton-to-garmin/pkgs/container/peloton-to-garmin)

```bash
docker run -v /full/path/to/configuration.local.json:/app/configuration.local.json -v /full/path/to/output:/app/output ghcr.io/philosowaffle/peloton-to-garmin:stable
```

## docker-compose

*Pre-requisite:* You have either `docker-compose` or `Docker Desktop` installed

1. Create a directory `p2g-headless`
    1. Inside this folder create a [docker-compose.yaml](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/docker-compose.yaml) file in the directory
    1. Also create a [configuration.local.json](https://github.com/philosowaffle/peloton-to-garmin/blob/master/configuration.example.json) file in the directory.
    1. Edit the configuration file to use your Peloton and Garmin credentials
1. Open a terminal in this folder
1. Run: `docker-compose pull && docker-compose up -d`

Any logs or generated files will be available in the `output` directory.  Additionally, you can learn more about customizing your configuration over in the [Configuration Section]({{ site.baseurl }}{% link configuration/index.md %})

### To stop P2G

1. You can use Docker Desktop application to kill the containers
1. Or, you can open a terminal in the `p2g-headless` folder
    1. Run: `docker-compose down`

### To update P2G

1. Open a terminal in the `p2g-headless` folder
    1. Run: `docker-compose pull && docker-compose up -d`

## Prometheus

If you configure P2G to serve Prometheus metrics then you will also need to map the corresponding port for your docker container. By default, Prometheus metrics will be served on port `4000`. You can learn more about P2G and Prometheus in the [Observability Configuration]({{ site.baseurl }}{% link configuration/index.md %}) section.

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
      - ./output:/app/output
```
