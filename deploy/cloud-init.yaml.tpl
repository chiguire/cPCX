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
          server_name deerpost.cx www.deerpost.cx emf.deerpost.cx;

          location /.well-known/acme-challenge/ {
              root /var/www/certbot;
          }

          location / {
              return 301 https://$host$request_uri;
          }
      }

      # deerpost.cx — redirect to www
      server {
          listen 443 ssl;
          server_name deerpost.cx;

          ssl_certificate     /etc/letsencrypt/live/deerpost.cx/fullchain.pem;
          ssl_certificate_key /etc/letsencrypt/live/deerpost.cx/privkey.pem;

          return 301 https://www.deerpost.cx$request_uri;
      }

      # www.deerpost.cx — static site
      server {
          listen 443 ssl;
          server_name www.deerpost.cx;

          ssl_certificate     /etc/letsencrypt/live/deerpost.cx/fullchain.pem;
          ssl_certificate_key /etc/letsencrypt/live/deerpost.cx/privkey.pem;

          root  /var/www/deerpost;
          index index.html;
      }

      # emf.deerpost.cx — reverse proxy, admin IP only
      server {
          listen 443 ssl;
          server_name emf.deerpost.cx;

          ssl_certificate     /etc/letsencrypt/live/deerpost.cx/fullchain.pem;
          ssl_certificate_key /etc/letsencrypt/live/deerpost.cx/privkey.pem;

          if ($admin_allowed = 0) {
              return 302 https://www.deerpost.cx;
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
      -d deerpost.cx -d www.deerpost.cx -d emf.deerpost.cx \
      --non-interactive --agree-tos -m ${certbot_email} || \
      echo "certbot failed — run manually after DNS propagation"