---
layout: default
title: Observability
nav_order: 5
---

# Observability

P2G supports publishing Open Telemetry metrics. These metrics can be setup and configured in the [Observability config section]({{ site.baseurl }}{% link configuration/json.md %}#observability-config). P2G publishes the following:

1. Logs via Serilog. Logs can be sunk to a variety of sources including Console, File, ElasticSearch, and Grafana Loki.
1. Metrics via Prometheus. Metrics are exposed on a standard `/metrics` endpoint and the port is configurable.
1. Traces via Jaeger. Traces can be collected via an agent of your choice. Some options include Jaeger Agent/Jaeger Query, or Grafana Tempo.
1. P2G also provides a sample Grafana dashboard which can be found [in the repository](https://github.com/philosowaffle/peloton-to-garmin/tree/master/grafana).

The grafana dashboard assumes you have the following datasources setup but can be easily modified to meet your needs:

1. Prometheus
1. Loki
1. If running as a docker image a docker metrics exporter

![Grafana Dashboard](https://github.com/philosowaffle/peloton-to-garmin/raw/master/images/grafana_dashboard.png?raw=true "Grafana Dashboard")
