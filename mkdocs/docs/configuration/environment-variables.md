
# Environment Variable Configuration

All of the values defined in the [Json config file](json.md) can also be defined as environment variables. This functionality is provided by the default dotnet [IConfiguration interface](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables-1).

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
