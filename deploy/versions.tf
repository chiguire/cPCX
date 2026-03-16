terraform {
  required_version = "1.14.7"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  backend "azurerm" {
    # Populated at `terraform init` time via -backend-config flags (see bootstrap.sh).
    key = "cpcx.tfstate"
  }
}

provider "azurerm" {
  features {}
}
