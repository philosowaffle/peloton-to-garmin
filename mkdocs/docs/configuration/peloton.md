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

## Choosing Number of Workouts To Download

When choosing the number of workouts P2G should download each polling cycle its important to keep your configured [Polling Interval](app.md) in mind. If, for example, your polling interval is set to hourly, then you may want to set `NumWorkoutsToDownload` to 4 or greater. This ensures if you did four 15min workouts during that hour they would all be captured.

Garmin is capable of rejecting duplicate workouts, so it is safe for P2G to attempt to sync a workout that may have been previously synced.

## Exclude Workout Types

If there are [Exercise Types](exercise-types.md) that you do not want P2G to sync, then you can specify those in the settings.

Some example use cases include:

1. You take a wide variety of Peloton classes, including meditation and you want to skip uploading meditation classes.
1. You want to avoid double-counting activities you already track directly on a Garmin device, such as outdoor running workouts.

The list of valid values are any [Exercise Type](exercise-types.md).
