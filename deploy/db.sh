#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

usage() {
  echo "Usage: $0 <plan|apply|destroy>"
  exit 1
}

ACTION="${1:-}"

[[ -z "$ACTION" ]] && usage
[[ "$ACTION" != "plan" && "$ACTION" != "apply" && "$ACTION" != "destroy" ]] && usage

# shellcheck source=secrets
source "$SCRIPT_DIR/secrets"

cd "$SCRIPT_DIR/db"

terraform init
terraform "$ACTION"
