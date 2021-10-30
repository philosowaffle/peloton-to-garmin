---
layout: default
title: Build from Source
parent: Install
nav_order: 2
---

# Build from Source

To compile and run P2G on your machine, follow the below steps:

1. Install the [dotnet 5.0 sdk](https://dotnet.microsoft.com/download/dotnet/5.0)
1. Clone this repository locally
1. In the local repo, find the file named `configuration.example.json`. Make a copy of it and name it `configuration.local.json`.
1. Move `configuration.local.json` into the `src/PelotongToGarminConsole` directory
1. Open `configuration.local.json` in a text editor of your choice and configure your settings.
    1. In the Garmin Config section set: `"UploadStrategy": 2`

    ```json
        "Garmin": {
            "Email": "garmin@gmail.com",
            "Password": "garmin",
            "Upload": false,
            "FormatToUpload": "fit",
            "UploadStrategy": 2
        }
    ```

    1. Be sure to set your usernames and passwords in Garmin and Peloton config sections respectively.
    1. Save and close the file
1. Open a terminal and run the below one-time setup steps:

```bash
> cd peloton-to-garmin
> dotnet restore
> dotnet build
```

## To run P2G

```bash
> dotnet run --project ./src/PelotonToGarminConsole/PelotonToGarminConsole.csproj
```
