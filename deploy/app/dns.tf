# ---------------------------------------------------------------------------
# Namecheap DNS — A records for www and app subdomains, one per environment
# Uses MERGE mode so unmanaged records on the domain are left untouched.
# ---------------------------------------------------------------------------

locals {
  # Build a flat map of { "<env>_www" => {hostname, ip}, "<env>_app" => ... }
  dns_records = merge([
    for env_key, env in local.active_environments : {
      "${env_key}_www" = {
        hostname = trimsuffix(env.www_domain, ".${var.apex_domain}")
        ip       = azurerm_public_ip.nginx[env_key].ip_address
      }
      "${env_key}_app" = {
        hostname = trimsuffix(env.app_domain, ".${var.apex_domain}")
        ip       = azurerm_public_ip.nginx[env_key].ip_address
      }
    }
  ]...)
}

resource "namecheap_domain_records" "main" {
  domain = var.apex_domain
  mode   = "MERGE"

  dynamic "record" {
    for_each = local.dns_records
    content {
      hostname = record.value.hostname
      type     = "A"
      address  = record.value.ip
      ttl      = 300
    }
  }
}
