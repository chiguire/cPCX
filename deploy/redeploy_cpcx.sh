#!/usr/bin/bash

source ./secrets

EXTRA_ARGS=""
if [[ "${1:-}" == "--reset-db" ]]; then
    echo "This will DELETE the production database at ${POSTGRES_DATA_PATH} on the VPS (there is no automatic backup)."
    read -r -p "Type the database name (${POSTGRES_DB}) to continue: " answer
    if [[ "$answer" != "${POSTGRES_DB}" ]]; then
        echo "Aborted." >&2
        exit 1
    fi
    EXTRA_ARGS="-e redeploy_db=true -e confirm_wipe=yes -e postgres_data_path=${POSTGRES_DATA_PATH} -K"
fi

ANSIBLE_PYTHON_INTERPRETER=auto_silent ansible-playbook -v -i inventory.ini playbooks/redeploy_cpcx.yml \
    -e "github_repo=${GITHUB_REPO}" \
    -e "smtp_host=${SMTP_HOST}" \
    -e "smtp_username=${SMTP_USERNAME}" \
    -e "smtp_password=${SMTP_PASSWORD}" \
    -e "caretaker_email=${CARETAKER_EMAIL}" \
    -e "data_protection_keys_path=${DATA_PROTECTION_KEYS_PATH}" \
    $EXTRA_ARGS
