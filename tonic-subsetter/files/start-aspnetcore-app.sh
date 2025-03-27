#!/bin/bash
# --------------------------------------
# Description : Script for starting and ASP.NET Core web application.
# --------------------------------------

SCRIPT_VERSION="2020080501"

if [ $# -eq 1 ]; then

    # if `docker run` only has one arguments, we assume user is running alternate command like `bash` to inspect the image
    exec "$@"

elif [ -z ${DOTNET_ARGS} ]; then

    # if `docker run` does not define 'DOTNET_ARGS' environment variable, we assume user is running alternate command like `ls -la`
    exec "$@"
else

    # if `docker run` defines 'DOTNET_ARGS' environment variable, we assume user wants to start up a web application
    dotnet ${DOTNET_ARGS}
fi
