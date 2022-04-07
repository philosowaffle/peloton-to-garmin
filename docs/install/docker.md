---
layout: default
title: Docker
parent: Install
nav_order: 0
---

# Docker

The recommended and easiest way to get started is with Docker. To learn more about Docker head on over to their [website](https://www.docker.com/).

```bash
docker run philosowaffle/peloton-to-garmin:stable -v ./configuration.local.json:/app/configuration.local.json -v ./output:/app/output
```

## Docker Tags

The P2G docker image is available on [DockerHub](https://hub.docker.com/r/philosowaffle/peloton-to-garmin). The following tags are provided:

1. `stable` - Always points to the latest release
1. `latest` - The bleeding edge of the master branch, breaking changes may happen
1. `vX.Y.Z` - For using a specific released version

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
```

The generated `tcx`, `fit`, `json`, and log files can be found in `app/output` which can be mounted as seen below.

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
      - ./output:/app/output
```

## Docker User

The P2G images run the process under the user and group `p2g:p2g`.

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
