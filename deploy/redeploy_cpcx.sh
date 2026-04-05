#!/usr/bin/bash

source ./secrets

EXTRA_ARGS=""
if [[ "$1" == "--reset-db" ]]; then
    EXTRA_ARGS="-e redeploy_db=true -e postgres_data_path=${POSTGRES_DATA_PATH} -K"
fi

ANSIBLE_PYTHON_INTERPRETER=auto_silent ansible-playbook -v -i inventory.ini playbooks/redeploy_cpcx.yml \
    -e "github_repo=${GITHUB_REPO}" \
    -e "smtp_host=${SMTP_HOST}" \
    -e "smtp_username=${SMTP_USERNAME}" \
    -e "smtp_password=${SMTP_PASSWORD}" \
    -e "caretaker_email=${CARETAKER_EMAIL}" \
    $EXTRA_ARGS
