variable "postgres_admin_password" {
  description = "Administrator password for the PostgreSQL server."
  type        = string
  sensitive   = true
}

# ---------------------------------------------------------------------------
# Remote state — points to the db template's backend
# ---------------------------------------------------------------------------

variable "tf_state_resource_group" {
  description = "Resource group containing the Terraform state storage account."
  type        = string
  default     = "cpcx-tfstate-rg"
}

variable "tf_state_storage_account" {
  description = "Storage account name for Terraform state."
  type        = string
  default     = "cpcxtfstate"
}

variable "tf_state_container" {
  description = "Blob container name for Terraform state."
  type        = string
  default     = "tfstate"
}

# ---------------------------------------------------------------------------
# Shared across all environments
# ---------------------------------------------------------------------------

variable "nginx_ssh_public_key" {
  description = "SSH public key installed on all nginx VMs."
  type        = string
  sensitive   = true
}

variable "certbot_email" {
  description = "Email address for Let's Encrypt certificate notifications."
  type        = string
}

variable "admin_public_ip" {
  description = "Your public IP. Restricts SSH access to nginx VMs and emf subdomain access."
  type        = string
  default     = "2.100.169.251"
}

variable "apex_domain" {
  description = "Apex domain shared by all environments (e.g. deerpost.cx)."
  type        = string
  default     = "deerpost.cx"
}

# ---------------------------------------------------------------------------
# Per-environment configuration
# ---------------------------------------------------------------------------

variable "target_environments" {
  description = "Optional subset of environment names to deploy. When empty (default) all environments are deployed."
  type        = set(string)
  default     = []
}

variable "environments" {
  description = "Map of environment name to per-environment configuration. Keys must match those used in the db template."
  type = map(object({
    www_domain           = string
    app_domain           = string
    nginx_vm_size        = optional(string, "Standard_B1s")
    app_service_sku      = optional(string, "B1")
    cpcx_active_event_id = optional(string, "E26")
    cpcx_enable_api      = optional(bool, false)
    cpcx_enable_seed     = optional(bool, false)
  }))
}
