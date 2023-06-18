---
layout: default
title: Using Github Actions
nav_order: 4
parent: Install
has_children: false
---

# Github Actions

A Github Actions workflow exists that can be used to automatically sync your rides on a schedule (by default once a day). This option does not support Garmin accounts protected by Two Step Verification.

## Getting started

The easiest way to use the action is to simply create a fork of the repo using the `Fork` button.

1. Create an account on [GitHub](https://github.com)
1. Navigate to the [P2G repo](https://github.com/philosowaffle/peloton-to-garmin)
1. In the upper right hand corner, click the button that says `Fork`
1. Make note of the url to your forked copy
1. Continue on to the [Secrets instructions](#secrets) below

## Secrets

Once you've created your fork, you'll need to set a number of secrets in your repository.

1. From your forked copy of P2G, click the `Settings`
1. Then, on the side nav, select `Secrets` and then `Actions`.

From this point on you can add secrets by clicking the `New repository secret` button at the top right.

| Secret Name             | Value                                                                |
|-------------------------|----------------------------------------------------------------------|
| `P2G_PELOTON__EMAIL`    | The email address you use to login to Peloton                        |
| `P2G_PELOTON__PASSWORD` | The password that you use to login to Peloton                        |
| `P2G_GARMIN__EMAIL`     | The email address that you use to login to Garmin                    |
| `P2G_GARMIN__PASSWORD`  | The password that you use to log into Garmin                          |
| `DEVICE_INFO`           | The contents of the deviceInfo.xml that you want to use for the sync |

## Starting the workflow

Once you've configured your secrets, you can then navigate to the `Actions` tab within your repository. 

1. Enable actions for your fork. 
1. You can now run the `Sync workflow` manually

To have tha action run on a schedule set the `cron` line in the `./github/workflows/sync_peloton_to_garmin.yml` file.

## Configuration

If you're doing more than 5 activities a day, you will need to change the default number of workouts downloaded as part of the workflow. This is configured in the `./github/workflows/sync_peloton_to_garmin.yml` file.

## Updating

1. From the home page of your forked repository, there should be a button to `Sync fork`, click this to pull in the latest changes from the original repo