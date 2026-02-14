#!/bin/bash
# =============================================================
# AR4CT Migration Script
# Old Server: 188.245.252.3
# New Server: 89.167.2.114 (IPv4) / 2a01:4f9:c012:77bd::/64 (IPv6)
# =============================================================

set -e

OLD_SERVER="root@188.245.252.3"
NEW_SERVER="root@89.167.2.114"

echo "============================================"
echo "  AR4CT Server Migration"
echo "  Old: 188.245.252.3"
echo "  New: 89.167.2.114"
echo "============================================"
echo ""

# ----------------------------------------------------------
# PRE-CHECK: Verify SSH connectivity to both servers
# ----------------------------------------------------------
echo ">>> Pre-check: Testing SSH connectivity..."
echo "  Testing old server..."
ssh -o ConnectTimeout=5 $OLD_SERVER "echo 'Old server OK'" || { echo "ERROR: Cannot reach old server"; exit 1; }
echo "  Testing new server..."
ssh -o ConnectTimeout=5 $NEW_SERVER "echo 'New server OK'" || { echo "ERROR: Cannot reach new server"; exit 1; }
echo ""

# ----------------------------------------------------------
# STEP 1: Setup new server (clean Ubuntu)
# ----------------------------------------------------------
echo ">>> Step 1: Setting up new server (clean Ubuntu)..."
ssh $NEW_SERVER bash -s <<'SETUP'
  set -e
  export DEBIAN_FRONTEND=noninteractive

  # Update system (noninteractive to avoid prompts on clean Ubuntu)
  apt-get update
  apt-get upgrade -y -o Dpkg::Options::="--force-confdef" -o Dpkg::Options::="--force-confold"

  # Install essential tools
  apt-get install -y curl wget git rsync ufw

  # Install Docker
  if ! command -v docker &> /dev/null; then
    echo "Installing Docker..."
    curl -fsSL https://get.docker.com | sh
    systemctl enable docker
    systemctl start docker
  else
    echo "Docker already installed."
  fi

  # Verify Docker Compose plugin (included with modern Docker)
  if ! docker compose version &> /dev/null; then
    echo "Installing Docker Compose plugin..."
    apt-get install -y docker-compose-plugin
  else
    echo "Docker Compose already available."
  fi

  # Install Nginx
  if ! command -v nginx &> /dev/null; then
    echo "Installing Nginx..."
    apt-get install -y nginx
    systemctl enable nginx
    systemctl start nginx
  else
    echo "Nginx already installed."
  fi

  # Install Certbot
  if ! command -v certbot &> /dev/null; then
    echo "Installing Certbot..."
    apt-get install -y certbot python3-certbot-nginx
  else
    echo "Certbot already installed."
  fi

  # Configure firewall
  ufw allow OpenSSH
  ufw allow 'Nginx Full'
  echo "y" | ufw enable 2>/dev/null || true

  # Create project directory
  mkdir -p /opt/ar4ct

  echo ""
  echo "=== New server setup complete! ==="
  echo "Docker: $(docker --version)"
  echo "Docker Compose: $(docker compose version)"
  echo "Nginx: $(nginx -v 2>&1)"
  echo "Certbot: $(certbot --version 2>&1)"
SETUP

# ----------------------------------------------------------
# STEP 2: Sync project files from LOCAL to new server
#   (fresher than old server; excludes dev artifacts)
# ----------------------------------------------------------
echo ""
echo ">>> Step 2: Syncing project files to new server..."

echo "  2a. Uploading local WebApp to new server..."
cd /Users/simongraeber/UnityProjects/AR4CT/WebApp
rsync -avz --progress \
  --exclude 'node_modules' \
  --exclude '.venv' \
  --exclude '__pycache__' \
  --exclude 'data' \
  --exclude '.git' \
  --exclude '*.log' \
  --exclude 'dist' \
  --exclude 'migrate.sh' \
  ./ $NEW_SERVER:/opt/ar4ct/

# ----------------------------------------------------------
# STEP 3: Export and migrate Docker volumes (data)
# ----------------------------------------------------------
echo ""
echo ">>> Step 3: Migrating Docker volume data..."

# Export volume from old server
echo "  3a. Exporting ar4ct_data volume from old server..."
ssh $OLD_SERVER "docker run --rm -v ar4ct_data:/data -v /tmp:/backup alpine tar czf /backup/ar4ct_data.tar.gz -C /data ." || {
  echo "  WARNING: No ar4ct_data volume on old server (or Docker not running). Skipping data migration."
  echo "  You can migrate data manually later."
  SKIP_VOLUME=true
}

