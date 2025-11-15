# Peloton Settings

The Peloton Settings provide settings related to how P2G should fetch workouts from Peloton.

## Settings location

| Run Method | Location |
|------------|----------|
| Web UI     |  UI > Settings > Peloton Tab  |
| Windows Exe | UI > Settings > Peloton Tab |
| GitHubAction | Config Section in Workflow |
| Headless (Docker or Console) | Config section in `configuration.local.json` |

## File Configuration

```json
"Peloton": {
    "Email": "peloton@gmail.com",
    "Password": "peloton",
    "SessionId": "adsfd",
    "NumWorkoutsToDownload": 1,
    "ExcludeWorkoutTypes": [ "meditation" ]
  }
```

!!! warning
    Console or Docker Headless: Your username and password for Peloton and Garmin Connect are stored in clear text, which **is not secure**. Please be aware of the risks.
!!! success "WebUI version 3.3.0+: Credentials are stored **encrypted**."
!!! success "Windows Exe version 4.0.0+: Credentials are stored **encrypted**."
!!! success "GitHub Actions: Credentials are stored **encrypted**."

## Settings Overview

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Email | **yes** | `null` | Your Peloton email used to sign in |
| Password | **yes** | `null` | Your Peloton password used to sign in. **Note: Does not support `\` character in password** |
| SessionId | **no** | `null` | Your Peloton sessionId [Read more...](#peloton-session-id) |
| BearerToken | **no** | `null` | Your Peloton API Bearer Token (excluding the word `Bearer`) [Read more...](#peloton-bearer-token) |
| NumWorkoutsToDownload | no | 5 | The default number of workouts to download. See [choosing number of workouts to download](#choosing-number-of-workouts-to-download).  Set this to `0` if you would like P2G to prompt you each time for a number to download. |
| ExcludeWorkoutTypes | no | none | An array of workout types that you do not want P2G to download/convert/upload. [Read more...](#exclude-workout-types) |

## Choosing Number of Workouts To Download

When choosing the number of workouts P2G should download each polling cycle its important to keep your configured [Polling Interval](app.md) in mind. If, for example, your polling interval is set to hourly, then you may want to set `NumWorkoutsToDownload` to 4 or greater. This ensures if you did four 15min workouts during that hour they would all be captured.

Garmin is capable of rejecting duplicate workouts, so it is safe for P2G to attempt to sync a workout that may have been previously synced.

## Exclude Workout Types

If there are [Exercise Types](exercise-types.md) that you do not want P2G to sync, then you can specify those in the settings.

Some example use cases include:

1. You take a wide variety of Peloton classes, including meditation and you want to skip uploading meditation classes.
1. You want to avoid double-counting activities you already track directly on a Garmin device, such as outdoor running workouts.

The list of valid values are any [Exercise Type](exercise-types.md).

## Peloton Session Id

In the event P2G is not able to authenticate with Peloton, this configuration field can be used as a fallback option.

By visiting the website, and logging in, you can grab your `peloton_session_id` out of the saved cookie.

Saving the session id in the config file and restarting P2G will cause P2G to use that token for authentication, bypassing the need to "login".

You will need to manually update this token every time it expires.  In order to stop using the token, simply delete `"SessionId": "..."` from your config file and restart P2G.

!!! note 
    Windows users, your config file is found in `<Your P2G Folder>/data/SettingsDb.json`.  Quit P2G, edit this file, then start P2G again.

!!! danger 
    SessionId is like a password and should never be shared.
    Github action users should set SessionId as a secret similar to how you configure Email and Password.

!!! warning 
    TODO: Better instructions and the ability to edit this from UI

## Peloton Bearer Token

In the event P2G is not able to authenticate with Peloton, this configuration field can be used as a fallback option.  If you use this option, you DO NOT need to set `Session Id`, consider this mutually exclusive with `Session Id` authentication.

By visiting the website, and logging in, you can find your Bearer token by inspecting the Network traffic.  Quick, not great instructions on how to do that:

1. Got to Peloton and login
2. Hit F12 on your keyboard, this should open the Developer Console
3. Look for a Network tab
4. Refresh the web page, this should cause new traffic to populate in the Network tab
5. Look for any item that has domain `api.peloton.com` and single click it
6. This will open a details pane for that request
7. Look for the `Headers` tab in the details pane, then look for a Header called `Authorization: Bearer aIdlkjf....`
8. Copy all of the text AFTER the word `Bearer`
9. Save that copied text into your P2G config

Saving the Bearer Token in the config file and restarting P2G will cause P2G to use that token for authentication, bypassing the need to "login".

You will need to manually update this token every time it expires.  In order to stop using the token, simply delete `"BearerToken": "..."` from your config file and restart P2G.

!!! note 
    Windows users, your config file is found in `<Your P2G Folder>/data/SettingsDb.json`.  Quit P2G, edit this file, then start P2G again.
    
!!! danger 
    BearerToken is like a password and should never be shared.
    Github action users should set BearerToken as a secret similar to how you configure Email and Password.

!!! warning 
    TODO: Better instructions and the ability to edit this from UI