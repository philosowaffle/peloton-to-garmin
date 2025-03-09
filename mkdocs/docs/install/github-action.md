# Github Actions

A Github Actions workflow exists that can be used to automatically sync your rides on a schedule (by default once a day).

## ⬇️ Install

The following steps will walk you through:

1. Creating an account on [GitHub](https://github.com)
1. Creating a personal copy of the [P2G repo](https://github.com/philosowaffle/peloton-to-garmin)
1. Configuring your personal P2G copy
1. Setting up the P2G Github Action to run on a scehdule or on demand

### 1. Fork the Repository

The easiest way to use the action is to simply create a fork of the repo using the `Fork` button.

1. Create an account on [GitHub](https://github.com)
1. Navigate to the [P2G repo](https://github.com/philosowaffle/peloton-to-garmin)
1. In the upper right hand corner, click the button that says `Fork`
1. Make note of the url to your forked copy
1. Continue on to the [Secrets instructions](#secrets) below

### 2. Setup Secrets

Once you've created your fork, you'll need to set a number of secrets in your repository.

1. From your forked copy of P2G, click the `Settings`
1. Then, on the side nav, select `Secrets` and then `Actions`.
1. Continue on to the [Action Permissions](#action-permissions) instructions below, then come back here

From this point on you can add secrets by clicking the `New repository secret` button at the top right.

| Secret Name             | Value                                                                |
|-------------------------|----------------------------------------------------------------------|
| `P2G_PELOTON__EMAIL`    | The email address you use to login to Peloton                        |
| `P2G_PELOTON__PASSWORD` | The password that you use to login to Peloton                        |
| `P2G_GARMIN__EMAIL`     | The email address that you use to login to Garmin                    |
| `P2G_GARMIN__PASSWORD`  | The password that you use to log into Garmin                         |

#### Action Permissions

1. From your forked copy of P2G, click the `Settings` tab
1. Navigate to the settings for `Actions > General`
1. Under `Action Permissions` choose the 4th radio button titled: "Allow <youruser>, and select non-<youruser>, actions and reusable workflows".
1. Under the same radio button, check the checkbox to "Allow actions created by GitHub"

### 3. Running the Action

Once you've configured your secrets, you can then navigate to the `Actions` tab within your repository.

1. Enable actions for your fork.
1. You can now run the `Sync workflow` manually

To have tha action run on a schedule set the `cron` line in the `./github/workflows/sync_peloton_to_garmin.yml` file.

### 4. Configuration

Nearly all of your configuration is done in this file `./github/workflows/sync_peloton_to_garmin.yml`.  The only exception is for `Secret` data which is configured using the [Secrets](#2-setup-secrets) feature of GitHub.

You can learn more about customizing your configuration over in the [Configuration Section](../configuration/index.md).

## ⬆️ Updating

1. Make a copy of your current configuration in `.github/workflows/sync_peloton_to_garmin.yml` as you will need to reapply these changes after updating.
1. From the home page of your forked repository, there should be a button to `Sync fork`, click this to pull in the latest changes from the original repo.
1. Go back to your `.github/workflows/sync_peloton_to_garmin.yml` and re-apply any changes from step 1.

## ❌ Uninstalling

1. From your fokred copy of P2G, click the `Settings` tab
1. Under `General` scoll all the way to the bottom of the page to the `Danger Zone` section
1. Click `Delete this repository` and follow the guided steps

!!! danger
    This is non-recoverable.  Any and all customizations will be lost.  You can re-install P2G again by starting over.

## #️⃣ Changing Versions

1. In your forked copy of P2G navigate to `./github/workflows/sync_peloton_to_garmin.yml`
1. Find this [line](https://github.com/philosowaffle/peloton-to-garmin/blob/master/.github/workflows/sync_peloton_to_garmin.yml#L23)
1. The identifier `console-stable` can be edited to any of the [supported tags](docker.md#version-tags)

!!! warning

    Attempting to use configuration or data from a later version of P2G with an older version is not guaranteed to work. You may need to reconfigure your instance.

## 👪 Multiple Users

Have each person follow the install steps here.

## 🚧 Limitations

Due to technical limitations, Garmin accounts protected by MFA/2FA are not supported with this install method.