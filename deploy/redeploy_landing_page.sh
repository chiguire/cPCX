#!/usr/bin/bash

source ./secrets && \
ANSIBLE_PYTHON_INTERPRETER=auto_silent ansible-playbook -v -K -i inventory.ini playbooks/redeploy_landing_page.yml \
    -e "github_repo=${GITHUB_REPO}" \
    -e "emf_allowed_cidrs=${EMF_ALLOWED_CIDRS}"
