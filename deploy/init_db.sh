#!/usr/bin/bash
# Create the cpcx database.
#   ./init_db.sh          first-time setup; refuses if a database already exists
#   ./init_db.sh --wipe   DELETES the existing database (asks you to type its name)
set -euo pipefail

source ./secrets

CONFIRM="no"
if [[ "${1:-}" == "--wipe" ]]; then
    echo "This will DELETE the production database at ${POSTGRES_DATA_PATH:?missing in secrets} on the VPS."
    read -r -p "Type the database name (${POSTGRES_DB:?missing in secrets}) to continue: " answer
    if [[ "$answer" != "${POSTGRES_DB}" ]]; then
        echo "Aborted." >&2
        exit 1
    fi
    CONFIRM="yes"
fi

ANSIBLE_PYTHON_INTERPRETER=auto_silent ansible-playbook -v -i inventory.ini playbooks/init_db.yml \
    -e "postgres_data_path=${POSTGRES_DATA_PATH}" \
    -e "confirm_wipe=${CONFIRM}"
