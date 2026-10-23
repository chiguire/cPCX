#!/usr/bin/bash
# Provision cpcx on a VPS that chiguire-vps has ALREADY set up (nginx, ufw,
# certbot, ssh hardening). Safe to re-run: never deletes data. See vps_bootstrap.md.
set -euo pipefail

source ./secrets

ANSIBLE_PYTHON_INTERPRETER=auto_silent ansible-playbook -v -i inventory.ini playbooks/provision.yml \
    -e "github_ssh_private_key_path=${GITHUB_SSH_PRIVATE_KEY_PATH:?missing in secrets}" \
    -e "github_repo=${GITHUB_REPO:?missing in secrets}" \
    -e "certbot_email=${CERTBOT_EMAIL:?missing in secrets}" \
    -e "domain_root=${DOMAIN_ROOT:?missing in secrets}" \
    -e "domain_apex=${DOMAIN_APEX:?missing in secrets}" \
    -e "domain_cpcx=${DOMAIN_CPCX:?missing in secrets}" \
    -e "postgres_db=${POSTGRES_DB:?missing in secrets}" \
    -e "postgres_user=${POSTGRES_USER:?missing in secrets}" \
    -e "postgres_data_path=${POSTGRES_DATA_PATH:?missing in secrets}" \
    -e "data_protection_keys_path=${DATA_PROTECTION_KEYS_PATH:-}" \
    -e "compose_file=${COMPOSE_FILE:-docker-compose.yml}" \
    "$@"