if [ "${SKIP_VOLUME}" != "true" ]; then
  # Download volume backup
  echo "  3b. Downloading volume backup..."
  mkdir -p /tmp/ar4ct-migration
  scp $OLD_SERVER:/tmp/ar4ct_data.tar.gz /tmp/ar4ct-migration/ar4ct_data.tar.gz

  # Upload to new server
  echo "  3c. Uploading volume backup to new server..."
  scp /tmp/ar4ct-migration/ar4ct_data.tar.gz $NEW_SERVER:/tmp/ar4ct_data.tar.gz

  # Import volume on new server
  echo "  3d. Importing volume on new server..."
  ssh $NEW_SERVER bash -s <<'VOLUME'
    set -e
    # Create the volume
    docker volume create ar4ct_data
    # Restore data into the volume
    docker run --rm -v ar4ct_data:/data -v /tmp:/backup alpine sh -c "cd /data && tar xzf /backup/ar4ct_data.tar.gz"
    rm -f /tmp/ar4ct_data.tar.gz
    echo "Volume data restored!"
VOLUME
fi

# ----------------------------------------------------------
# STEP 4: Setup Nginx config (HTTP-only initially for certbot)
# ----------------------------------------------------------
echo ""
echo ">>> Step 4: Setting up Nginx configuration..."

# On a clean server, we first need HTTP-only config for certbot to work.
# We'll install a minimal config, get certs, then install the full config.
ssh $NEW_SERVER bash -s <<'NGINX'
  set -e

  # Create a temporary HTTP-only nginx config for certbot verification
  cat > /etc/nginx/sites-available/ar4ct <<'EOF'
# AR4CT - Temporary HTTP-only config (for initial SSL setup)

server {
    listen 80;
    server_name ar4ct.com www.ar4ct.com;

    location / {
        proxy_pass http://127.0.0.1:3000/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}

server {
    listen 80;
    server_name api.ar4ct.com;

    client_max_body_size 500M;
    proxy_connect_timeout 300;
    proxy_send_timeout 300;
    proxy_read_timeout 300;

    location / {
        proxy_pass http://127.0.0.1:8000/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
EOF

  # Enable the site
  ln -sf /etc/nginx/sites-available/ar4ct /etc/nginx/sites-enabled/ar4ct
  rm -f /etc/nginx/sites-enabled/default

  # Test and reload
  nginx -t
  systemctl reload nginx
  echo "Nginx HTTP config installed (ready for certbot)."
NGINX

# ----------------------------------------------------------
# STEP 5: Build and start Docker containers on new server
# ----------------------------------------------------------
echo ""
echo ">>> Step 5: Building and starting Docker containers..."

ssh $NEW_SERVER bash -s <<'BUILD'
  set -e
  cd /opt/ar4ct
  docker compose -f docker-compose.prod.yml up -d --build
  echo ""
  echo "Containers started:"
  docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep ar4ct || true
  echo ""
  # Wait a moment for containers to be ready
  sleep 5
  echo "Testing endpoints..."
  curl -sf http://localhost:3000 > /dev/null && echo "  Client (port 3000): OK" || echo "  Client (port 3000): FAILED"
  curl -sf http://localhost:8000/hello > /dev/null && echo "  Server (port 8000): OK" || echo "  Server (port 8000): FAILED (may need /docs or /)"
BUILD

# ----------------------------------------------------------
# STEP 6: Reload Nginx (verify it works with containers)
# ----------------------------------------------------------
echo ""
echo ">>> Step 6: Verifying Nginx..."

ssh $NEW_SERVER bash -s <<'NGINXSTART'
  set -e
  nginx -t && systemctl reload nginx
  echo "Nginx running and proxying to Docker containers!"
NGINXSTART

# ----------------------------------------------------------
# DONE
# ----------------------------------------------------------
echo ""
echo "============================================"
echo "  Migration complete!"
echo "============================================"
echo ""
echo "NEXT STEPS (manual):"
echo ""
echo "1. UPDATE DNS RECORDS (you said you'll handle this):"
echo "   Point these to 89.167.2.114:"
echo "     ar4ct.com       A     89.167.2.114"
echo "     www.ar4ct.com   A     89.167.2.114"
echo "     api.ar4ct.com   A     89.167.2.114"
echo "   Optionally also add AAAA records:"
echo "     ar4ct.com       AAAA  2a01:4f9:c012:77bd::1"
echo "     www.ar4ct.com   AAAA  2a01:4f9:c012:77bd::1"
echo "     api.ar4ct.com   AAAA  2a01:4f9:c012:77bd::1"
echo ""
echo "2. GET SSL CERTIFICATES (after DNS propagates to 89.167.2.114):"
echo "   ssh root@89.167.2.114"
echo "   certbot --nginx -d ar4ct.com -d www.ar4ct.com -d api.ar4ct.com"
echo "   (certbot will auto-update the nginx config with HTTPS)"
echo ""
echo "3. VERIFY everything works:"
echo "   curl https://ar4ct.com"
echo "   curl https://api.ar4ct.com/hello"
echo ""
echo "4. SHUT DOWN old server (once everything works):"
echo "   ssh root@188.245.252.3"
echo "   cd /opt/ar4ct && docker compose -f docker-compose.prod.yml down"
echo ""
echo "5. CLEANUP local temp files:"
echo "   rm -rf /tmp/ar4ct-migration"
echo ""
