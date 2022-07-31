---
layout: default
title: Contributing
nav_order: 7
---

# Contributing

Enhancements and fixes are always welcome. Feel free to contribute to any of the Issues not already assigned to another person.

## Pull Requests

Ensure that your code:

1. Compiles
1. Follows established conventions of the codebase
1. Unit tests have been added and pass
1. Docs have been updated

## Development

1. Install [dotent 6.0 runtime](https://dotnet.microsoft.com/download/dotnet/6.0/runtime)

```
> dotnet restore
> dotnet build
> dotnet run ./src/PelotonToGarminConsole/PelotonToGarminConsole.csproj
```

## Package Windows exe
```
> dotnet publish ./src/PelotonToGarminConsole/PelotonToGarminConsole.csproj --no-restore -c Release -r win10-x64 -o ./dist --version-suffix local
```

## Developing against garmin-upload python library

1. Install [Python 3](https://www.python.org/downloads/) and pip
1. Set `"PythonAndGUploadInstalled": true`

```bash
> cd peloton-to-garmin
> cd python
> pip install -r requirements.txt
```

### Compile python exe

``` bash
> cd python
> pip install -r requirements.txt
> pip install pyinstaller
> pyinstaller -n upload --distpath ./ --console --clean --noconfirm upload.py
```

## Contribute to the docs

The docs site can be run locally using docker for faster development.

```bash
> cd peloton-to-garmin/docs
> docker-compose up
```

Browse to `http://localhost:4000` to see the docs site. The docker container watches for file changes and will hot reload.  Some of the navigation links may not work as expected locally, if you see the url change to `http://0.0.0.0/somePage.html` simply change it to `http://localhost/somePage.html` and the page will render.
