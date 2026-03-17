terraform {
  required_version = "1.14.7"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  backend "azurerm" {
    key = "cpcx-db.tfstate"
  }
}

provider "azurerm" {
  features {}
}
