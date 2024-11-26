
# Observability File Configuration

!!! tip

    These are advanced settings for those who like to play around with Logs, Metrics, and Traces.

## Overview

P2G supports publishing Open Telemetry metrics. P2G publishes the following:

1. Logs via Serilog. Logs can be sunk to a variety of sources including Console, File, ElasticSearch, and Grafana Loki.
1. Metrics via Prometheus. Metrics are exposed on a standard `/metrics` endpoint and the port is configurable.
1. Traces via Jaeger. Traces can be collected via an agent of your choice. Some options include Jaeger Agent/Jaeger Query, or Grafana Tempo.
1. P2G also provides a sample Grafana dashboard which can be found [in the repository](https://github.com/philosowaffle/peloton-to-garmin/tree/master/grafana).

The grafana dashboard assumes you have the following datasources setup but can be easily modified to meet your needs:

1. Prometheus
1. Loki
1. If running as a docker image a docker metrics exporter

![Grafana Dashboard](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/grafana_dashboard.png?raw=true "Grafana Dashboard")

## Observability Config

P2G looks for a file named `configuration.local.json` in the same directory where it is run.  Within this file, P2G supports configuring an `Observability` section, as seen below.

```json
"Observability": {

    "Prometheus": { /**(1)!**/ }, // Metrics
    "Jaeger": { /**(2)!**/ }, // Traces
    "Serilog": { /**(3)!**/ } // Logs
  }
```

1. Jump to [Prometheus Config Documentation](#prometheus-config)
2. Jump to [Jaeger Config Documentation](#jaeger-config)
3. Jump to [Serilog Config Documentation](#serilog-config)

### Prometheus Config

```json
"Prometheus": {
      "Enabled": false,
      "Port": 4000
    }
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Enabled | no | `false` | Whether or not to expose metrics. Metrics will be available at `http://localhost:{port}/metrics` |
| Port | no | `80` | The port the metrics endpoint should be served on. Only valid for Console mode, not Api/WebUI |

!!! tip
    If you are using Docker, ensure you have exposed the port from your container.

#### Example Prometheus scraper config

```yaml
- job_name: 'p2g'
    scrape_interval: 60s
    static_configs:
      - targets: [<p2gIPaddress>:<p2gPort>]
    tls_config:
      insecure_skip_verify: true
```

### Jaeger Config

```json
"Jaeger": {
      "Enabled": false,
      "AgentHost": "localhost",
      "AgentPort": 6831
    }
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Enabled | no | `false` | Whether or not to generate traces. |
| AgentHost | **yes - if Enalbed=true** | `null` | The host address for your trace collector. |
| AgentPort | **yes - if Enabled=true** | `null` | The port for your trace collector. |

### Serilog Config

```json
"Serilog": {
      "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Grafana.Loki" ],
      "MinimumLevel": {
        "Default": "Information",
        "Override": {
          "Microsoft": "Error",
          "System": "Error"
        }
      },
      "WriteTo": [
        { "Name": "Console" },
        {
          "Name": "File",
          "Args": {
            "path": "./output/log.txt",
            "rollingInterval": "Day",
            "retainedFileCountLimit": 7
          }
        },
        {
          "Name": "GrafanaLoki",
          "Args": {
            "uri": "http://192.168.1.95:3100",
            "textFormatter": "Serilog.Sinks.Grafana.Loki.LokiJsonTextFormatter, Serilog.Sinks.Grafana.Loki",
            "labels": [
              {
                "key": "app",
                "value": "p2g"
              }
            ]
          }
        }]
}
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Using | no | `null` | A list of sinks you would like use. The valid sinks are listed in the examplea above. |
| MinimumLevel | no | `null` | The minimum level to write. `[Verbose, Debug, Information, Warning, Error, Fatal]` |
| WriteTo | no | `null` | Additional config for various sinks you are writing to. |

More detailed information about configuring Logging can be found on the [Serilog Config Repo](https://github.com/serilog/serilog-settings-configuration#serilogsettingsconfiguration--).