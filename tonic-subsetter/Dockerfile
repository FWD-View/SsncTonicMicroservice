FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

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

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Tonic.Subsetter.dll"]

