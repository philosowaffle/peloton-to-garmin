# API File Configuration

!!! tip

    These settings only apply if you are running an Instance of the API.  P2G provides some [recommended config files](https://github.com/philosowaffle/peloton-to-garmin/tree/master/docker/webui) to get you started.

Some lower level configuration cannot be provided via the web user interface and can only be provided by config file.

The Api looks for a file named `configuration.local.json` in the same directory where it is run.  Below is an example of the structure of this config file.

```json
{
  "Api": { /** (1)! **/ }, 
  "Observability": { /** (2)! **/ }
}
```

1. Jump to [Api Config Documentation](#api-config)
2. Go to [Observability Config Documentation](observability.md#observability-config)

## Api Config

!!! warning
    Most people should not need to change this setting.

```json
  "Api": {
        "HostUrl": "http://*:8080"
      }
```

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| HostUrl | no | `http://localhost:8080` | none | The host and port the WebUI should bind to and listen on. |