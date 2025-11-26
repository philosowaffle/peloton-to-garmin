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
| NumWorkoutsToDownload | no | 5 | The default number of workouts to download. See [choosing number of workouts to download](#choosing-number-of-workouts-to-download).  Set this to `0` if you would like P2G to prompt you each time for a number to download. |
| ExcludeWorkoutTypes | no | none | An array of workout types that you do not want P2G to download/convert/upload. [Read more...](#exclude-workout-types) |
| Advanced Config | **no** | N/A | [Read more...](#advanced-api-configuration)|

## Choosing Number of Workouts To Download

When choosing the number of workouts P2G should download each polling cycle its important to keep your configured [Polling Interval](app.md) in mind. If, for example, your polling interval is set to hourly, then you may want to set `NumWorkoutsToDownload` to 4 or greater. This ensures if you did four 15min workouts during that hour they would all be captured.

Garmin is capable of rejecting duplicate workouts, so it is safe for P2G to attempt to sync a workout that may have been previously synced.

## Exclude Workout Types

If there are [Exercise Types](exercise-types.md) that you do not want P2G to sync, then you can specify those in the settings.

Some example use cases include:

1. You take a wide variety of Peloton classes, including meditation and you want to skip uploading meditation classes.
1. You want to avoid double-counting activities you already track directly on a Garmin device, such as outdoor running workouts.

The list of valid values are any [Exercise Type](exercise-types.md).

## Advanced API Configuration

In general you should not need to modify these settings unless specifically recommended to do so.

```json

"Peloton": {
    ...Existing config options...
    "BearerToken": "",
    "Api": {
        "ApiUrl": "https://api.onepeloton.com/",
        "AuthDomain": "auth.onepeloton.com",
        "AuthClientId": "WVoJxVDdPoFx4RNewvvg6ch2mZ7bwnsM",
        "AuthAudience": "https://api.onepeloton.com/",
        "AuthScope": "offline_access openid peloton-api.members:default",
        "AuthRedirectUri": "https://members.onepeloton.com/callback",
        "Auth0ClientPayload": "eyJuYW1lIjoiYXV0aDAuanMtdWxwIiwidmVyc2lvbiI6IjkuMTQuMyJ9",
        "AuthAuthorizePath": "/authorize",
        "AuthTokenPath": "/oauth/token",
        "BearerTokenDefaultTtlSeconds": 172800,
    }
  },

```

### Peloton Bearer Token

In the event P2G is not able to authenticate with Peloton, this configuration field can be used as a fallback option.  If you set this option then P2G will bypass authenticating with Peloton and use the provided BearerToken to authenticate requests.

Quick, not great instructions on how to locate a Bearer Token:

1. Go to Peloton website and login
2. Hit F12 on your keyboard, this should open the Developer Console
3. Look for a Network tab
4. Refresh the web page, this should cause new traffic to populate in the Network tab
5. Look for any item that has domain `api.peloton.com` and single click it
6. This will open a details pane for that request
7. Look for the `Headers` tab in the details pane, then look for a Header called `Authorization: Bearer aIdlkjf....`
8. Copy all of the text AFTER the word `Bearer`
9. Save that copied text into your P2G config

Saving the Bearer Token in the config file and restarting P2G will cause P2G to use that token for authentication, bypassing the need to "login".

You will need to manually update this token every time it expires which will be roughly every 48hrs.  In order to stop using the token, simply delete `"BearerToken": "..."` from your config file and restart P2G.

!!! note 
    Windows users and Web UI users, your config file is found in `<Your P2G Folder>/data/SettingsDb.json`.  Quit P2G, edit this file, then start P2G again.
    
!!! danger 
    BearerToken is like a password and should never be shared.
    Github action users should set BearerToken as a secret similar to how you configure Email and Password.

!!! warning 
    TODO: Better instructions and the ability to edit this from UI