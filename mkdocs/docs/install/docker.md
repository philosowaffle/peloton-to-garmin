
# Docker

The recommended installation method is with Docker. If you're not familiar with Docker you can [learn more here](#more-about-docker).

P2G offers two main flavors of docker images:

| Flavor | Support Garmin 2-Step Verification | Support Automatic Syncing |
|:------------------|:-----------------------------------|:--------------------------|
| [Web UI](docker-webui.md) | yes | only when Garmin 2fa is disabled |
| [Docker Headless](docker-headless.md) | partial | only when Garmin 2fa is disabled |

!!! info "Image Repositories"

    P2G publishes Docker images to both [DockerHub](https://hub.docker.com/r/philosowaffle/peloton-to-garmin) and [GitHub Package](https://github.com/philosowaffle/peloton-to-garmin/pkgs/container/peloton-to-garmin).

## Tags

P2G ships several different flavors of containers that can be combined with a version tag:

### Image flavors

1. `console`- The headless version of P2G, simple console application
1. `api` - Used in conjunction with the `webui` image, provides the API and server for P2G user interface
1. `webui` - Used in conjunction with the `api` image, provides a P2G web user interface

### Version tags

1. `stable` - Always points to the latest release
1. `v{X}` / `v3` / `v4` - Always points to the latest of the current major version
1. `latest` - The bleeding edge of the master branch, breaking changes may happen
1. `vX.Y.Z` - For using a specific released version

### Using Flavor and Version Tag together

In the below examples, you can substitute `console` for any [Image Flavor](#image-flavors).

1. `console-latest`
1. `console-stable`
1. `console-v4`
1. `console-v3.6.0`

## Docker User

The P2G images run the process under the user and group `p2g:p2g` with uid and gid `1015:1015`.  To access files created by `p2g`:

1. Create a group on the local machine `p2g` with group id `1015`
1. Add your user on the local machine to the `p2g` group

```sh
> sudo groupadd -g 1015 p2g
> sudo usermod -aG p2g <yourUser>
```

## More about Docker

Docker provides an easy and consistent way to install, update, and uninstall applications across multiple Operating Systems.  Docker is extremely popular in the self-hosted community, a group interested in minimizing dependencies on Cloud providers in favor of attempting to keep their data local, private, and free.  You can learn more about the ever growing list of self-hosted applications on the [awesome-selfhosted list](https://github.com/awesome-selfhosted/awesome-selfhosted).

To learn more about Docker head on over to their [website](https://www.docker.com/resources/what-container/).

### Mac / Windows Docker Quick Start

1. Download and install Docker Desktop, this will give you all the tools you need and a handy UI for managing docker containers
    1. [Install Mac Docker Desktop](https://docs.docker.com/desktop/install/mac-install/)
    1. [Install Windows Docker Desktop](https://docs.docker.com/desktop/install/windows-install/)
1. Follow the remaining instructions [here](docker-webui.md#docker-compose)
