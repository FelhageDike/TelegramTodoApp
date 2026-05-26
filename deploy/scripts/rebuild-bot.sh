#!/usr/bin/env bash
# Только Telegram-бот: pull + build bot + up (без сборки bff и микросервисов).
set -euo pipefail
cd "$(dirname "$0")/.."
git -C ~/TelegramTodoApp pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env build bot
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env up -d --no-build bot
docker logs deploy-bot-1 --tail 25
