
# Build from Source

To compile and run P2G on your machine, follow the below steps.

## ⬇️ Install

1. Install the latest [dotnet 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
1. Clone the GitHub repository locally, this will result in P2G being cloned into a folder `peloton-to-garmin`

Proceed with the following steps for the specific flavor of P2G you would like to use.

1. [Console](#console)
1. [WebUI](#webui)
1. [Windows Application](#windows-application)

### Console

Open a terminal and run the provided commands.

```bash
> cd peloton-to-garmin
> dotnet restore ./src/ConsoleClient/ConsoleClient.csproj
> dotnet build ./src/ConsoleClient/ConsoleClient.csproj
```

1. Inside the `peloton-to-garmin` folder, find the file named `configuration.example.json`. Make a copy of it and name it `configuration.local.json`.
1. Move `configuration.local.json` into the `src/ConsoleClient` directory
1. Open `configuration.local.json` in a text editor of your choice and edit it to use your Peloton and Garmin credentials

You can learn more about customizing your configuration over in the [Configuration Section](../configuration/index.md).  You will apply any customizations to the `configuration.local.json` file we just created.

When you're ready to start P2G, run the below command.  This will be used any time you want to run the application.

```bash
> dotnet run --project ./src/ConsoleClient/ConsoleClient.csproj
```

### WebUI

Open a terminal and run the provided commands.

```bash
> cd peloton-to-garmin
>
> dotnet restore ./src/WebUI/WebUI.csproj
> dotnet build ./src/WebUI/WebUI.csproj
>
> dotnet restore ./src/Api/Api.csproj
> dotnet build ./src/Api/Api.csproj
```

When you're ready to start P2G, run the below commands.  You will likely need to open two terminal windows and run one command in each.

```bash
> dotnet run --project ./src/WebUI/WebUI.csproj
```

```bash
> dotnet run --project ./src/Api/Api.csproj
```

P2G will now be available at `http://localhost:8080`.

You can learn more about customizing your configuration over in the [Configuration Section](../configuration/index.md).

### Windows Application

!!! tip
    These steps only work on a Windows machine.

Open a terminal and run the provided commands.

```bash
> cd peloton-to-garmin
>
> dotnet restore ./src/ClientUI/ClientUI.csproj
> dotnet build ./src/ClientUI/ClientUI.csproj
> dotnet run --project ./src/ClientUI/ClientUI.csproj
```

You can learn more about customizing your configuration over in the [Configuration Section](../configuration/index.md).

### ⬆️ Updating

Open a terminal and run the below commands from within the `peloton-to-garmin` directory:

```bash
> cd peloton-to-garmin
> git fetch && git pull
```

Refer back to the [Install](#️-install) section for your flavor of P2G.  You will need to run the following commands again for your specific flavor.

```bash
> dotnet restore
> dotnet build
> dotnet run
```

### ❌ Uninstalling

Simply delete your P2G folder. Done.

!!! warning

    On most systems this will move P2G to your Trash folder, which means you can restore it from there if needed.  However, once it is deleted from your Trash, it will no longer be recoverable.  This includes all of your configuration and settings.

### #️⃣ Changing Versions

1. Find the release you want from the [releases page](https://github.com/philosowaffle/peloton-to-garmin/tags)
1. From your P2G folder, run `git checkout <tag>`, for example `get checkout v4.4.0`, this will update your local copy of P2G to be that version

Refer back to the [Install](#️-install) section for your flavor of P2G.  You will need to run the following commands again for your specific flavor.

```bash
> dotnet restore
> dotnet build
> dotnet run
```

!!! warning

    Attempting to use configuration or data from a later version of P2G with an older version is not guaranteed to work. You may need to reconfigure your instance.

### 👪 Multiple Users

To setup P2G for an additional user, simply clone P2G again into a new Folder named `<PersonsName>-P2G`.  When `PersonsName` would like to launch P2G, they should do so by running the specified commands from a terminal in this folder.

### 🚧 Limitations

Building from Source is not the preferred method of running P2G.  It can be tedious and error prone and difficult to receive support from others for when things go wrong.
