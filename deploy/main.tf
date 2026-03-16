locals {
  # Unique suffix to avoid global naming collisions on storage/postgres/app service names.
  suffix = "${var.app_name}-${var.environment}"
}

# ---------------------------------------------------------------------------
# Resource Group
# ---------------------------------------------------------------------------

resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
}

# ---------------------------------------------------------------------------
# nginx VM — networking
# ---------------------------------------------------------------------------

resource "azurerm_virtual_network" "nginx" {
  name                = "${local.suffix}-nginx-vnet"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  address_space       = ["10.0.0.0/16"]

  tags = { environment = var.environment }
}

resource "azurerm_subnet" "nginx" {
  name                 = "nginx-subnet"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.nginx.name
  address_prefixes     = ["10.0.1.0/24"]
}

resource "azurerm_public_ip" "nginx" {
  name                = "${local.suffix}-nginx-pip"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  allocation_method   = "Static"
  sku                 = "Standard"

  tags = { environment = var.environment }
}

resource "azurerm_network_security_group" "nginx" {
  name                = "${local.suffix}-nginx-nsg"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  security_rule {
    name                       = "allow-http"
    priority                   = 100
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "80"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "allow-https"
    priority                   = 110
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "443"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "allow-ssh-admin"
    priority                   = 120
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "22"
    source_address_prefix      = "${var.admin_public_ip}/32"
    destination_address_prefix = "*"
  }

  tags = { environment = var.environment }
}

resource "azurerm_network_interface" "nginx" {
  name                = "${local.suffix}-nginx-nic"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.nginx.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.nginx.id
  }

  tags = { environment = var.environment }
}

resource "azurerm_network_interface_security_group_association" "nginx" {
  network_interface_id      = azurerm_network_interface.nginx.id
  network_security_group_id = azurerm_network_security_group.nginx.id
}

# ---------------------------------------------------------------------------
# nginx VM
# ---------------------------------------------------------------------------

resource "azurerm_linux_virtual_machine" "nginx" {
  name                = "${local.suffix}-nginx-vm"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  size                = var.nginx_vm_size
  admin_username      = "azureuser"

  network_interface_ids = [azurerm_network_interface.nginx.id]

  admin_ssh_key {
    username   = "azureuser"
    public_key = var.nginx_ssh_public_key
  }

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "0001-com-ubuntu-server-jammy"
    sku       = "22_04-lts-gen2"
    version   = "latest"
  }

  custom_data = base64encode(templatefile("${path.module}/cloud-init.yaml.tpl", {
    admin_ip             = var.admin_public_ip
    app_service_hostname = azurerm_linux_web_app.app.default_hostname
    certbot_email        = var.certbot_email
  }))

  tags = { environment = var.environment }
}

# ---------------------------------------------------------------------------
# PostgreSQL Flexible Server — B1ms burstable, public + firewall
# ---------------------------------------------------------------------------

resource "azurerm_postgresql_flexible_server" "db" {
  name                   = "${local.suffix}-pg"
  resource_group_name    = azurerm_resource_group.main.name
  location               = azurerm_resource_group.main.location
  version                = "17"
  administrator_login    = var.postgres_admin_username
  administrator_password = var.postgres_admin_password

  sku_name   = "B_Standard_B1ms"
  storage_mb = 32768

  backup_retention_days        = 7
  geo_redundant_backup_enabled = false

  tags = {
    environment = var.environment
  }
}

resource "azurerm_postgresql_flexible_server_database" "app_db" {
  name      = var.postgres_db_name
  server_id = azurerm_postgresql_flexible_server.db.id
  collation = "en_US.utf8"
  charset   = "utf8"
}

# Allow outbound IPs of the App Service to reach PostgreSQL.
resource "azurerm_postgresql_flexible_server_firewall_rule" "app_service" {
  count     = length(azurerm_linux_web_app.app.outbound_ip_address_list)
  name      = "app-service-ip-${count.index}"
  server_id = azurerm_postgresql_flexible_server.db.id

  start_ip_address = tolist(azurerm_linux_web_app.app.outbound_ip_address_list)[count.index]
  end_ip_address   = tolist(azurerm_linux_web_app.app.outbound_ip_address_list)[count.index]
}

# ---------------------------------------------------------------------------
# App Service Plan — B1 Basic (Linux)
# ---------------------------------------------------------------------------

resource "azurerm_service_plan" "main" {
  name                = "${local.suffix}-asp"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = "B1"

  tags = {
    environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# App Service — cpcx (.NET 10)
# ---------------------------------------------------------------------------

resource "azurerm_linux_web_app" "app" {
  name                = "${local.suffix}-app"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id
  https_only          = true

  site_config {
    always_on = true

    application_stack {
      dotnet_version = "10.0"
    }

    ip_restriction_default_action = "Deny"

    ip_restriction {
      name       = "nginx-only"
      ip_address = "${azurerm_public_ip.nginx.ip_address}/32"
      action     = "Allow"
      priority   = 100
    }
  }

  app_settings = {
    # Connection string passed as an env var; ASP.NET Core reads ConnectionStrings__DefaultConnection.
    "ConnectionStrings__DefaultConnection" = "Host=${azurerm_postgresql_flexible_server.db.fqdn};Port=5432;Database=${var.postgres_db_name};Username=${var.postgres_admin_username};Password=${var.postgres_admin_password};SslMode=Require"

    "Cpcx__ActiveEventId" = var.cpcx_active_event_id
    "Cpcx__EnableApi"     = tostring(var.cpcx_enable_api)
    "Cpcx__EnableSeed"    = tostring(var.cpcx_enable_seed)

    # Disable EF migration endpoint outside development.
    "ASPNETCORE_ENVIRONMENT" = "Production"
  }

  tags = {
    environment = var.environment
  }
}
