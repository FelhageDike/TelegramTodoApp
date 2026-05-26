#!/usr/bin/env bash
# Деплой только указанных сервисов (без postgres/rabbitmq/portainer).
# Usage: ./scripts/deploy-services.sh "bot bff"
set -euo pipefail

SERVICES="${1:-}"
if [[ -z "$SERVICES" ]]; then
  echo "Нет сервисов для деплоя — выход."
  exit 0
fi

APP_DIR="${APP_DIR:-$HOME/TelegramTodoApp}"
COMPOSE=(docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env)

cd "$APP_DIR"
git fetch origin main
git reset --hard origin/main
cd deploy

echo "=== Деплой: $SERVICES ==="
# БД должны быть запущены (образы, без сборки)
"${COMPOSE[@]}" up -d postgres rabbitmq

"${COMPOSE[@]}" build --no-deps $SERVICES
"${COMPOSE[@]}" up -d --no-build $SERVICES

echo "=== Статус ==="
"${COMPOSE[@]}" ps $SERVICES
