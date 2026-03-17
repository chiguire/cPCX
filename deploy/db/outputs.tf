output "postgres_server_id" {
  description = "Resource ID of the PostgreSQL server (used by the app template for firewall rules)."
  value       = azurerm_postgresql_flexible_server.db.id
}

output "postgres_fqdn" {
  description = "FQDN of the PostgreSQL server."
  value       = azurerm_postgresql_flexible_server.db.fqdn
}

output "postgres_admin_username" {
  description = "PostgreSQL administrator username."
  value       = var.postgres_admin_username
}

output "database_names" {
  description = "Map of environment name to database name."
  value       = var.db_names
}

output "resource_group_name" {
  description = "Name of the resource group."
  value       = azurerm_resource_group.main.name
}

output "resource_group_location" {
  description = "Location of the resource group."
  value       = azurerm_resource_group.main.location
}
