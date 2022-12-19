---
layout: default
title: Docker
parent: Install
nav_order: 0
---

# Docker

The recommended installation method is with Docker. If you're not familiar with Docker but would like to try it check out the [quick start guide](#quick-start-guide).

P2G offers two main flavors of docker images:

1. [Docker Web UI]({{ site.baseurl }}{% link install/docker-webui.md %})
1. [Docker Headless]({{ site.baseurl }}{% link install/docker-headless.md %})

## Image Repositories

P2G publishes Docker images to both [DockerHub](https://hub.docker.com/r/philosowaffle/peloton-to-garmin) and [GitHub Package](https://github.com/philosowaffle/peloton-to-garmin/pkgs/container/peloton-to-garmin).

## Tags

The following tags are provided:

### Image flavors

1. `stable` / `latest` - By default the base tag points to the headless version of P2G
1. `api-stable` / `api-latest` - Used in conjunction with the `webui` image, provides the API and server for P2G user interface
1. `webui-stable` / `webui-latest` - Used in conjunction with the `api` image, provides a P2G web user interface

### Tag versioning

1. `stable` - Always points to the latest release
1. `v{X}` / `v3` / `v4` - Always points to the latest of the current major version
1. `latest` - The bleeding edge of the master branch, breaking changes may happen
1. `vX.Y.Z` - For using a specific released version

## Docker User

The P2G images run the process under the user and group `p2g:p2g` with uid and gid `1015:1015`.  To access files created by `p2g`:

1. Create a group on the local machine `p2g` with group id `1015`
1. Add your user on the local machine to the `p2g` group

## Quick Start Guide

Docker provides an easy and consistent way to install, update, and uninstall applications across multiple Operating Systems.  Docker is extremely popular in the self-hosted community, a group interested in minimizing dependencies on Cloud providers in favor of attempting to keep their data local, private, and free.  You can learn more about the ever growing list of self-hosted applications on the [awesome-selfhosted list](https://github.com/awesome-selfhosted/awesome-selfhosted).

To learn more about Docker head on over to their [website](https://www.docker.com/resources/what-container/).

### Mac / Windows Docker Quick Start

1. Download and install Docker Desktop, this will give you all the tools you need and a handy UI for managing docker containers
    1. [Install Mac Docker Desktop](https://docs.docker.com/desktop/install/mac-install/)
    1. [Install Windows Docker Desktop](https://docs.docker.com/desktop/install/windows-install/)
1. Follow the remaining instructions [here]({{ site.baseurl }}{% link install/docker-webui.md %}#docker-compose)
