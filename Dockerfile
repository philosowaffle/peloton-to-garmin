FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

COPY . /build
WORKDIR /build
RUN dotnet publish /build/src/PelotonToGarminConsole/PelotonToGarminConsole.csproj -c Release -o /build/published

FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine

ENV PYTHONUNBUFFERED=1
RUN apk add --update --no-cache python3 && ln -sf python3 /usr/bin/python
RUN python3 -m ensurepip
RUN pip3 install --no-cache --upgrade pip setuptools

WORKDIR /app

COPY --from=build /build/published .
COPY --from=build /build/requirements.txt ./requirements.txt
COPY --from=build /build/LICENSE ./LICENSE
COPY --from=build /build/configuration.example.json ./configuration.local.json

RUN mkdir output
RUN mkdir working

RUN touch syncHistory.json
RUN echo "{}" >> syncHistory.json

RUN pip3 install -r requirements.txt

RUN ls -l
ENTRYPOINT ["./PelotonToGarminConsole"]