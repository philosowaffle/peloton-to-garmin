
# Docker Headless

This flavor of P2G runs without any user interface and relies on configuration from `configuration.local.json` file.

!!! info "[DockerHub](https://hub.docker.com/r/philosowaffle/peloton-to-garmin)"

    ```bash
    docker run -i -v /full/path/to/configuration.local.json:/app/configuration.local.json -v /full/path/to/output:/app/output philosowaffle/peloton-to-garmin:stable
    ```

!!! info "[GitHub Package](https://github.com/philosowaffle/peloton-to-garmin/pkgs/container/peloton-to-garmin)"

    ```bash
    docker run -i -v /full/path/to/configuration.local.json:/app/configuration.local.json -v /full/path/to/output:/app/output ghcr.io/philosowaffle/peloton-to-garmin:stable
    ```

## docker-compose

*Pre-requisite:* You have either `docker-compose` or `Docker Desktop` installed
*This method does not work with Garmin accounts protected by Two Step Verification*

1. Create a directory `p2g-headless`
    1. Inside this folder create a [docker-compose.yaml](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/headless/docker-compose.yaml) file in the directory
    1. Within this same directory, also create a folder called `config`
        1. Inside the `config` folder create the [configuration.local.json](https://github.com/philosowaffle/peloton-to-garmin/blob/master/configuration.example.json).
    1. Edit the configuration file to use your Peloton and Garmin credentials
1. Open a terminal in this folder
1. Run: `docker-compose pull && docker-compose up -d`

Any logs or generated files will be available in the `output` directory.  Additionally, you can learn more about customizing your configuration over in the [Configuration Section](../configuration/index.md)

### To stop P2G

1. You can use Docker Desktop application to kill the containers
1. Or, you can open a terminal in the `p2g-headless` folder
    1. Run: `docker-compose down`

### To update P2G

1. Open a terminal in the `p2g-headless` folder
    1. Run: `docker-compose pull && docker-compose up -d`

## Prometheus

If you configure P2G to serve Prometheus metrics then you will also need to map the corresponding port for your docker container. By default, Prometheus metrics will be served on port `4000`. You can learn more about P2G and Prometheus in the [Observability Configuration](../configuration/index.md) section.
