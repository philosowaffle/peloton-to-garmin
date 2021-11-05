---
layout: default
title: Experimental
nav_order: 6
---

# Experimental

New enhancements not quite ready for the prime time can be found here.

## Web UI

Still in the very early alpha stages, the docker image now includes an optional web ui.  To enable this feature, use the `web` command when launching the docker container then visit `http://localhost:80` in your browser.

```yaml
version: "3.9"

services:
  p2g:
    container_name: p2g
    image: philosowaffle/peloton-to-garmin
    command: "web"
    ports:
      - 80:80
      - 443:443
    environment:
      - TZ=America/Chicago
    volumes:
      - ./configuration.local.json:/app/configuration.local.json
      - ./output:/app/output
      - ./working:/app/working
```

### Polling Service

If configured, the polling service will run as normal in the background even when the Web UI is enabled.  At this time the Web UI just provides more insight into when the next sync is scheduled for and allows you to kick off an ad hoc sync.

### OpenApi

The web version also exposes an API that can be interacted with. You can find documentation for the api at `http://localhost/swagger`.  Expect breaking changes will be made to this api until this reaches a more final release state.