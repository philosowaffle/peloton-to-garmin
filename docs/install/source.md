---
layout: default
title: Build from Source
parent: Install
nav_order: 5
---

# Build from Source

To compile and run P2G on your machine, follow the below steps.

## dotnet 6.0

1. Install the latest [dotnet 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
1. Clone the GitHub repository locally
1. In the local repo, find the file named `configuration.example.json`. Make a copy of it and name it `configuration.local.json`.
1. Move `configuration.local.json` into the `src/PelotongToGarminConsole` directory
1. Open `configuration.local.json` in a text editor of your choice and edit it to use your Peloton and Garmin credentials
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

## Updating

```bash
> git fetch
> git pull
> cd peloton-to-garmin
> dotnet restore ./src/PelotonToGarminConsole/PelotonToGarminConsole.csproj
> dotnet build ./src/PelotonToGarminConsole/PelotonToGarminConsole.csproj
> dotnet run --project ./src/PelotonToGarminConsole/PelotonToGarminConsole.csproj
```
