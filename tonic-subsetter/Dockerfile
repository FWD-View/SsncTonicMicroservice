# ARG DOTNET_CORE_IMAGE=runtime

# # oracle_packager stage to get and configure oracle client
# FROM ecr.vip.ebayc3.com/tess/alpine:hardened AS oracle_packager
# RUN \
# apk update && \
# apk add --no-cache unzip wget
# #rm -rf /var/lib/apt/lists/*

# # install Oracle Instant Client
# RUN mkdir -p /opt/oracle/ && \
#     wget https://download.oracle.com/otn_software/linux/instantclient/1919000/instantclient-basic-linux.x64-19.19.0.0.0dbru.zip && \
#     wget https://download.oracle.com/otn_software/linux/instantclient/1919000/instantclient-tools-linux.x64-19.19.0.0.0dbru.zip && \
#     wget https://download.oracle.com/otn_software/linux/instantclient/1919000/instantclient-sqlplus-linux.x64-19.19.0.0.0dbru.zip && \
#     unzip instantclient-basic-linux.x64-19.19.0.0.0dbru.zip -d /opt/oracle/ && \
#     unzip instantclient-tools-linux.x64-19.19.0.0.0dbru.zip -d /opt/oracle/ && \
#     unzip instantclient-sqlplus-linux.x64-19.19.0.0.0dbru.zip -d /opt/oracle/ && \
#     rm instantclient-basic-linux.x64-19.19.0.0.0dbru.zip && \
#     rm instantclient-tools-linux.x64-19.19.0.0.0dbru.zip && \
#     rm instantclient-sqlplus-linux.x64-19.19.0.0.0dbru.zip

# # Build stage/image
# FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# WORKDIR /source

# RUN dotnet tool install --tool-path /tools dotnet-sos && \
#   dotnet tool install --tool-path /tools dotnet-counters && \
#   dotnet tool install --tool-path /tools dotnet-dump && \
#   dotnet tool install --tool-path /tools dotnet-gcdump && \
#   dotnet tool install --tool-path /tools dotnet-trace

# ARG CONFIGURATION=Release
# ARG PROJECT=Tonic.PrimaryKeyRemapper

# # Copy csproj/sln and restore as distinct layers
# COPY *.sln ./
# COPY Tonic.Common/Tonic.Common.csproj Tonic.Common/
# COPY Tonic.ForeignKeyMaskConfigure/Tonic.ForeignKeyMaskConfigure.csproj Tonic.ForeignKeyMaskConfigure/
# COPY Tonic.PrimaryKeyRemapper/Tonic.PrimaryKeyRemapper.csproj Tonic.PrimaryKeyRemapper/
# COPY Tonic.ResetSchema/Tonic.ResetSchema.csproj Tonic.ResetSchema/
# COPY Tonic.Subsetter/Tonic.Subsetter.csproj Tonic.Subsetter/
# COPY Tonic.TableCount/Tonic.TableCount.csproj Tonic.TableCount/
# COPY Tonic.CleanBatch/Tonic.CleanBatch.csproj Tonic.CleanBatch/
# COPY Tonic.SubsetterWebService/Tonic.SubsetterWebService.csproj Tonic.SubsetterWebService/
# COPY Tonic.ReuseRowsAlterations/Tonic.ReuseRowsAlterations.csproj Tonic.ReuseRowsAlterations/
# COPY Tonic.Test/Tonic.Test.csproj Tonic.Test/
# # Tonic.ReuseRowsLocalPrototype is meant for local development only,
# # and as such does not produce a binary when `dotnet publish` is executed.
# # .csproj is copied here so no errors are thrown during compilation
# COPY Tonic.ReuseRowsLocalPrototype/Tonic.ReuseRowsLocalPrototype.csproj Tonic.ReuseRowsLocalPrototype/
# RUN dotnet restore

# # Copy and publish app and libraries
# COPY . .
# WORKDIR /source/${PROJECT}
# RUN dotnet publish -c ${CONFIGURATION} -o /app --no-restore

# # Final stage/image
# FROM mcr.microsoft.com/dotnet/${DOTNET_CORE_IMAGE}:6.0
# ENV TZ UTC
# WORKDIR /app
# COPY --from=build /app .

# # dotnet tools
# WORKDIR /tools
# COPY --from=build /tools .
# ENV PATH=/tools:$PATH
  
# RUN apt-get update && \
#   apt-get install -y --no-install-recommends \
#   dnsutils \
#   libaio1 \
#   libaio-dev \
#   netcat \
#   telnet \
#   vim \
#   && \
#   apt-get clean && \
#   rm -rf /var/lib/apt/lists/*

# # Use oracle-packager to configure oracle client
# COPY --from=oracle_packager /opt/oracle /opt/oracle

# RUN sh -c "echo /opt/oracle/instantclient_19_19 > /etc/ld.so.conf.d/oracle-instantclient.conf" && \
#   ldconfig

# ENV NLS_LANG=AMERICAN_AMERICA.AL32UTF8
# ENV ORACLE_HOME=/opt/oracle/instantclient_19_19
# ENV PATH=$PATH:/opt/oracle/instantclient_19_19

