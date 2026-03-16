output "app_service_url" {
  description = "Default HTTPS URL of the cpcx App Service."
  value       = "https://${azurerm_linux_web_app.app.default_hostname}"
}

output "postgres_fqdn" {
  description = "Fully-qualified domain name of the PostgreSQL server."
  value       = azurerm_postgresql_flexible_server.db.fqdn
}

output "resource_group_name" {
  description = "Name of the created resource group."
  value       = azurerm_resource_group.main.name
}

output "nginx_public_ip" {
  description = "Public IP of the nginx VM. Point deerpost.cx, www.deerpost.cx, and emf.deerpost.cx here."
  value       = azurerm_public_ip.nginx.ip_address
}
