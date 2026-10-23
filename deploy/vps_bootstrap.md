# Bootstrapping a fresh VPS for cpcx

cpcx shares the VPS with the static sites, Gemini and finger. The shared base
(SSH hardening, ufw, nginx, certbot, logrotate, the deploy user) is owned by
the **chiguire-vps** Ansible project; this folder only adds cpcx on top.
Do these in order.

## 0. Prerequisites (local machine)

```
pip install --break-system-packages ansible
ansible-galaxy collection install community.docker community.general ansible.posix
```

On the new VPS (Debian 12): a sudo-capable user with your SSH key
(`debian` on OVHcloud) and key-based login working.

Point DNS for the cpcx names (`DOMAIN_ROOT`, `DOMAIN_APEX`, `DOMAIN_CPCX`
in `secrets`) at the VPS **before step 3** - Let's Encrypt validates them.

## 1. Base system (chiguire-vps)

```
cd ../chiguire-vps/ansible        # adjust to where you keep it
ansible-playbook site.yml
```

`provision.yml` checks for this and stops with a clear message if it is missing.

## 2. Secrets

```
cp secrets.example secrets        # then fill in every value
```

Use a **read-only GitHub deploy key for the cpcx repo** for
`GITHUB_SSH_PRIVATE_KEY_PATH` (repo -> Settings -> Deploy keys, leave "Allow
write access" off), never a personal key.

## 3. Provision cpcx

```
./provision.sh
```

Installs Docker + rootless Docker, clones the app, writes `.env` (only if it
does not exist), pulls images, installs the user service, and obtains the
deerpost certificate on first run. It never deletes data and is safe to re-run.

## 4. Create the database (first time only)

```
./init_db.sh
```

Refuses to run if a database already exists. To reset one on purpose:
`./init_db.sh --wipe` (asks you to type the database name). Back it up first.

## 5. Landing page + nginx vhost, then start the app

```
./redeploy_landing_page.sh
./redeploy_cpcx.sh
```

## Day to day

- App update: `./redeploy_cpcx.sh`
- Landing page / vhost: `./redeploy_landing_page.sh`
- Everything else on the box (nginx, TLS renewal, firewall, SSH, logs):
  the chiguire-vps project.
