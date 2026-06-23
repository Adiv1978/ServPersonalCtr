#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ ! -f ".env.production" ]]; then
  echo "Falta el archivo .env.production"
  exit 1
fi

docker compose --env-file .env.production -f docker-compose.prod.yml build app
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --remove-orphans
docker image prune -f

echo "Despliegue completado."
