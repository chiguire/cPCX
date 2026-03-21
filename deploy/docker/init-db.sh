#!/usr/bin/env bash
# Usage: ./init-db.sh <MigrationName>
#
# Starts postgres, adds a new EF Core migration (files are written back to the
# host via a volume mount), then applies all pending migrations.
set -euo pipefail

MIGRATION_NAME=${1:?Usage: $0 <MigrationName>}

# ---------------------------------------------------------------------------
# Load .env if present
# ---------------------------------------------------------------------------
if [ -f .env ]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi

POSTGRES_DB=${POSTGRES_DB:-cpcx}
POSTGRES_USER=${POSTGRES_USER:-cpcx}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-cpcxdev}
CPCX_PROJECT_PATH=${CPCX_PROJECT_PATH:-cpcx}

# Use a fixed project name so the network name is predictable.
COMPOSE_PROJECT=cpcx
NETWORK=${COMPOSE_PROJECT}_backend

REPO_ROOT=$(realpath "$(dirname "$0")/../..")

# ---------------------------------------------------------------------------
# 1. Start postgres
# ---------------------------------------------------------------------------
echo "==> Starting postgres..."
docker-compose -p "$COMPOSE_PROJECT" -f docker-compose.dev.yml up -d postgres

# ---------------------------------------------------------------------------
# 2. Wait for postgres to accept connections
# ---------------------------------------------------------------------------
echo "==> Waiting for postgres to be ready..."
until docker compose -p "$COMPOSE_PROJECT" -f docker-compose.dev.yml \
    exec -T postgres pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
    > /dev/null 2>&1; do
  sleep 1
done
echo "    postgres is ready."

# ---------------------------------------------------------------------------
# 3. Add migration + update database
#    The repo root is mounted at /src so EF Core can write migration files
#    back to the host.
# ---------------------------------------------------------------------------
echo "==> Adding migration '${MIGRATION_NAME}' and updating database..."
docker run --rm \
  --network "$NETWORK" \
  -v "$REPO_ROOT:/src" \
  -e "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}" \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  /bin/sh -c "
    set -e
    dotnet tool install --global dotnet-ef --version '10.*' 2>/dev/null || true
    export PATH=\"\$PATH:/root/.dotnet/tools\"
    cd /src
    dotnet ef migrations add '${MIGRATION_NAME}' --project '${CPCX_PROJECT_PATH}'
    dotnet ef database update --project '${CPCX_PROJECT_PATH}'
  "

echo "==> Taking down containers..."
docker-compose -p "$COMPOSE_PROJECT" -f docker-compose.dev.yml down

echo "==> Done."
