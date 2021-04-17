# Contributing

Enhancements and fixes are always welcome. Feel free to contribute to any of the Issues not already assigned to another person.

1. Install [Python 3](https://www.python.org/downloads/) and pip
1. Install [dotent 5.0 runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime)
1. Clone this repository
1. In the repository modify the `configuration.local.json`
1. Set `"PythonAndGUploadInstalled": true`
1. Open the command line

```
> cd peloton-to-garmin
> cd python
> pip install -r requirements.txt
> cd ../
> dotnet restore
> dotnet build
> dotnet run ./PelotonToGarminConsole/PelotonToGarminConsole.csproj
```

# Python
```
> cd python
> edit python script
> pip install -r requirements.txt
> pip install pyinstaller
> pyinstaller -n upload_<os> --distpath ./ --console --clean --noconfirm upload.py
```