# # Miscellaneous files
# COPY files/start-aspnetcore-app.sh /usr/local/bin/start-aspnetcore-app.sh

# WORKDIR /source/${PROJECT}

# ENTRYPOINT ["/usr/local/bin/start-aspnetcore-app.sh"]

# # Use .NET SDK 6.0 to match your project requirements
# # Use .NET SDK 6.0 to match your project requirements
# # Use .NET SDK 6.0 to match your project requirements
# FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# WORKDIR /src

# # Create custom NuGet.config to avoid Windows paths
# RUN echo '<?xml version="1.0" encoding="utf-8"?> \
# <configuration> \
#   <packageSources> \
#     <clear /> \
#     <add key="nuget.org" value="https://api.nuget.org/v3/index.json" /> \
#   </packageSources> \
#   <fallbackPackageFolders> \
#     <clear /> \
#   </fallbackPackageFolders> \
#   <config> \
#     <add key="globalPackagesFolder" value="/root/.nuget/packages" /> \
#   </config> \
# </configuration>' > /root/NuGet.Config

# # Use the custom NuGet config
# ENV NUGET_CONFIG=/root/NuGet.Config

# # Copy solution file if you have one
# COPY *.sln ./

# # Create directories for the projects
# RUN mkdir -p Tonic.Subsetter Tonic.Common Tonic.Test

# # Copy csproj files
# COPY Tonic.Subsetter/*.csproj ./Tonic.Subsetter/
# COPY Tonic.Common/*.csproj ./Tonic.Common/
# COPY Tonic.Test/*.csproj ./Tonic.Test/

# # Restore dependencies
# RUN dotnet restore Tonic.Subsetter/Tonic.Subsetter.csproj --verbosity detailed
# RUN dotnet restore Tonic.Common/Tonic.Common.csproj --verbosity detailed
# RUN dotnet restore Tonic.Test/Tonic.Test.csproj --verbosity detailed

# # Copy project files
# COPY Tonic.Subsetter/. ./Tonic.Subsetter/
# COPY Tonic.Common/. ./Tonic.Common/
# COPY Tonic.Test/. ./Tonic.Test/

# # Build and publish
# RUN dotnet publish Tonic.Subsetter/Tonic.Subsetter.csproj -c Release -o /app

# # Build runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
# WORKDIR /app
# COPY --from=build /app .
# ENTRYPOINT ["dotnet", "Tonic.Subsetter.dll"]

# FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# WORKDIR /src

# RUN apt-get update && \
#   apt-get install -y --no-install-recommends \
#   dnsutils \
#   libaio1 \
#   libaio-dev \
#   netcat \
#   telnet \
#   vim \
#   && \
#   apt-get clean && \
#   rm -rf /var/lib/apt/lists/*

# # Create custom NuGet.config to avoid Windows paths
# RUN echo '<?xml version="1.0" encoding="utf-8"?> \
# <configuration> \
#   <packageSources> \
#     <clear /> \
#     <add key="nuget.org" value="https://api.nuget.org/v3/index.json" /> \
#   </packageSources> \
#   <fallbackPackageFolders> \
#     <clear /> \
#   </fallbackPackageFolders> \
#   <config> \
#     <add key="globalPackagesFolder" value="/root/.nuget/packages" /> \
#   </config> \
# </configuration>' > /root/NuGet.Config

# # Use the custom NuGet config
# ENV NUGET_CONFIG=/root/NuGet.Config

# # Copy solution file if you have one
# COPY *.sln ./

# # Create directories for the projects
# RUN mkdir -p Tonic.Subsetter Tonic.Common Tonic.Test

# # Copy csproj files
# COPY Tonic.Subsetter/*.csproj ./Tonic.Subsetter/
# COPY Tonic.Common/*.csproj ./Tonic.Common/
# COPY Tonic.Test/*.csproj ./Tonic.Test/

# # Restore dependencies
# RUN dotnet restore Tonic.Subsetter/Tonic.Subsetter.csproj --verbosity detailed
# RUN dotnet restore Tonic.Common/Tonic.Common.csproj --verbosity detailed
# RUN dotnet restore Tonic.Test/Tonic.Test.csproj --verbosity detailed

# # Locate the DB2 native libraries in the NuGet packages
# RUN find /root/.nuget/packages/ -name "db2app64.dll" || echo "db2app64.dll not found in NuGet packages"
# RUN find /root/.nuget/packages/ -path "*/runtimes/*/native/*" -name "*.dll" | grep -i db2 || echo "No DB2 native DLLs found"

# # Copy project files
# COPY Tonic.Subsetter/. ./Tonic.Subsetter/
# COPY Tonic.Common/. ./Tonic.Common/
# COPY Tonic.Test/. ./Tonic.Test/

# # Build and publish
# RUN dotnet publish Tonic.Subsetter/Tonic.Subsetter.csproj -c Release -o /app

# # Build runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
# WORKDIR /app

