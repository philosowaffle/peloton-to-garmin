# Configuration

## How are you running P2G?

1. [I'm using the Web UI](#web-ui-configuration)
1. [I'm using the Windows GUI](#windows-ui-configuration)
1. [I'm using GitHub Actions](#config-file)
1. [I'm running Headless](#config-file)

## Web UI Configuration

The most common settings can be configured via the UI itself.  Additional lower level settings can be provided via config file.

1. Settings
    1. [App Settings](app.md)
    1. [Conversion Settings](format.md)
    1. [Peloton Settings](peloton.md)
    1. [Garmin Settings](garmin.md)
1. Low Level Settings
    1. [Api Configuration](api.md)
    1. [Web UI Configuration](webui.md)

## Windows UI Configuration

The most common settings can be configured via the UI itself.

1. Settings
    1. [App Settings](app.md)
    1. [Conversion Settings](format.md)
    1. [Peloton Settings](peloton.md)
    1. [Garmin Settings](garmin.md)

## Config File

When using a flavor of P2G that does not provide a user interface, all settings are provided via a JSON config file.

P2G looks for a file named `configuration.local.json` in the same directory where it is run to load its settings.

The structure of this file is as follows:

```json
{
    "App": { /**(1)!**/ },
    "Format": { /**(2)!**/ },
    "Peloton": { /**(3)!**/ },
    "Garmin": { /**(4)!**/ },
    "Observability": { /**(5)!**/ }
}
```

1. Go to [App Settings Documentation](app.md)
2. Go to [Format Settings Documentation](format.md)
3. Go to [Peloton Settings Documentation](peloton.md)
4. Go to [Garmin Settings Documentation](garmin.md)
5. Go to [Observability Settings Documentation](observability.md)

!!! tip
    P2G provides an [example config](https://github.com/philosowaffle/peloton-to-garmin/blob/master/configuration.example.json) to get you started.

## Additional Configuration Options

P2G supports configuration via

1. [command line arguments](#command-line-configuration)
1. [environment variables](#environment-variable-configuration)
1. [json config file](#config-file)
1. [via the user interface](#windows-ui-configuration)

By default, P2G looks for a file named `configuration.local.json` in the same directory where it is run.

!!! tip
    You can override where the config Directory is mounted in the docker container by setting the environment vairable `P2G_CONFIG_DIRECTORY`.  P2G will expect to find a `configuration.local.json` file in the specified directory.

### Config Precedence

The following defines the precedence in which config definitions are honored. With the first items having higher precendence than the next items.

1. Command Line
1. Environment Variables
1. Config File

For example, if you defined your Peloton credentials ONLY in the Config file, then the Config file credentials will be used.

If you defined your credentials in both the Config file AND the Environment variables, then the Environment variable credentials will be used.

If you defined credentials using all 3 methods (config file, env, and command line), then the credentials provided via the command line will be used.

### Command Line Configuration

All of the values defined in the [Json config file](#config-file) can also be defined as command line arguments. This functionality is provided by the default dotnet [IConfiguration interface](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#command-line-1).

### Environment Variable Configuration

All of the values defined in the [Json config file](#config-file) can also be defined as environment variables. This functionality is provided by the default dotnet [IConfiguration interface](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables-1).

The variables use the following convention, note the use of both single and double underscores:

```bash
P2G_CONFIGSECTION__CONFIGPROPERTY=value
```

#### Example App Config

```bash
P2G_APP__WORKINGDIRECTORY
P2G_APP__ENABLEPOLLING
P2G_APP__POLLINGINTERVALSECONDS
P2G_APP__PYTHONANDGUPLOADINSTALLED
```

#### Example Arrays

```bash
P2G_PELOTON__EXCLUDEWORKOUTTYPES__0="meditation"
P2G_PELOTON__EXCLUDEWORKOUTTYPES__1="stretching"
P2G_PELOTON__EXCLUDEWORKOUTTYPES__2="yoga"
...and so on
```

#### Example Nested Sections

For nested config sections, continue to use the same naming convention of defining the entire json path using `__` double underscores, and 0 based indexing for array values.

```bash
P2G_OBSERVABILITY__SERILOG__WRITETO__0__NAME="File"
```
#### Additional Environment Variables

In addition to overriding config values, the following extra environment variables are also supported.

| ENV Variable | Required | Default | Description | 
|:-------------|:---------|:--------|:--------------------------|
| `P2G_CONFIG_DIRECTORY` | false | P2G Directory | Tells P2G where to look for the `configuration.local.json` file |