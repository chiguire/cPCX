#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

usage() {
  echo "Usage: $0 <environment> <plan|apply>"
  exit 1
}

ENV="${1:-}"
ACTION="${2:-}"

[[ -z "$ENV" || -z "$ACTION" ]] && usage
[[ "$ACTION" != "plan" && "$ACTION" != "apply" ]] && usage

# shellcheck source=secrets
source "$SCRIPT_DIR/secrets"

cd "$SCRIPT_DIR/app"

terraform "$ACTION" -var="target_environments=[\"$ENV\"]"

if [[ "$ACTION" == "apply" ]]; then
  APP_NAME="cpcx-${ENV}-app"
  RESOURCE_GROUP="$(terraform output -raw resource_group_name)"
  PUBLISH_DIR="$(mktemp -d)"

  echo "Publishing cpcx to $APP_NAME..."
  dotnet publish "$SCRIPT_DIR/../cpcx/cpcx.csproj" \
    --configuration Release \
    --output "$PUBLISH_DIR"

  (cd "$PUBLISH_DIR" && zip -r ../cpcx-publish.zip .)

  az webapp deploy \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_NAME" \
    --src-path "$(dirname "$PUBLISH_DIR")/cpcx-publish.zip" \
    --type zip

  rm -rf "$PUBLISH_DIR" "$(dirname "$PUBLISH_DIR")/cpcx-publish.zip"
  echo "Deployment complete."
fi
