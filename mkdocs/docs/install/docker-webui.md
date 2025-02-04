
# Docker - Web UI

P2G provides a website user interface. Some key features include:

1. Configure your settings via a user interface
1. Trigger a sync from any browser (your computer, your phone, etc.)
1. Sync service can still run in the background, syncing periodically
1. OpenApi for custom scripts and workflows

![P2G UI Demo](../img/p2g_demo.gif "P2G UI Demo")

## docker-compose

*Pre-requisite:* You have either `docker-compose` or `Docker Desktop` installed

1. Create a folder named `p2g-webui`
    1. Inside this folder create [docker-compose.yaml](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/docker-compose-ui.yaml)
    1. Within this same directory, also create a folder called `config`
        1. Create two more folders within the `config` directory: `api` and `webui`
        1. Within the `api` folder, create [configuration.local.json](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/config/api/configuration.local.json)
        1. Within the `webui` folder, also create a [configuration.local.json](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui/config/webui/configuration.local.json) with slightly different content
        1. Your final directory structure should look similar to [this](https://github.com/philosowaffle/peloton-to-garmin/blob/master/docker/webui).
1. Open a terminal in the `p2g-webui` folder
1. Run: `docker-compose pull && docker-compose up -d`
    1. This will pull the containers and start them up running in the background
    1. You can close the terminal at this time
1. Open a browser and navigate to `http://localhost:8002`

Any logs or generated files will be available in the `output` directory.  Additionally, you can learn more about customizing your configuration over in the [Configuration Section](../configuration/index.md)

### To stop P2G

1. You can use Docker Desktop application to kill the containers
1. Or, you can open a terminal in the `p2g-webui` folder
    1. Run: `docker-compose down`

### To update P2G

1. Open a terminal in the `p2g-webui` folder
    1. Run: `docker-compose pull && docker-compose up -d`

## Configuration

If you are migrating to the Web UI for the first time you will need to reconfigure most of your settings using the user interface.  The only settings that are carried over and still configured via the configuration file are the ones related to `Observability`.

## Open Api

To access the Open API spec  for P2G you will need to expose port `8080` on the Api docker container.  The open API spec will be available at `http://localhost:8001/swagger`.
