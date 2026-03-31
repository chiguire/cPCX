#!/usr/bin/env bash
# Usage: ./cpcxcli.sh <cpcxcli arguments...>
# Example: ./cpcxcli.sh user block alice --for 2h30m
set -euo pipefail

NETWORK=docker_backend

SCRIPT_DIR=$(dirname "$(realpath "$0")")
REPO_ROOT=$(realpath "$SCRIPT_DIR/..")

if [ -f "$SCRIPT_DIR/docker/.env" ]; then
  set -a
  # shellcheck disable=SC1091
  source "$SCRIPT_DIR/docker/.env"
  set +a
fi

POSTGRES_DB=${POSTGRES_DB:-cpcx}
POSTGRES_USER=${POSTGRES_USER:-cpcx}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}

docker run --rm \
  --network "$NETWORK" \
  -v "$REPO_ROOT:/src" \
  -e "CPCX_CONNECTION_STRING=Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}" \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  /bin/sh -c "
    set -e
    cd /src
    dotnet run --project cpcxcli -- $*
  "