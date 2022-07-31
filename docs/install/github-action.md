---
layout: default
title: Using Github Actions
nav_order: 4
parent: Install
has_children: false
---

# Github Actions

A Github Actions workflow exists that can be used to automatically sync your rides on a schedule (by default once a day)

## Getting started

The easiest way to use the action is to simply create a fork of the repo using the `Fork` button. 

## Secrets

Once you've created your fork, you'll need to set a number of secrets in your repository.

You can set secrets by clicking the `Settings` tab in your fork. Then, on the side nav, select `Secrets` and then `Actions`.

From this point on you can add secrets by clicking the `New repository secret` button at the top right.

| Secret Name             | Value                                                                |
|-------------------------|----------------------------------------------------------------------|
| `P2G_PELOTON__EMAIL`    | The email address you use to login to Peloton                        |
| `P2G_PELOTON__PASSWORD` | The password that you use to login to Peloton                        |
| `P2G_GARMIN__EMAIL`     | The email address that you use to login to Garmin                    |
| `P2G_GARMIN__PASSWORD`  |The password that you use to log into Garmin                          |
| `DEVICE_INFO`           | The contents of the deviceInfo.xml that you want to use for the sync |

## Starting the workflow

Once you've configured your secrets, you can then navigate to the `Actions` tab within your repository. Enable actions for your fork. You can now run the `Sync workflow` manually, or have it automatically run on the schedule set by the `cron` line in the `./github/workflows/sync_peloton_to_garmin.yml` file. 

## Additional Notes

If you're doing more than 5 activities a day, you will need to change the default number of workouts downloaded as part of the workflow.
