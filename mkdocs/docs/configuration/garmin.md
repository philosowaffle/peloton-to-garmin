# Garmin Settngs

This Garmin Settings provide settings related to uploading workouts to Garmin.

## Settings location

| Run Method | Location |
|------------|----------|
| Web UI     |  UI > Settings > Garmin Tab  |
| Windows Exe | UI > Settings > Garmin Tab |
| GitHubAction | Config Section in Workflow |
| Headless (Docker or Console) | Config section in `configuration.local.json` |

## File Configuration

```json
"Garmin": {
    "Email": "garmin@gmail.com",
    "Password": "garmin",
    "TwoStepVerificationEnabled": false,
    "Upload": false,
    "FormatToUpload": "fit"
  }
```

!!! warning
    Console or Docker Headless: Your username and password for Peloton and Garmin Connect are stored in clear text, which **is not secure**. Please be aware of the risks.
!!! success "WebUI version 3.3.0+: Credentials are stored **encrypted**."
!!! success "Windows Exe version 4.0.0+: Credentials are stored **encrypted**."
!!! success "GitHub Actions: Credentials are stored **encrypted**."

## Settings Overview

| Field      | Required | Default | UI Setting Location | Description |
|:-----------|:---------|:--------|:--------------------|:------------|
| Email | **yes - if Upload=true** | `null` | `Garmin Tab` | Your Garmin email used to sign in. |
| Password | **yes - if Upload=true** | `null` | `Garmin Tab` | Your Garmin password used to sign in. **Note: Does not support `\` character in password** |
| TwoStepVerificationEnabled | no | `false` | `Garmin Tab` | Whether or not your Garmin account is protected by Two Step Verification |
| Upload | no | `false` | `Garmin Tab` |  `true` indicates you wish downloaded Peloton workouts to be uploaded to Garmin Connect. |
| FormatToUpload | no | `fit` | `Garmin Tab > Advanced` | Valid values are `fit` or `tcx`. Ensure the format you specify here is also enabled in your [Format config](format.md) |