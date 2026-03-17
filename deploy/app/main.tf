# ---------------------------------------------------------------------------
# Remote state — read database outputs from the db template
# ---------------------------------------------------------------------------

data "terraform_remote_state" "db" {
  backend = "azurerm"
  config = {
    resource_group_name  = var.tf_state_resource_group
    storage_account_name = var.tf_state_storage_account
    container_name       = var.tf_state_container
    key                  = "cpcx-db.tfstate"
  }
}

locals {
  rg_name     = data.terraform_remote_state.db.outputs.resource_group_name
  rg_location = data.terraform_remote_state.db.outputs.resource_group_location

  active_environments = length(var.target_environments) > 0 ? {
    for k, v in var.environments : k => v if contains(var.target_environments, k)
  } : var.environments
}

# ---------------------------------------------------------------------------
# nginx VM — networking (one per environment)
# ---------------------------------------------------------------------------

resource "azurerm_virtual_network" "nginx" {
  for_each            = local.active_environments
  name                = "cpcx-${each.key}-nginx-vnet"
  resource_group_name = local.rg_name
  location            = local.rg_location
  address_space       = ["10.0.0.0/16"]

  tags = { environment = each.key }
}

resource "azurerm_subnet" "nginx" {
  for_each             = local.active_environments
  name                 = "nginx-subnet"
  resource_group_name  = local.rg_name
  virtual_network_name = azurerm_virtual_network.nginx[each.key].name
  address_prefixes     = ["10.0.1.0/24"]
}

resource "azurerm_public_ip" "nginx" {
  for_each            = local.active_environments
  name                = "cpcx-${each.key}-nginx-pip"
  resource_group_name = local.rg_name
  location            = local.rg_location
  allocation_method   = "Static"
  sku                 = "Standard"

  tags = { environment = each.key }
}

resource "azurerm_network_security_group" "nginx" {
  for_each            = local.active_environments
  name                = "cpcx-${each.key}-nginx-nsg"
  resource_group_name = local.rg_name
  location            = local.rg_location

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

  tags = { environment = each.key }
}

resource "azurerm_network_interface" "nginx" {
  for_each            = local.active_environments
  name                = "cpcx-${each.key}-nginx-nic"
  resource_group_name = local.rg_name
  location            = local.rg_location

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.nginx[each.key].id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.nginx[each.key].id
  }

  tags = { environment = each.key }
}

resource "azurerm_network_interface_security_group_association" "nginx" {
  for_each                  = local.active_environments
  network_interface_id      = azurerm_network_interface.nginx[each.key].id
  network_security_group_id = azurerm_network_security_group.nginx[each.key].id
}

# ---------------------------------------------------------------------------
# nginx VM (one per environment)
# ---------------------------------------------------------------------------

resource "azurerm_linux_virtual_machine" "nginx" {
  for_each            = local.active_environments
  name                = "cpcx-${each.key}-nginx-vm"
  resource_group_name = local.rg_name
  location            = local.rg_location
  size                = each.value.nginx_vm_size
  admin_username      = "azureuser"

  network_interface_ids = [azurerm_network_interface.nginx[each.key].id]

  admin_ssh_key {
    username   = "azureuser"
    public_key = var.nginx_ssh_public_key
  }

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  source_image_reference {
    publisher = "Debian"
    offer     = "debian-13"
    sku       = "13-gen2"
    version   = "latest"
  }

  custom_data = base64encode(templatefile("${path.module}/cloud-init.yaml.tpl", {
    admin_ip             = var.admin_public_ip
    app_service_hostname = azurerm_linux_web_app.app[each.key].default_hostname
    certbot_email        = var.certbot_email
    apex_domain          = var.apex_domain
    www_domain           = each.value.www_domain
    app_domain           = each.value.app_domain
  }))

  tags = { environment = each.key }
}

# ---------------------------------------------------------------------------
# App Service Plan (one per environment)
# ---------------------------------------------------------------------------

resource "azurerm_service_plan" "main" {
  for_each            = local.active_environments
  name                = "cpcx-${each.key}-asp"
  resource_group_name = local.rg_name
  location            = local.rg_location
  os_type             = "Linux"
  sku_name            = each.value.app_service_sku

  tags = { environment = each.key }
}

# ---------------------------------------------------------------------------
# App Service — cpcx (.NET 10, one per environment)
# ---------------------------------------------------------------------------

resource "azurerm_linux_web_app" "app" {
  for_each            = local.active_environments
  name                = "cpcx-${each.key}-app"
  resource_group_name = local.rg_name
  location            = local.rg_location
  service_plan_id     = azurerm_service_plan.main[each.key].id
  https_only          = true

  site_config {
    always_on = true

    application_stack {
      dotnet_version = "10.0"
    }

    ip_restriction_default_action = "Deny"

    ip_restriction {
      name       = "nginx-only"
      ip_address = "${azurerm_public_ip.nginx[each.key].ip_address}/32"
      action     = "Allow"
      priority   = 100
    }
  }

  app_settings = {
    "ConnectionStrings__DefaultConnection" = "Host=${data.terraform_remote_state.db.outputs.postgres_fqdn};Port=5432;Database=${data.terraform_remote_state.db.outputs.database_names[each.key]};Username=${data.terraform_remote_state.db.outputs.postgres_admin_username};Password=${var.postgres_admin_password};SslMode=Require"

    "Cpcx__ActiveEventId"    = each.value.cpcx_active_event_id
    "Cpcx__EnableApi"        = tostring(each.value.cpcx_enable_api)
    "Cpcx__EnableSeed"       = tostring(each.value.cpcx_enable_seed)
    "ASPNETCORE_ENVIRONMENT" = "Production"
  }

  tags = { environment = each.key }
}

# ---------------------------------------------------------------------------
# PostgreSQL firewall — allow all App Service outbound IPs
# ---------------------------------------------------------------------------

locals {
  all_app_outbound_ips = distinct(flatten([
    for app in azurerm_linux_web_app.app : tolist(app.outbound_ip_address_list)
  ]))
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "app_service" {
  for_each  = toset(local.all_app_outbound_ips)
  name      = "app-ip-${replace(each.value, ".", "-")}"
  server_id = data.terraform_remote_state.db.outputs.postgres_server_id

  start_ip_address = each.value
  end_ip_address   = each.value
}
