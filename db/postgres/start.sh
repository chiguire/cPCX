#!/bin/bash
set -e
CONTAINER_NAME="cpcx-postgres"
IMAGE_NAME="cpcx-postgres"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    echo "Removing existing container..."
    docker rm -f "$CONTAINER_NAME"
fi

echo "Building image..."
docker build -t "$IMAGE_NAME" "$SCRIPT_DIR"

echo "Starting PostgreSQL on 127.0.0.1:5432..."
docker run -d \
    --name "$CONTAINER_NAME" \
    -p 127.0.0.1:5432:5432 \
    "$IMAGE_NAME"

echo "Done. PostgreSQL is available at localhost:5432"
