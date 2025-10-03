# Web UI File Configuration

!!! tip

    These settings only apply if you are running an Instance of the Web UI.  P2G provides some [recommended config files](https://github.com/philosowaffle/peloton-to-garmin/tree/master/docker/webui) to get you started.

Some lower level configuration cannot be provided via the web user interface and can only be provided by config file.

The Web UI looks for a file named `configuration.local.json` in the same directory where it is run.  Below is an example of the structure of this config file.

```json linenums="1"
{
  "Api": { /** (1)! **/ }, 
  "WebUI": { /** (2)! **/ },
  "Observability": { /** (3)! **/ }
}
```

1. Jump to [Api Config Documentation](#api-config)
2. Jump to [Web UI Config Documentation](#web-ui-config)
3. Go to [Observability Config Documentation](observability.md#observability-config)

## Api Config

This section helps inform the Web UI where to find the P2G Api.

```json
 "Api": {
      "HostUrl": "http://p2g-api:8080"
    }
```

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| HostUrl | yes | `null` | none | The host and port for the Web UI to communicate with the Api. |

## Web UI Config

!!! warning
    Optional - most users should not need to change this setting.

```json
 "WebUI": {
      "HostUrl": "http://*:8080"
    }
```

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| HostUrl | no | `http://localhost:8080` | none | The host and port the WebUI should bind to and listen on. |