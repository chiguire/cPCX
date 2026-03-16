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
  description = "Base name used for all resources (App Service, PostgreSQL, etc.)."
  type        = string
  default     = "cpcx"
}

variable "environment" {
  description = "Deployment environment tag (e.g. dev, staging, prod)."
  type        = string
  default     = "prod"
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

variable "postgres_db_name" {
  description = "Name of the application database."
  type        = string
  default     = "cpcx"
}

variable "cpcx_active_event_id" {
  description = "Value for the Cpcx:ActiveEventId application setting."
  type        = string
  default     = "E26"
}

variable "cpcx_enable_api" {
  description = "Whether to enable the cpcx API controller (Cpcx:EnableApi)."
  type        = bool
  default     = false
}

variable "cpcx_enable_seed" {
  description = "Whether to seed the database with test users on startup (Cpcx:EnableSeed)."
  type        = bool
  default     = false
}

# ---------------------------------------------------------------------------
# nginx VM
# ---------------------------------------------------------------------------

variable "admin_public_ip" {
  description = "Your public IP address. Used to restrict SSH access and emf.deerpost.cx."
  type        = string
  default     = "2.100.169.251"
}

variable "nginx_vm_size" {
  description = "Azure VM size for the nginx reverse proxy."
  type        = string
  default     = "Standard_B1s"
}

variable "nginx_ssh_public_key" {
  description = "SSH public key (contents of id_rsa.pub or similar) for the nginx VM."
  type        = string
  sensitive   = true
}

variable "certbot_email" {
  description = "Email address for Let's Encrypt certificate expiry notifications."
  type        = string
}
