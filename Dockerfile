###################
# CREATE FINAL LAYER
###################
FROM mcr.microsoft.com/dotnet/aspnet:5.0 as final

ENV PYTHONUNBUFFERED=1
RUN apt-get update \
	&& apt-get -y install bash python3 python3-pip tzdata \
	&& apt-get purge -y -f --force-yes $EXT_BUILD_DEPS \
	&& apt-get autoremove -y \
	&& apt-get clean \
	&& rm -rf /var/lib/apt/lists/* \
	&& ln -sf python3 /usr/bin/python \
	&& pip3 install --no-cache --upgrade pip setuptools \
	&& python --version \
	&& pip3 --version

# Setup console app
WORKDIR /app

RUN mkdir output \
	&& mkdir working \
	&& mkdir data

###################
# BUILD LAYER
###################
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

COPY . /build
WORKDIR /build

SHELL ["/bin/bash", "-c"]

ARG TARGETPLATFORM
ARG VERSION

RUN echo $TARGETPLATFORM \
	&& echo $VERSION
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
FROM final

COPY --from=build /build/published .
COPY --from=build /build/python/requirements.txt ./requirements.txt
COPY --from=build /build/LICENSE ./LICENSE
COPY --from=build /build/configuration.example.json ./configuration.local.json

# Setup web app
COPY --from=build /buildweb/published .

RUN pip3 install -r requirements.txt

COPY --chmod ./entrypoint.sh .
RUN chmod +x entrypoint.sh

EXPOSE 80 443
ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["console"]
