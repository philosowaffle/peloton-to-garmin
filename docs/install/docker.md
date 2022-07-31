---
layout: default
title: Docker
parent: Install
nav_order: 0
---

# Docker

The recommended and easiest way to get started is with Docker. To learn more about Docker head on over to their [website](https://www.docker.com/).

P2G offers two main flavors of docker images:

1. [Docker Web UI]({{ site.baseurl }}{% link install/docker-webui.md %})
1. [Docker Headless]({{ site.baseurl }}{% link install/docker-headless.md %})

## Tags

The P2G docker images are available on [DockerHub](https://hub.docker.com/r/philosowaffle/peloton-to-garmin). The following tags are provided:

### Image flavors

1. `stable` / `latest` - By default the base tag points to the headless version of P2G
1. `api-stable` / `api-latest` - Used in conjunction with the `webui` image, provides the API and server for P2G user interface
1. `webui-stable` / `webui-latest` - Used in conjunction with the `api` image, provides a P2G web user interface

### Tag versioning

1. `stable` - Always points to the latest release
1. `latest` - The bleeding edge of the master branch, breaking changes may happen
1. `vX.Y.Z` - For using a specific released version

## Docker User

The P2G images run the process under the user and group `p2g:p2g` with uid and gid `1015:1015`.  To access files created by `p2g`:

1. Create a group on the local machine `p2g` with group id `1015`
1. Add your user on the local machine to the `p2g` group