# # Install required packages for DB2 client
# RUN apt-get update && \
#     apt-get install -y --no-install-recommends \
#     libaio1 \
#     wget \
#     unzip \
#     && \
#     apt-get clean && \
#     rm -rf /var/lib/apt/lists/*

# # Download and install IBM Data Server Driver
# WORKDIR /tmp
# RUN wget -q https://public.dhe.ibm.com/ibmdl/export/pub/software/data/db2/drivers/odbc_cli/linuxx64_odbc_cli.tar.gz && \
#     mkdir -p /opt/ibm/db2/clidriver && \
#     tar -xzf linuxx64_odbc_cli.tar.gz -C /opt/ibm/db2/clidriver --strip-components=1 && \
#     rm -f linuxx64_odbc_cli.tar.gz

# # Create runtimes directory structure - this is crucial for .NET Native DLL lookup
# RUN mkdir -p /app/runtimes/linux-x64/native && \
#     mkdir -p /app/runtimes/win-x64/native

# # Copy important DB2 libraries to the runtimes native folder where .NET will look for them
# RUN if [ -f /opt/ibm/db2/clidriver/lib/libdb2.so ]; then \
#        cp /opt/ibm/db2/clidriver/lib/libdb2.so /app/runtimes/linux-x64/native/; \
#     fi

# # Set environment variables for DB2
# ENV DB2_CLI_DRIVER_INSTALL_PATH=/opt/ibm/db2/clidriver
# ENV IBM_DB_HOME=/opt/ibm/db2/clidriver
# ENV LD_LIBRARY_PATH=${LD_LIBRARY_PATH}:/opt/ibm/db2/clidriver/lib
# ENV PATH=${PATH}:/opt/ibm/db2/clidriver/bin

# # Create symbolic links for Linux equivalents of Windows DLLs
# RUN ln -s /opt/ibm/db2/clidriver/lib/libdb2.so /usr/lib/libdb2.so && \
#     ln -s /opt/ibm/db2/clidriver/lib/libdb2.so /usr/lib/db2app64.so

# # Configure Linux to find the shared libraries
# RUN echo "/opt/ibm/db2/clidriver/lib" > /etc/ld.so.conf.d/db2clidriver.conf && ldconfig

# # Copy the application
# WORKDIR /app
# COPY --from=build /app .

# # Create directory for your config
# RUN mkdir -p /app/subsetter/toml-config/

# # Create diagnostic script
# RUN echo '#!/bin/bash\n\
# echo "DB2 Environment Configuration:"\n\
# echo "DB2_CLI_DRIVER_INSTALL_PATH=$DB2_CLI_DRIVER_INSTALL_PATH"\n\
# echo "IBM_DB_HOME=$IBM_DB_HOME"\n\
# echo "LD_LIBRARY_PATH=$LD_LIBRARY_PATH"\n\
# echo "\nDirectory Listings:"\n\
# echo "DB2 Client Driver:"\n\
# ls -la /opt/ibm/db2/clidriver/lib | grep -i db2\n\
# echo "\nRuntime Native Directories:"\n\
# ls -la /app/runtimes/linux-x64/native\n\
# echo "\nSystem Libraries:"\n\
# ldconfig -p | grep -i db2\n\
# echo "\nStarting application..."\n\
# dotnet Tonic.Subsetter.dll /app/subsetter/toml-config/tes_db2.toml\n' > /app/start.sh && \
#     chmod +x /app/start.sh

# ENTRYPOINT ["/app/start.sh"]

# FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# WORKDIR /src

# RUN apt-get update && \
#   apt-get install -y --no-install-recommends \
#   dnsutils \
#   libaio1 \
#   libaio-dev \
#   netcat \
#   telnet \
#   vim \
#   && \
#   apt-get clean && \
#   rm -rf /var/lib/apt/lists/*

# # Create custom NuGet.config to avoid Windows paths
# RUN echo '<?xml version="1.0" encoding="utf-8"?> \
# <configuration> \
#   <packageSources> \
#     <clear /> \
#     <add key="nuget.org" value="https://api.nuget.org/v3/index.json" /> \
#   </packageSources> \
#   <fallbackPackageFolders> \
#     <clear /> \
#   </fallbackPackageFolders> \
#   <config> \
#     <add key="globalPackagesFolder" value="/root/.nuget/packages" /> \
#   </config> \
# </configuration>' > /root/NuGet.Config

# # Use the custom NuGet config
# ENV NUGET_CONFIG=/root/NuGet.Config

# # Copy solution file if you have one
# COPY *.sln ./

# # Create directories for the projects
# RUN mkdir -p Tonic.Subsetter Tonic.Common Tonic.Test

# # Copy csproj files
# COPY Tonic.Subsetter/*.csproj ./Tonic.Subsetter/
# COPY Tonic.Common/*.csproj ./Tonic.Common/
# COPY Tonic.Test/*.csproj ./Tonic.Test/

# # Restore dependencies
# RUN dotnet restore Tonic.Subsetter/Tonic.Subsetter.csproj --verbosity detailed
# RUN dotnet restore Tonic.Common/Tonic.Common.csproj --verbosity detailed
# RUN dotnet restore Tonic.Test/Tonic.Test.csproj --verbosity detailed

