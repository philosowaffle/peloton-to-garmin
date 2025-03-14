# App Settings

The App Settings provide global settings for the P2G application.

## Settings location

| Run Method | Location |
|------------|----------|
| Web UI     |  UI > Settings > App Tab  |
| Windows Exe | UI > Settings > App Tab |
| GitHubAction | Config Section in Workflow |
| Headless (Docker or Console) | Config section in `configuration.local.json` |

## File Configuration

```json
 "App": {
    "EnablePolling": true,
    "PollingIntervalSeconds": 86400,
    "CheckForUpdates": true,
    "CloseConsoleOnFinish": false
  }
```

## Settings Overview

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| EnablePolling  | no | `true` | `true` if you wish P2G to run continuously and poll Peloton for new workouts. |
| PollingIntervalSeconds | no | 86400 | The polling interval in seconds determines how frequently P2G should check for new workouts. Be warned, that setting this to a frequency of hourly or less may get you flagged by Peloton as a bad actor and they may reset your password. The default is set to Daily. |
| CheckForUpdates | no | `true` | `true` if P2G should check for updates and write a log message if a new release is available. If using the UI this message will display there as well. |
| CloseConsoleOnFinish | no | `false` | `true` if P2G should immediately exit after completing its run. This setting only applies if you are using Console Client / Docker Headless version of P2G. |
