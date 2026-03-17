output "resource_group_name" {
  description = "Name of the resource group (passed through from db remote state)."
  value       = local.rg_name
}

output "nginx_public_ips" {
  description = "Public IPs of the nginx VMs, keyed by environment. Point DNS records here."
  value       = { for k, pip in azurerm_public_ip.nginx : k => pip.ip_address }
}

output "app_service_urls" {
  description = "Default HTTPS URLs of the App Services, keyed by environment."
  value       = { for k, app in azurerm_linux_web_app.app : k => "https://${app.default_hostname}" }
}