# # Locate the DB2 native libraries in the NuGet packages
# RUN find /root/.nuget/packages/ -name "db2app64.dll" || echo "db2app64.dll not found in NuGet packages"
# RUN find /root/.nuget/packages/ -path "*/runtimes/*/native/*" -name "*.dll" | grep -i db2 || echo "No DB2 native DLLs found"

# # Copy project files
# COPY Tonic.Subsetter/. ./Tonic.Subsetter/
# COPY Tonic.Common/. ./Tonic.Common/
# COPY Tonic.Test/. ./Tonic.Test/

# # Build and publish
# RUN dotnet publish Tonic.Subsetter/Tonic.Subsetter.csproj -c Release -o /app

# # Build runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
# WORKDIR /app

# # Install required packages for DB2 client
# RUN apt-get update && \
#     apt-get install -y --no-install-recommends \
#     libaio1 \
#     wget \
#     unzip \
#     && \
#     apt-get clean && \
#     rm -rf /var/lib/apt/lists/*

# # Download and install IBM Data Server Driver
# WORKDIR /tmp
# RUN wget -q https://public.dhe.ibm.com/ibmdl/export/pub/software/data/db2/drivers/odbc_cli/linuxx64_odbc_cli.tar.gz && \
#     mkdir -p /opt/ibm/db2/clidriver && \
#     tar -xzf linuxx64_odbc_cli.tar.gz -C /opt/ibm/db2/clidriver --strip-components=1 && \
#     rm -f linuxx64_odbc_cli.tar.gz

# # Create runtimes directory structure - this is crucial for .NET Native DLL lookup
# RUN mkdir -p /app/runtimes/linux-x64/native && \
#     mkdir -p /app/runtimes/win-x64/native

# # Copy important DB2 libraries to the runtimes native folder where .NET will look for them
# RUN if [ -f /opt/ibm/db2/clidriver/lib/libdb2.so ]; then \
#        cp /opt/ibm/db2/clidriver/lib/libdb2.so /app/runtimes/linux-x64/native/; \
#     fi

# # IMPORTANT FIX: Copy libdb2.so as db2app64.dll in the win-x64 folder
# # This tricks .NET into finding the Linux library when it looks for the Windows DLL
# RUN if [ -f /opt/ibm/db2/clidriver/lib/libdb2.so ]; then \
#        cp /opt/ibm/db2/clidriver/lib/libdb2.so /app/runtimes/win-x64/native/db2app64.dll; \
#     fi

# # Copy all necessary DB2 client libraries directly to the app directory
# RUN mkdir -p /app/clidriver/lib && \
#     cp -r /opt/ibm/db2/clidriver/lib/* /app/clidriver/lib/ && \
#     chmod 755 /app/clidriver/lib/*.so*

# # Set environment variables for DB2
# ENV DB2_CLI_DRIVER_INSTALL_PATH=/app/clidriver
# ENV IBM_DB_HOME=/app/clidriver
# ENV LD_LIBRARY_PATH=${LD_LIBRARY_PATH}:/app/clidriver/lib
# ENV PATH=${PATH}:/app/clidriver/bin

# # Create symbolic links for Linux equivalents of Windows DLLs
# RUN ln -s /app/clidriver/lib/libdb2.so /usr/lib/libdb2.so && \
#     ln -s /app/clidriver/lib/libdb2.so /usr/lib/db2app64.so && \
#     # Create Windows DLL named files that actually contain the Linux .so libraries
#     cp /app/clidriver/lib/libdb2.so /usr/lib/db2app64.dll

# # Configure Linux to find the shared libraries
# RUN echo "/app/clidriver/lib" > /etc/ld.so.conf.d/db2clidriver.conf && ldconfig

# # Copy the application
# WORKDIR /app
# COPY --from=build /app .

# # Create directory for your config
# RUN mkdir -p /app/subsetter/toml-config/

# # Create diagnostic script with more detailed information
# RUN echo '#!/bin/bash\n\
# echo "Current directory:"\n\
# pwd\n\
# echo "\\nContents of /app:"\n\
# ls -la\n\
# echo "\\nContents of /app/runtimes (if exists):"\n\
# if [ -d "/app/runtimes" ]; then ls -la /app/runtimes; fi\n\
# echo "\\nContents of win-x64/native directory:"\n\
# if [ -d "/app/runtimes/win-x64/native" ]; then ls -la /app/runtimes/win-x64/native; fi\n\
# echo "\\nContents of linux-x64/native directory:"\n\
# if [ -d "/app/runtimes/linux-x64/native" ]; then ls -la /app/runtimes/linux-x64/native; fi\n\
# echo "\\nEnvironment variables:"\n\
# echo "\\nLD_LIBRARY_PATH:"\n\
# echo $LD_LIBRARY_PATH\n\
# echo "\\nStarting application..."\n\
# dotnet Tonic.Subsetter.dll /app/subsetter/toml-config/tes_db2.toml\n' > /app/start.sh && \
#     chmod +x /app/start.sh

