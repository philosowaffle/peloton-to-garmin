FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

COPY . /build
WORKDIR /build

SHELL ["/bin/bash", "-c"]

ARG TARGETPLATFORM
ARG VERSION

RUN echo $TARGETPLATFORM
RUN echo $VERSION
ENV VERSION=${VERSION}

###################
# BUILD CONSOLE APP
###################
RUN if [[ "$TARGETPLATFORM" = "linux/arm64" ]] ; then \
		dotnet publish /build/src/PelotonToGarminConsole/PelotonToGarminConsole.csproj -c Release -r linux-arm64 -o /build/published --version-suffix $VERSION ; \
	else \
		dotnet publish /build/src/PelotonToGarminConsole/PelotonToGarminConsole.csproj -c Release -r linux-x64 -o /build/published --version-suffix $VERSION ; \
	fi

###################
# BUILD WEB APP
###################
RUN if [[ "$TARGETPLATFORM" = "linux/arm64" ]] ; then \
		dotnet publish /build/src/WebApp/WebApp.csproj -c Release -r linux-arm64 -o /buildweb/published --version-suffix $VERSION ; \
	else \
		dotnet publish /build/src/WebApp/WebApp.csproj -c Release -r linux-x64 -o /buildweb/published --version-suffix $VERSION ; \
 	fi

###################
# FINAL
###################
FROM mcr.microsoft.com/dotnet/aspnet:5.0

ENV PYTHONUNBUFFERED=1
RUN apt-get update
RUN apt-get -y install bash python3 python3-pip tzdata && ln -sf python3 /usr/bin/python
RUN pip3 install --no-cache --upgrade pip setuptools

RUN python --version
RUN pip3 --version

# Setup console app
WORKDIR /app

RUN useradd 1030
USER 1030

COPY --from=build /build/published .
COPY --from=build /build/python/requirements.txt ./requirements.txt
COPY --from=build /build/LICENSE ./LICENSE
COPY --from=build /build/configuration.example.json ./configuration.local.json

RUN mkdir output
RUN mkdir working
RUN mkdir data

# Setup web app
COPY --from=build /buildweb/published .

RUN pip3 install -r requirements.txt

COPY ./entrypoint.sh .
RUN chmod 777 entrypoint.sh

EXPOSE 80 443
ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["console"]
