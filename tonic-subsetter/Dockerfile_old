ARG DOTNET_CORE_IMAGE=runtime

# oracle_packager stage to get and configure oracle client
FROM ecr.vip.ebayc3.com/tess/alpine:hardened AS oracle_packager
RUN \
apk update && \
apk add --no-cache unzip wget
#rm -rf /var/lib/apt/lists/*

# install Oracle Instant Client
RUN mkdir -p /opt/oracle/ && \
    wget https://download.oracle.com/otn_software/linux/instantclient/1919000/instantclient-basic-linux.x64-19.19.0.0.0dbru.zip && \
    wget https://download.oracle.com/otn_software/linux/instantclient/1919000/instantclient-tools-linux.x64-19.19.0.0.0dbru.zip && \
    wget https://download.oracle.com/otn_software/linux/instantclient/1919000/instantclient-sqlplus-linux.x64-19.19.0.0.0dbru.zip && \
    unzip instantclient-basic-linux.x64-19.19.0.0.0dbru.zip -d /opt/oracle/ && \
    unzip instantclient-tools-linux.x64-19.19.0.0.0dbru.zip -d /opt/oracle/ && \
    unzip instantclient-sqlplus-linux.x64-19.19.0.0.0dbru.zip -d /opt/oracle/ && \
    rm instantclient-basic-linux.x64-19.19.0.0.0dbru.zip && \
    rm instantclient-tools-linux.x64-19.19.0.0.0dbru.zip && \
    rm instantclient-sqlplus-linux.x64-19.19.0.0.0dbru.zip

# Build stage/image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

RUN dotnet tool install --tool-path /tools dotnet-sos && \
  dotnet tool install --tool-path /tools dotnet-counters && \
  dotnet tool install --tool-path /tools dotnet-dump && \
  dotnet tool install --tool-path /tools dotnet-gcdump && \
  dotnet tool install --tool-path /tools dotnet-trace

ARG CONFIGURATION=Release
ARG PROJECT=Tonic.PrimaryKeyRemapper

# Copy csproj/sln and restore as distinct layers
COPY *.sln ./
COPY Tonic.Common/Tonic.Common.csproj Tonic.Common/
COPY Tonic.ForeignKeyMaskConfigure/Tonic.ForeignKeyMaskConfigure.csproj Tonic.ForeignKeyMaskConfigure/
COPY Tonic.PrimaryKeyRemapper/Tonic.PrimaryKeyRemapper.csproj Tonic.PrimaryKeyRemapper/
COPY Tonic.ResetSchema/Tonic.ResetSchema.csproj Tonic.ResetSchema/
COPY Tonic.Subsetter/Tonic.Subsetter.csproj Tonic.Subsetter/
COPY Tonic.TableCount/Tonic.TableCount.csproj Tonic.TableCount/
COPY Tonic.CleanBatch/Tonic.CleanBatch.csproj Tonic.CleanBatch/
COPY Tonic.SubsetterWebService/Tonic.SubsetterWebService.csproj Tonic.SubsetterWebService/
COPY Tonic.ReuseRowsAlterations/Tonic.ReuseRowsAlterations.csproj Tonic.ReuseRowsAlterations/
COPY Tonic.Test/Tonic.Test.csproj Tonic.Test/
# Tonic.ReuseRowsLocalPrototype is meant for local development only,
# and as such does not produce a binary when `dotnet publish` is executed.
# .csproj is copied here so no errors are thrown during compilation
COPY Tonic.ReuseRowsLocalPrototype/Tonic.ReuseRowsLocalPrototype.csproj Tonic.ReuseRowsLocalPrototype/
RUN dotnet restore

# Copy and publish app and libraries
COPY . .
WORKDIR /source/${PROJECT}
RUN dotnet publish -c ${CONFIGURATION} -o /app --no-restore

# Final stage/image
FROM mcr.microsoft.com/dotnet/${DOTNET_CORE_IMAGE}:6.0
ENV TZ UTC
WORKDIR /app
COPY --from=build /app .

# dotnet tools
WORKDIR /tools
COPY --from=build /tools .
ENV PATH=/tools:$PATH
  
RUN apt-get update && \
  apt-get install -y --no-install-recommends \
  dnsutils \
  libaio1 \
  libaio-dev \
  netcat \
  telnet \
  vim \
  && \
  apt-get clean && \
  rm -rf /var/lib/apt/lists/*

# Use oracle-packager to configure oracle client
COPY --from=oracle_packager /opt/oracle /opt/oracle

RUN sh -c "echo /opt/oracle/instantclient_19_19 > /etc/ld.so.conf.d/oracle-instantclient.conf" && \
  ldconfig

ENV NLS_LANG=AMERICAN_AMERICA.AL32UTF8
ENV ORACLE_HOME=/opt/oracle/instantclient_19_19
ENV PATH=$PATH:/opt/oracle/instantclient_19_19

# Miscellaneous files
COPY files/start-aspnetcore-app.sh /usr/local/bin/start-aspnetcore-app.sh

WORKDIR /source/${PROJECT}

ENTRYPOINT ["/usr/local/bin/start-aspnetcore-app.sh"]