# ENTRYPOINT ["/app/start.sh"]

# FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# WORKDIR /src

# RUN apt-get update && \
#   apt-get install -y --no-install-recommends \
#   dnsutils \
#   libaio1 \
#   libaio-dev \
#   netcat \
#   telnet \
#   vim \
#   && \
#   apt-get clean && \
#   rm -rf /var/lib/apt/lists/*

# # Create custom NuGet.config to avoid Windows paths
# RUN echo '<?xml version="1.0" encoding="utf-8"?> \
# <configuration> \
#   <packageSources> \
#     <clear /> \
#     <add key="nuget.org" value="https://api.nuget.org/v3/index.json" /> \
#   </packageSources> \
#   <fallbackPackageFolders> \
#     <clear /> \
#   </fallbackPackageFolders> \
#   <config> \
#     <add key="globalPackagesFolder" value="/root/.nuget/packages" /> \
#   </config> \
# </configuration>' > /root/NuGet.Config

# # Use the custom NuGet config
# ENV NUGET_CONFIG=/root/NuGet.Config

# # Copy solution file if you have one
# COPY *.sln ./

# # Create directories for the projects
# RUN mkdir -p Tonic.Subsetter Tonic.Common Tonic.Test

# # Copy csproj files
# COPY Tonic.Subsetter/*.csproj ./Tonic.Subsetter/
# COPY Tonic.Common/*.csproj ./Tonic.Common/
# COPY Tonic.Test/*.csproj ./Tonic.Test/

# # Restore dependencies
# RUN dotnet restore Tonic.Subsetter/Tonic.Subsetter.csproj --verbosity detailed
# RUN dotnet restore Tonic.Common/Tonic.Common.csproj --verbosity detailed
# RUN dotnet restore Tonic.Test/Tonic.Test.csproj --verbosity detailed

# # Copy project files
# COPY Tonic.Subsetter/. ./Tonic.Subsetter/
# COPY Tonic.Common/. ./Tonic.Common/
# COPY Tonic.Test/. ./Tonic.Test/

# # Build and publish
# RUN dotnet publish Tonic.Subsetter/Tonic.Subsetter.csproj -c Release -o /app

# # Build runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
# WORKDIR /app

# # Install required packages for DB2 client
# RUN apt-get update && \
#     apt-get install -y --no-install-recommends \
#     libaio1 \
#     wget \
#     unzip \
#     libxml2 \
#     lsb-release \
#     && \
#     apt-get clean && \
#     rm -rf /var/lib/apt/lists/*

# # Download and install IBM Data Server Driver
# WORKDIR /tmp
# RUN wget -q https://public.dhe.ibm.com/ibmdl/export/pub/software/data/db2/drivers/odbc_cli/linuxx64_odbc_cli.tar.gz && \
#     mkdir -p /opt/ibm/db2/clidriver && \
#     tar -xzf linuxx64_odbc_cli.tar.gz -C /opt/ibm/db2/clidriver --strip-components=1 && \
#     rm -f linuxx64_odbc_cli.tar.gz

# # Set environment variables for DB2
# ENV DB2_CLI_DRIVER_INSTALL_PATH=/opt/ibm/db2/clidriver
# ENV IBM_DB_HOME=/opt/ibm/db2/clidriver
# ENV LD_LIBRARY_PATH=${LD_LIBRARY_PATH}:/opt/ibm/db2/clidriver/lib
# ENV PATH=${PATH}:/opt/ibm/db2/clidriver/bin

# # Create .NET's expected directory structure
# RUN mkdir -p /app/runtimes/linux-x64/native && \
#     mkdir -p /app/runtimes/win-x64/native

# # Copy the main DB2 library to the expected locations
# RUN cp /opt/ibm/db2/clidriver/lib/libdb2.so /app/runtimes/linux-x64/native/
# RUN cp /opt/ibm/db2/clidriver/lib/libdb2.so /app/runtimes/win-x64/native/db2app64.dll

# # Create symbolic links in system libraries path
# RUN ln -sf /opt/ibm/db2/clidriver/lib/libdb2.so /usr/lib/libdb2.so && \
#     ln -sf /opt/ibm/db2/clidriver/lib/libdb2.so /usr/lib/db2app64.dll && \
#     ln -sf /opt/ibm/db2/clidriver/lib/libdb2.so /usr/lib/db2app64.so

# # Copy ALL DB2 client libraries to app's runtime directories
# RUN cp -r /opt/ibm/db2/clidriver/lib/* /app/runtimes/linux-x64/native/

# # Configure Linux to find the shared libraries
# RUN echo "/opt/ibm/db2/clidriver/lib" > /etc/ld.so.conf.d/db2clidriver.conf && ldconfig

# # Copy the application
# WORKDIR /app
# COPY --from=build /app .

# # Create a dllmap configuration file
# RUN echo '<?xml version="1.0" encoding="utf-8"?> \
# <configuration> \
#   <dllmap dll="db2app64.dll" target="libdb2.so" os="linux"/> \
# </configuration>' > /app/Tonic.Subsetter.dll.config

