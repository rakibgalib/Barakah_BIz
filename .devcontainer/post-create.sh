#!/usr/bin/env bash
# Runs once when the Codespace container is first created.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> Setting up env files"
[ -f .env ] || cp .env.example .env
[ -f src/Web/Barakah.Admin/.env.local ] || cp src/Web/Barakah.Admin/.env.local.example src/Web/Barakah.Admin/.env.local

echo "==> Restoring .NET packages"
dotnet restore BarakahPlatform.sln

echo "==> Installing dashboard dependencies"
(cd src/Web/Barakah.Admin && npm install)

echo ""
echo "Setup complete. Next steps:"
echo "  1. docker compose up -d          # start Postgres, Kafka, and all 8 backend services"
echo "  2. cd src/Web/Barakah.Admin && npm run dev   # start the dashboard"
echo "  3. Open the forwarded port 3000 in the Ports tab (it should auto-open)"
