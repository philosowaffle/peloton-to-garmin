
# Build from Source

To compile and run P2G on your machine, follow the below steps.

## Setup

1. Install the latest [dotnet 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
1. Clone the GitHub repository locally
1. In the local repo, find the file named `configuration.example.json`. Make a copy of it and name it `configuration.local.json`.
1. Open a terminal and run the below one-time setup steps:

```bash
> cd peloton-to-garmin
> dotnet restore
> dotnet build
```

## To run P2G

### Console

1. Move `configuration.local.json` into the `src/ConsoleClient` directory
1. Open `configuration.local.json` in a text editor of your choice and edit it to use your Peloton and Garmin credentials

```bash
> dotnet run --project ./src/ConsoleClient/ConsoleClient.csproj
```

### Windows UI

```bash
> dotnet run --project ./src/ClientUI/ClientUI.csproj
```

### Web UI

```bash
> dotnet run --project ./src/WebUI/WebUI.csproj
> dotnet run --project ./src/Api/Api.csproj
```

## Updating

```bash
> git fetch
> git pull
> cd peloton-to-garmin
> dotnet restore
> dotnet build
```