# # Create directory for your config
# RUN mkdir -p /app/subsetter/toml-config/

# # Create comprehensive diagnostic script
# RUN echo '#!/bin/bash\n\
# echo "=============== DB2 DRIVER DIAGNOSTIC INFORMATION ==============="\n\
# echo "Current directory:"\n\
# pwd\n\
# echo "\\nOperating System:"\n\
# cat /etc/os-release\n\
# echo "\\nEnvironment variables:"\n\
# echo "LD_LIBRARY_PATH=$LD_LIBRARY_PATH"\n\
# echo "DB2_CLI_DRIVER_INSTALL_PATH=$DB2_CLI_DRIVER_INSTALL_PATH"\n\
# echo "IBM_DB_HOME=$IBM_DB_HOME"\n\
# echo "\\nContents of /app:"\n\
# ls -la /app\n\
# echo "\\nContents of /app/runtimes:"\n\
# if [ -d "/app/runtimes" ]; then ls -la /app/runtimes; fi\n\
# echo "\\nContents of /app/runtimes/win-x64/native:"\n\
# if [ -d "/app/runtimes/win-x64/native" ]; then ls -la /app/runtimes/win-x64/native; fi\n\
# echo "\\nContents of /app/runtimes/linux-x64/native:"\n\
# if [ -d "/app/runtimes/linux-x64/native" ]; then ls -la /app/runtimes/linux-x64/native; fi\n\
# echo "\\nContents of /opt/ibm/db2/clidriver/lib:"\n\
# if [ -d "/opt/ibm/db2/clidriver/lib" ]; then ls -la /opt/ibm/db2/clidriver/lib; fi\n\
# echo "\\nLibrary search paths:"\n\
# ldconfig -v 2>/dev/null | grep -v "^\\s"\n\
# echo "\\nSearching for DB2 libraries:"\n\
# find / -name "libdb2.so" 2>/dev/null\n\
# find / -name "db2app64.dll" 2>/dev/null\n\
# echo "\\nTrying ldd on important files:"\n\
# if [ -f "/usr/lib/db2app64.dll" ]; then \
#   echo "ldd on /usr/lib/db2app64.dll:" && ldd /usr/lib/db2app64.dll 2>&1 || echo "Failed to run ldd"; \
# fi\n\
# if [ -f "/app/runtimes/win-x64/native/db2app64.dll" ]; then \
#   echo "ldd on /app/runtimes/win-x64/native/db2app64.dll:" && ldd /app/runtimes/win-x64/native/db2app64.dll 2>&1 || echo "Failed to run ldd"; \
# fi\n\
# echo "\\nChecking for dll config file:"\n\
# if [ -f "/app/Tonic.Subsetter.dll.config" ]; then \
#   echo "Config file exists:" && cat /app/Tonic.Subsetter.dll.config; \
# else \
#   echo "Config file not found"; \
# fi\n\
# echo "\\n=============== STARTING APPLICATION ==============="\n\
# LD_DEBUG=libs dotnet Tonic.Subsetter.dll /app/subsetter/toml-config/tes_db2.toml\n' > /app/start.sh && \
#     chmod +x /app/start.sh

# ENTRYPOINT ["/app/start.sh"]

# FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# WORKDIR /src

# RUN apt-get update && \
#   apt-get install -y --no-install-recommends \
#   dnsutils \
#   libaio1 \
#   libaio-dev \
#   netcat \
#   telnet \
#   vim \
#   && \
#   apt-get clean && \
#   rm -rf /var/lib/apt/lists/*

# # Create custom NuGet.config to avoid Windows paths
# RUN echo '<?xml version="1.0" encoding="utf-8"?> \
# <configuration> \
#   <packageSources> \
#     <clear /> \
#     <add key="nuget.org" value="https://api.nuget.org/v3/index.json" /> \
#   </packageSources> \
#   <fallbackPackageFolders> \
#     <clear /> \
#   </fallbackPackageFolders> \
#   <config> \
#     <add key="globalPackagesFolder" value="/root/.nuget/packages" /> \
#   </config> \
# </configuration>' > /root/NuGet.Config

# # Use the custom NuGet config
# ENV NUGET_CONFIG=/root/NuGet.Config

# # Copy solution file if you have one
# COPY *.sln ./

# # Create directories for the projects
# RUN mkdir -p Tonic.Subsetter Tonic.Common Tonic.Test

# # Copy csproj files
# COPY Tonic.Subsetter/*.csproj ./Tonic.Subsetter/
# COPY Tonic.Common/*.csproj ./Tonic.Common/
# COPY Tonic.Test/*.csproj ./Tonic.Test/

# # Restore dependencies
# RUN dotnet restore Tonic.Subsetter/Tonic.Subsetter.csproj --verbosity detailed
# RUN dotnet restore Tonic.Common/Tonic.Common.csproj --verbosity detailed
# RUN dotnet restore Tonic.Test/Tonic.Test.csproj --verbosity detailed

# # Copy project files
# COPY Tonic.Subsetter/. ./Tonic.Subsetter/
# COPY Tonic.Common/. ./Tonic.Common/
# COPY Tonic.Test/. ./Tonic.Test/

