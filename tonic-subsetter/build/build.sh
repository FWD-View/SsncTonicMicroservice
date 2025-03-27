#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

dotnet publish "$DIR"/../Tonic.Subsetter
mkdir -p subsetter_docker_context
cp -a "$DIR"/../Tonic.Subsetter/bin/Debug/net6.0/publish/. subsetter_docker_context/
docker build -f Dockerfile.subsetter -t subsetter subsetter_docker_context

dotnet publish "$DIR"/../Tonic.PrimaryKeyRemapper
mkdir -p pkremapper_docker_context
cp -a "$DIR"/../Tonic.PrimaryKeyRemapper/bin/Debug/net6.0/publish/. pkremapper_docker_context/
docker build -f Dockerfile.pkremapper -t pkremapper pkremapper_docker_context

dotnet publish "$DIR"/../Tonic.ResetSchema
mkdir -p resetschema_docker_context
cp -a "$DIR"/../Tonic.ResetSchema/bin/Debug/net6.0/publish/. resetschema_docker_context/
docker build -f Dockerfile.resetschema -t resetschema resetschema_docker_context

dotnet publish "$DIR"/../Tonic.TableCount
mkdir -p tablecount_docker_context
cp -a "$DIR"/../Tonic.TableCount/bin/Debug/net6.0/publish/. tablecount_docker_context/
docker build -f Dockerfile.tablecount -t tablecount tablecount_docker_context

dotnet publish "$DIR"/../Tonic.ForeignKeyMaskConfigure
mkdir -p foreignkeymaskconfigure_docker_context
cp -a "$DIR"/../Tonic.ForeignKeyMaskConfigure/bin/Debug/net6.0/publish/. foreignkeymaskconfigure_docker_context/
docker build -f Dockerfile.foreignkeymaskconfigure -t foreignkeymaskconfigure foreignkeymaskconfigure_docker_context
