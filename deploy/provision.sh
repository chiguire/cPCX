#!/usr/bin/env bash

ansible-playbook -K -i inventory.ini playbooks/provision.yml \
    -e "github_ssh_private_key_path=${GITHUB_SSH_PRIVATE_KEY_PATH}" \
    -e "github_repo=${GITHUB_REPO}" \
    -e "certbot_email=${CERTBOT_EMAIL}" \
    -e "domain_apex=${DOMAIN_APEX}" \
    -e "domain_emf=${DOMAIN_EMF}" \
    -e "emf_allowed_cidrs=${EMF_ALLOWED_CIDRS}" \
    -e "postgres_db=${POSTGRES_DB}" \
    -e "postgres_user=${POSTGRES_USER}"