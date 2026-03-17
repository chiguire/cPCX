variable "location" {
  description = "Azure region for all resources."
  type        = string
  default     = "uksouth"
}

variable "resource_group_name" {
  description = "Name of the resource group to create."
  type        = string
  default     = "cpcx-rg"
}

variable "app_name" {
  description = "Base name used for all resources."
  type        = string
  default     = "cpcx"
}

variable "postgres_admin_username" {
  description = "Administrator username for the PostgreSQL server."
  type        = string
  default     = "cpcxadmin"
}

variable "postgres_admin_password" {
  description = "Administrator password for the PostgreSQL server."
  type        = string
  sensitive   = true
}

variable "db_names" {
  description = "Map of environment name to database name."
  type        = map(string)
}
