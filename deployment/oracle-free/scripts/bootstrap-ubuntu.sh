#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -eq 0 ]]; then
  echo "Bu betiği root yerine sudo yetkili normal kullanıcıyla çalıştırın." >&2
  exit 1
fi

sudo apt-get update
sudo DEBIAN_FRONTEND=noninteractive apt-get install -y \
  ca-certificates curl docker.io docker-compose-v2 fail2ban git jq rclone ufw

sudo usermod -aG docker "${USER}"
sudo systemctl enable --now docker fail2ban

sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw --force enable

sudo install -d -o "${USER}" -g "${USER}" -m 0750 /opt/systemcel /opt/systemcel/backups

if ! swapon --show | grep -q '/swapfile'; then
  sudo fallocate -l 2G /swapfile
  sudo chmod 600 /swapfile
  sudo mkswap /swapfile
  sudo swapon /swapfile
  grep -q '^/swapfile ' /etc/fstab || echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab >/dev/null
fi

echo "Hazır. Docker grup üyeliğinin geçerli olması için SSH oturumunu kapatıp yeniden açın."
