#cloud-config
package_update: true
package_upgrade: true
packages:
  - nginx
  - certbot
  - python3-certbot-nginx

write_files:
  - path: /var/www/deerpost/index.html
    permissions: "0644"
    content: |
      <!DOCTYPE html>
      <html lang="en">
      <head><meta charset="UTF-8"><title>DeerPost</title></head>
      <body><h1>Hello World</h1></body>
      </html>

  - path: /etc/nginx/conf.d/deerpost.conf
    permissions: "0644"
    content: |
      geo $admin_allowed {
          default 0;
          ${admin_ip}/32 1;
      }

      # HTTP — ACME challenge passthrough, redirect everything else to HTTPS
      server {
          listen 80;
          server_name ${apex_domain} ${www_domain} ${app_domain};

          location /.well-known/acme-challenge/ {
              root /var/www/certbot;
          }

          location / {
              return 301 https://$host$request_uri;
          }
      }

      # ${apex_domain} — redirect to www
      server {
          listen 443 ssl;
          server_name ${apex_domain};

          ssl_certificate     /etc/letsencrypt/live/${apex_domain}/fullchain.pem;
          ssl_certificate_key /etc/letsencrypt/live/${apex_domain}/privkey.pem;

          return 301 https://${www_domain}$request_uri;
      }

      # ${www_domain} — static site
      server {
          listen 443 ssl;
          server_name ${www_domain};

          ssl_certificate     /etc/letsencrypt/live/${apex_domain}/fullchain.pem;
          ssl_certificate_key /etc/letsencrypt/live/${apex_domain}/privkey.pem;

          root  /var/www/deerpost;
          index index.html;
      }

      # ${app_domain} — reverse proxy, admin IP only
      server {
          listen 443 ssl;
          server_name ${app_domain};

          ssl_certificate     /etc/letsencrypt/live/${apex_domain}/fullchain.pem;
          ssl_certificate_key /etc/letsencrypt/live/${apex_domain}/privkey.pem;

          if ($admin_allowed = 0) {
              return 302 https://${www_domain};
          }

          location / {
              proxy_pass         https://${app_service_hostname};
              proxy_set_header   Host              ${app_service_hostname};
              proxy_set_header   X-Real-IP         $remote_addr;
              proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
              proxy_set_header   X-Forwarded-Proto https;
          }
      }

runcmd:
  - mkdir -p /var/www/certbot
  - rm -f /etc/nginx/sites-enabled/default
  - nginx -t && systemctl enable nginx && systemctl start nginx
  - |
    certbot --nginx \
      -d ${apex_domain} -d ${www_domain} -d ${app_domain} \
      --non-interactive --agree-tos -m ${certbot_email} || \
      echo "certbot failed — run manually after DNS propagation"
