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
    "FormatToUpload": "fit",
    "api": {
      "ssoSignInUrl": "https://sso.garmin.com/sso/signin",
      "ssoEmbedUrl": "https://sso.garmin.com/sso/embed",
      "ssoMfaCodeUrl": "https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode",
      "ssoUserAgent": "GCM-iOS-5.7.2.1",
      "oAuth1TokenUrl": "https://connectapi.garmin.com/oauth-service/oauth/preauthorized",
      "oAuth1LoginUrlParam": "https://sso.garmin.com/sso/embed&accepts-mfa-tokens=true",
      "oAuth2RequestUrl": "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0",
      "uploadActivityUrl": "https://connectapi.garmin.com/upload-service/upload",
      "uploadActivityUserAgent": "GCM-iOS-5.7.2.1",
      "uplaodActivityNkHeader": "NT",
      "origin": "https://sso.garmin.com",
      "referer": "https://sso.garmin.com/sso/signin"
    }
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
| Api | no | See sample above | `Garmin Tab > Advanced > Garmin Api Settings` | Configures how P2G communicates with the Garmin Api. **Do not modify unless told to do so** |
