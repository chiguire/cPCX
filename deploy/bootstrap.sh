#!/bin/bash
# Creates the Azure storage account and container used to store Terraform state.
# Run once before `terraform init`. Requires the Azure CLI to be logged in.
set -e

RESOURCE_GROUP="${TF_STATE_RESOURCE_GROUP:-cpcx-tfstate-rg}"
LOCATION="${TF_STATE_LOCATION:-uksouth}"
STORAGE_ACCOUNT="${TF_STATE_STORAGE_ACCOUNT:-cpcxtfstate}"   # must be globally unique, 3-24 lowercase alphanumeric
CONTAINER="${TF_STATE_CONTAINER:-tfstate}"

echo "Creating resource group '$RESOURCE_GROUP'..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

echo "Creating storage account '$STORAGE_ACCOUNT'..."
az storage account create \
  --name "$STORAGE_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --output none

echo "Creating blob container '$CONTAINER'..."
az storage container create \
  --name "$CONTAINER" \
  --account-name "$STORAGE_ACCOUNT" \
  --auth-mode login \
  --output none

echo ""
echo "Bootstrap complete. Add the following backend configuration to terraform init:"
echo ""
echo "  terraform init \\"
echo "    -backend-config=\"resource_group_name=$RESOURCE_GROUP\" \\"
echo "    -backend-config=\"storage_account_name=$STORAGE_ACCOUNT\" \\"
echo "    -backend-config=\"container_name=$CONTAINER\" \\"
echo "    -backend-config=\"key=cpcx.tfstate\""
