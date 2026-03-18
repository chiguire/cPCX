terraform {
  required_version = "1.14.7"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    namecheap = {
      source  = "namecheap/namecheap"
      version = "~> 2.1"
    }
  }

  backend "azurerm" {
    key = "cpcx-app.tfstate"
  }
}

provider "azurerm" {
  features {}
}

provider "namecheap" {
  user_name = var.namecheap_username
  api_user  = var.namecheap_username
  api_key   = var.namecheap_api_key
  client_ip = var.admin_public_ip
}