# # Build and publish
# RUN dotnet publish Tonic.Subsetter/Tonic.Subsetter.csproj -c Release -o /app

# # Runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
# WORKDIR /app

# # Install required packages
# RUN apt-get update && \
#     apt-get install -y --no-install-recommends \
#     libaio1 \
#     wget \
#     unzip \
#     libxml2 \
#     lsb-release \
#     file \
#     && \
#     apt-get clean && \
#     rm -rf /var/lib/apt/lists/*

# # Download and install IBM Data Server Driver
# WORKDIR /tmp
# RUN wget -q https://public.dhe.ibm.com/ibmdl/export/pub/software/data/db2/drivers/odbc_cli/linuxx64_odbc_cli.tar.gz && \
#     mkdir -p /opt/ibm/db2/clidriver && \
#     tar -xzf linuxx64_odbc_cli.tar.gz -C /opt/ibm/db2/clidriver --strip-components=1 && \
#     rm -f linuxx64_odbc_cli.tar.gz

# # Copy the application
# WORKDIR /app
# COPY --from=build /app .

# # Create the dllmap configuration file to map Windows DLLs to Linux .so files
# RUN echo '<?xml version="1.0" encoding="utf-8"?>\n\
# <configuration>\n\
#   <dllmap dll="db2app64.dll" target="libdb2.so" os="linux"/>\n\
# </configuration>' > /app/IBM.Data.Db2.dll.config

# # Create directory structure expected by .NET P/Invoke
# RUN mkdir -p /app/runtimes/win-x64/native

# # Copy DB2 libraries to all possible locations
# RUN cp /opt/ibm/db2/clidriver/lib/libdb2.so /app/runtimes/win-x64/native/db2app64.dll && \
#     cp /opt/ibm/db2/clidriver/lib/libdb2.so /usr/lib/db2app64.dll && \
#     mkdir -p /app/clidriver/bin && \
#     mkdir -p /app/clidriver/lib && \
#     cp -r /opt/ibm/db2/clidriver/lib/* /app/clidriver/lib/ && \
#     cp -r /opt/ibm/db2/clidriver/bin/* /app/clidriver/bin/

# # Set correct permissions
# RUN chmod 755 /app/clidriver/lib/*.so* && \
#     chmod 755 /app/runtimes/win-x64/native/db2app64.dll && \
#     chmod 755 /usr/lib/db2app64.dll

# # Set environment variables for DB2
# ENV DB2_CLI_DRIVER_INSTALL_PATH=/app/clidriver
# ENV IBM_DB_HOME=/app/clidriver
# ENV LD_LIBRARY_PATH=/app/clidriver/lib:/opt/ibm/db2/clidriver/lib:/usr/lib:${LD_LIBRARY_PATH}
# ENV PATH=/app/clidriver/bin:/opt/ibm/db2/clidriver/bin:${PATH}

# # Create directory for your config
# RUN mkdir -p /app/subsetter/toml-config/

# # Configure Linux to find the DB2 shared libraries
# RUN echo "/app/clidriver/lib" > /etc/ld.so.conf.d/db2clidriver.conf && \
#     echo "/opt/ibm/db2/clidriver/lib" >> /etc/ld.so.conf.d/db2clidriver.conf && \
#     ldconfig

# # Create comprehensive diagnostic and startup script
# RUN echo '#!/bin/bash\n\
# echo "=============== DB2 DRIVER DIAGNOSTIC INFORMATION -->>> V0.0.17 ==============="\n\
# echo "Current directory:"\n\
# pwd\n\
# echo "\\nOperating System:"\n\
# cat /etc/os-release\n\
# echo "\\nEnvironment variables:"\n\
# echo "LD_LIBRARY_PATH=$LD_LIBRARY_PATH"\n\
# echo "DB2_CLI_DRIVER_INSTALL_PATH=$DB2_CLI_DRIVER_INSTALL_PATH"\n\
# echo "IBM_DB_HOME=$IBM_DB_HOME"\n\
# echo "\\nContents of directories:"\n\
# echo "\\n/app:"\n\
# ls -la /app\n\
# echo "\\n/app/clidriver/lib:"\n\
# ls -la /app/clidriver/lib | head -n 20\n\
# echo "\\n/app/runtimes/win-x64/native:"\n\
# ls -la /app/runtimes/win-x64/native\n\
# echo "\\n/usr/lib (db2* files):"\n\
# ls -la /usr/lib/db2* 2>/dev/null || echo "No db2 files in /usr/lib"\n\
# echo "\\nSearching for libdb2.so:"\n\
# find / -name "libdb2.so" 2>/dev/null\n\
# echo "\\nSearching for db2app64.dll:"\n\
# find / -name "db2app64.dll" 2>/dev/null\n\
# echo "\\nFile type of db2app64.dll:"\n\
# file /app/runtimes/win-x64/native/db2app64.dll 2>/dev/null || echo "File not found"\n\
# echo "\\nFile type of /usr/lib/db2app64.dll:"\n\
# file /usr/lib/db2app64.dll 2>/dev/null || echo "File not found"\n\
# echo "\\nChecking config files:"\n\
# echo "IBM.Data.Db2.dll.config:"\n\
# cat /app/IBM.Data.Db2.dll.config 2>/dev/null || echo "Config file not found"\n\
# echo "\\nContents of /etc/ld.so.conf.d:"\n\
# cat /etc/ld.so.conf.d/db2clidriver.conf\n\
# echo "\\nLibrary dependencies of IBM.Data.Db2.dll:"\n\
# ldd /app/IBM.Data.Db2.dll 2>&1 || echo "Could not get dependencies"\n\
# echo "\\n=============== STARTING APPLICATION ==============="\n\
# dotnet Tonic.Subsetter.dll /app/subsetter/toml-config/tes_db2.toml\n' > /app/start.sh && \
#     chmod +x /app/start.sh

