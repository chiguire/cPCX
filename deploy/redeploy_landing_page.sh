#!/usr/bin/bash

set -euo pipefail

source ./secrets

AS_PREFIXES=$(./get_as_prefixes.sh)
ALLOWED_CIDRS="${ALLOWED_CIDRS},${AS_PREFIXES}"

ANSIBLE_PYTHON_INTERPRETER=auto_silent ansible-playbook -v -K -i inventory.ini playbooks/redeploy_landing_page.yml \
    -e "github_repo=${GITHUB_REPO}" \
    -e "allowed_cidrs=${ALLOWED_CIDRS}"
