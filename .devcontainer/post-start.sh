#!/usr/bin/env bash
# Runs every time the Codespace container starts (create, resume, rebuild).
#
# The dashboard's NEXT_PUBLIC_*_URL vars must point at each backend's PUBLIC
# forwarded Codespaces URL, not localhost — the browser loads the dashboard
# from https://<codespace>-3000.<domain>, and a client-side fetch to
# "localhost:5001" would hit the viewer's own machine, not this container.
set -euo pipefail
cd "$(dirname "$0")/.."

ENV_FILE="src/Web/Barakah.Admin/.env.local"

if [ -n "${CODESPACE_NAME:-}" ]; then
  DOMAIN="${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-app.github.dev}"
  base() { echo "https://${CODESPACE_NAME}-$1.${DOMAIN}"; }

  cat > "$ENV_FILE" <<EOF
NEXT_PUBLIC_IDENTITY_URL=$(base 5001)
NEXT_PUBLIC_TENANT_URL=$(base 5003)
NEXT_PUBLIC_CATALOG_URL=$(base 5004)
NEXT_PUBLIC_INVENTORY_URL=$(base 5005)
NEXT_PUBLIC_ORDER_URL=$(base 5006)
NEXT_PUBLIC_PAYMENT_URL=$(base 5007)
NEXT_PUBLIC_NOTIFICATION_URL=$(base 5011)
NEXT_PUBLIC_ANALYTICS_URL=$(base 5012)
EOF
  echo "==> Wrote $ENV_FILE for Codespaces forwarded URLs (restart 'npm run dev' if it was already running)"
else
  echo "==> Not running in Codespaces — leaving $ENV_FILE untouched"
fi