# ENTRYPOINT ["/app/start.sh"]


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

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

# Create custom NuGet.config to avoid Windows paths
RUN echo '<?xml version="1.0" encoding="utf-8"?> \
<configuration> \
  <packageSources> \
    <clear /> \
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" /> \
  </packageSources> \
  <fallbackPackageFolders> \
    <clear /> \
  </fallbackPackageFolders> \
  <config> \
    <add key="globalPackagesFolder" value="/root/.nuget/packages" /> \
  </config> \
</configuration>' > /root/NuGet.Config

# Use the custom NuGet config
ENV NUGET_CONFIG=/root/NuGet.Config

# Copy solution file if you have one
COPY *.sln ./

# Create directories for the projects
RUN mkdir -p Tonic.Subsetter Tonic.Common Tonic.Test

# Copy csproj files
COPY Tonic.Subsetter/*.csproj ./Tonic.Subsetter/
COPY Tonic.Common/*.csproj ./Tonic.Common/
COPY Tonic.Test/*.csproj ./Tonic.Test/

# Restore dependencies
RUN dotnet restore Tonic.Subsetter/Tonic.Subsetter.csproj --verbosity detailed
RUN dotnet restore Tonic.Common/Tonic.Common.csproj --verbosity detailed
RUN dotnet restore Tonic.Test/Tonic.Test.csproj --verbosity detailed

# Copy project files
COPY Tonic.Subsetter/. ./Tonic.Subsetter/
COPY Tonic.Common/. ./Tonic.Common/
COPY Tonic.Test/. ./Tonic.Test/

# Build and publish
RUN dotnet publish Tonic.Subsetter/Tonic.Subsetter.csproj -c Release -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

# Install required packages for Linux DB2 client
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
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

# Copy DB2 libraries to standard Linux locations
RUN mkdir -p /app/clidriver/lib && \
    cp -r /opt/ibm/db2/clidriver/lib/* /app/clidriver/lib/ && \
    mkdir -p /app/clidriver/bin && \
    cp -r /opt/ibm/db2/clidriver/bin/* /app/clidriver/bin/

# Set correct permissions
RUN chmod 755 /app/clidriver/lib/*.so*

# Set environment variables for DB2
ENV DB2_CLI_DRIVER_INSTALL_PATH=/app/clidriver
ENV IBM_DB_HOME=/app/clidriver
ENV LD_LIBRARY_PATH=/app/clidriver/lib:${LD_LIBRARY_PATH}
ENV PATH=/app/clidriver/bin:${PATH}

# Configure Linux to find the DB2 shared libraries
RUN echo "/app/clidriver/lib" > /etc/ld.so.conf.d/db2clidriver.conf && \
    ldconfig

# Create directory for your config
RUN mkdir -p /app/subsetter/toml-config/

# Create diagnostic and startup script
RUN echo '#!/bin/bash\n\
echo "=============== DB2 DRIVER DIAGNOSTIC INFORMATION V.0.0.17 ==============="\n\
echo "Current directory: $(pwd)"\n\
echo "\\nOperating System:"\n\
cat /etc/os-release\n\
echo "\\nEnvironment variables:"\n\
echo "LD_LIBRARY_PATH=$LD_LIBRARY_PATH"\n\
echo "DB2_CLI_DRIVER_INSTALL_PATH=$DB2_CLI_DRIVER_INSTALL_PATH"\n\
echo "IBM_DB_HOME=$IBM_DB_HOME"\n\
echo "\\nContents of /app:"\n\
ls -la /app | head -20\n\
echo "\\nDB2 Client libraries:"\n\
ls -la /app/clidriver/lib | head -20\n\
echo "\\nSearching for libdb2.so:"\n\
find / -name "libdb2.so" 2>/dev/null\n\
echo "\\nSearching for DB2 libraries:"\n\
find / -name "*db2*.so*" 2>/dev/null | head -20\n\
echo "\\n=============== STARTING APPLICATION ==============="\n\
dotnet Tonic.Subsetter.dll /app/subsetter/toml-config/tes_db2.toml\n' > /app/start.sh && \
    chmod +x /app/start.sh

ENTRYPOINT ["/app/start.sh"]