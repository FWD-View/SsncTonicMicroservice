# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["NuGet.Config", "."]
COPY ["Tonic.Subsetter/Tonic.Subsetter.csproj", "Tonic.Subsetter/"]
COPY ["Tonic.Common/Tonic.Common.csproj", "Tonic.Common/"]
RUN dotnet restore "./Tonic.Subsetter/Tonic.Subsetter.csproj"
COPY . .
WORKDIR "/src/Tonic.Subsetter"
RUN dotnet build "./Tonic.Subsetter.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tonic.Subsetter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install required packages for Linux DB2 client
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    binutils \
    libaio1 \
    wget \
    unzip \
    libxml2 \
    && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Download and install IBM Data Server Driver
WORKDIR /tmp
RUN wget -q https://public.dhe.ibm.com/ibmdl/export/pub/software/data/db2/drivers/odbc_cli/linuxx64_odbc_cli.tar.gz && \
    mkdir -p /opt/ibm/db2/clidriver && \
    tar -xzf linuxx64_odbc_cli.tar.gz -C /opt/ibm/db2/clidriver --strip-components=1 && \
    rm -f linuxx64_odbc_cli.tar.gz

# Copy the application
WORKDIR /app
COPY --from=build /app .

# Install additional dependencies
RUN apt-get update && \
  apt-get install -y --no-install-recommends \
    dnsutils \
    libstdc++6 \
    libaio-dev \
    netcat \
    telnet \
    vim && \
  apt-get clean && \
  rm -rf /var/lib/apt/lists/*

# Remove any previously copied or conflicting DB2 files in /app/clidriver
RUN rm -rf /app/clidriver/lib /app/clidriver/bin

# Copy DB2 libraries to standard Linux locations (only from the /opt/ibm/db2/clidriver path)
RUN mkdir -p /app/clidriver/lib && \
    cp -r /opt/ibm/db2/clidriver/lib/* /app/clidriver/lib/ && \
    mkdir -p /app/clidriver/bin && \
    cp -r /opt/ibm/db2/clidriver/bin/* /app/clidriver/bin/

# Set correct permissions for the DB2 libraries and binaries
RUN chmod 755 /app/clidriver/lib/*.so* && \
    chmod 755 /app/clidriver/bin/*

# Set environment variables for DB2
ENV DB2_CLI_DRIVER_INSTALL_PATH=/app/clidriver
ENV IBM_DB_HOME=/app/clidriver
ENV LD_LIBRARY_PATH=/app/clidriver/lib:${LD_LIBRARY_PATH}
ENV PATH=/app/clidriver/bin:${PATH}



ENTRYPOINT ["dotnet", "Tonic.Subsetter.dll"]