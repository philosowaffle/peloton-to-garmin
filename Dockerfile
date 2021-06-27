FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

COPY . /build
WORKDIR /build

SHELL ["/bin/bash", "-c"]

ARG TARGETPLATFORM
ARG VERSION

RUN echo $TARGETPLATFORM
RUN echo $VERSION
ENV VERSION=${VERSION}

RUN if [[ "$TARGETPLATFORM" = "linux/arm64" ]] ; then \
		dotnet publish /build/src/PelotonToGarminConsole/PelotonToGarminConsole.csproj -c Release -r linux-arm64 -o /build/published --version-suffix $VERSION ; \
	else \
		dotnet publish /build/src/PelotonToGarminConsole/PelotonToGarminConsole.csproj -c Release -r linux-x64 -o /build/published --version-suffix $VERSION ; \
	fi

FROM mcr.microsoft.com/dotnet/aspnet:5.0

ENV PYTHONUNBUFFERED=1
RUN apt-get update
RUN apt-get -y install bash python3 python3-pip tzdata && ln -sf python3 /usr/bin/python
RUN pip3 install --no-cache --upgrade pip setuptools

RUN python --version
RUN pip3 --version

WORKDIR /app

COPY --from=build /build/published .
COPY --from=build /build/python/requirements.txt ./requirements.txt
COPY --from=build /build/LICENSE ./LICENSE
COPY --from=build /build/configuration.example.json ./configuration.local.json

RUN mkdir output
RUN mkdir working

RUN touch syncHistory.json
RUN echo "{}" >> syncHistory.json

RUN pip3 install -r requirements.txt

RUN ls -l
ENTRYPOINT ["./PelotonToGarminConsole"]