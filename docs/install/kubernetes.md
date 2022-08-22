---
layout: default
title: Kubernetes
parent: Install
nav_order: 2
---

# Kubernetes

It is also possible to run P2G in Kubernets via [Helm](https://helm.sh/). By default P2G is conmfigured with the default values from the [configuration.local.json file](helm/peloton-to-garmin/values.yaml).

In addition it is installed as a [Kubernetes Cronjob](https://kubernetes.io/docs/concepts/workloads/controllers/cron-jobs/) so it will run automatically on the scheduled interval (i.e. by default every 6 hours)

To provide your own values create an overrides_value.yaml file and provide it when installing the chart.

For example an overrides-value.yaml file might look like:
```
app:
  enablePolling: false
peloton:
  email: "fill_me_in@domain.com"
  password: "fill_me_in"
  numWorkoutsToDownload: 5
  excludeWorkoutTypes: []
garmin:
  email: "fill_me_in@domain.com"
  password: "fill_me_in"
  upload: true
config:
  # Every day at 3pm
  schedule: "0 15 * * *"
```

And installed with:
```
cd ./helm
helm install peloton-to-garmin ./peloton-to-garmin --values override.values.yaml
```
