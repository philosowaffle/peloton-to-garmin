###################
# BUILD LAYER
###################
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

COPY . /build
WORKDIR /build

SHELL ["/bin/bash", "-c"]

ARG TARGETPLATFORM="linux-x64"
ARG VERSION

RUN echo $TARGETPLATFORM \
	&& echo $VERSION
ENV VERSION=${VERSION}

###################
# BUILD  APP
###################
FROM build as build_console

RUN dotnet publish /build/src/PelotonToGarminConsole/PelotonToGarminConsole.csproj -c Release -r ${TARGETPLATFORM} -o /build/published --version-suffix $VERSION

FROM build as build_api

RUN dotnet publish /build/src/Api/Api.csproj -c Release -r ${TARGETPLATFORM} -o /build/published --version-suffix $VERSION 

FROM build as build_web

RUN dotnet publish /build/src/WebUI/WebUI.csproj -c Release -r ${TARGETPLATFORM} -o /build/published --version-suffix $VERSION

###################
# CREATE FINAL LAYER
###################
FROM mcr.microsoft.com/dotnet/aspnet:6.0 as final_base

RUN apt-get update \
	&& apt-get -y install bash python3 python3-pip tzdata \
	&& apt-get purge -y -f --force-yes $EXT_BUILD_DEPS \
	&& apt-get autoremove -y \
	&& apt-get clean \
	&& rm -rf /var/lib/apt/lists/*

RUN groupadd -g 1015 p2g && useradd -g p2g -u 1015 p2g
WORKDIR /app

FROM final_base as final_python

ENV PYTHONUNBUFFERED=1
RUN ln -sf python3 /usr/bin/python \
	&& pip3 install --no-cache --upgrade pip setuptools \
	&& python --version \
	&& pip3 --version

RUN mkdir -m770 {output,data,working}
COPY ./python/requirements.txt ./requirements.txt
RUN pip3 install -r requirements.txt

FROM final_python as final_console

COPY --from=build_console /build/published .
COPY --from=build_console /build/LICENSE ./LICENSE
COPY --from=build_console /build/configuration.example.json ./configuration.local.json

COPY --chmod=770 ./docker/entrypoint.sh .

RUN chown -R p2g:p2g /app
RUN chmod 770 -R /app

EXPOSE 80 443
ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["./PelotonToGarminConsole"]

FROM final_python as final_api

COPY --from=build_api /build/published .
COPY --from=build_api /build/LICENSE ./LICENSE
COPY --from=build_api /build/configuration.example.json ./configuration.local.json

COPY --chmod=770 ./docker/entrypoint.sh .

EXPOSE 80 443
ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["./Api"]

FROM final_base as final_web

COPY --from=build_web /build/published .
COPY --from=build_web /build/LICENSE ./LICENSE
COPY --from=build_web /build/configuration.example.json ./configuration.local.json

COPY --chmod=770 ./docker/entrypoint.sh .

EXPOSE 80 443
ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["./WebUI"]
