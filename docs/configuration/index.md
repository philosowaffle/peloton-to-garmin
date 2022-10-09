---
layout: default
title: Configuration
nav_order: 3
has_children: true
---

# Configuration

P2G supports configuration via [command line arguments]({{ site.baseurl }}{% link configuration/command-line.md %}), [environment variables]({{ site.baseurl }}{% link configuration/environment-variables.md %}), [json config file]({{ site.baseurl }}{% link configuration/json.md %}), and via the user interface. By default, P2G looks for a file named `configuration.local.json` in the same directory where it is run.

## Example working configs

1. [Headless config](https://github.com/philosowaffle/peloton-to-garmin/blob/master/configuration.example.json)
1. [WebUI configs](https://github.com/philosowaffle/peloton-to-garmin/tree/master/docker/webui)

## Config Precedence

The following defines the precedence in which config definitions are honored. With the first item overriding any below it.

1. Command Line
1. Environment Variables
1. Config File

For example, if you defined your Peloton credentials ONLY in the Config file, then the Config file credentials will be used.

If you defined your credentials in both the Config file AND the Environment variables, then the Environment variable credentials will be used.

If you defined credentials using all 3 methods (config file, env, and command line), then the credentials provided via the command line will be used.
