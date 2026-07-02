#!/usr/bin/bash

set -euo pipefail

if [[ -z "${ASN:-}" ]]; then
    echo "Error: ASN environment variable is not set" >&2
    exit 1
fi

API_URL="https://stat.ripe.net/data/announced-prefixes/data.json?resource=${ASN}"

if ! command -v jq &>/dev/null; then
    echo "Error: jq is required but not installed" >&2
    exit 1
fi

RESPONSE=$(curl -sf "${API_URL}") || {
    echo "Error: failed to fetch prefixes for ${ASN}" >&2
    exit 1
}

PREFIXES=$(echo "${RESPONSE}" | jq -r '.data.prefixes[].prefix' | tr '\n' ',' | sed 's/,$//')

if [[ -z "${PREFIXES}" ]]; then
    echo "Error: no prefixes returned for ${ASN}" >&2
    exit 1
fi

echo "${PREFIXES}"
