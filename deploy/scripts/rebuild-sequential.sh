#!/usr/bin/env bash
# Остановить стек, очистить кэш сборки, поднять сервисы по одному (--build).
#
#   cd ~/TelegramTodoApp/deploy
#   chmod +x scripts/rebuild-sequential.sh
#   ./scripts/rebuild-sequential.sh
#
#   ./scripts/rebuild-sequential.sh --wipe-volumes   # удалит БД Postgres
#   ./scripts/rebuild-sequential.sh --no-cache       # без кэша Docker-слоёв

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$DEPLOY_DIR"

ENV_FILE="${ENV_FILE:-.env}"
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Нет файла $ENV_FILE — создайте из env.production.example"
  exit 1
fi

USE_PROD=true
COMPOSE_ARGS=(-f docker-compose.yml)
if [[ -f docker-compose.prod.yml ]]; then
  COMPOSE_ARGS+=(-f docker-compose.prod.yml)
else
  USE_PROD=false
fi

compose() {
  docker compose "${COMPOSE_ARGS[@]}" --env-file "$ENV_FILE" "$@"
}

wipe_volumes=false
no_cache=false
for arg in "$@"; do
  case "$arg" in
    --wipe-volumes) wipe_volumes=true ;;
    --no-cache) no_cache=true ;;
    -h|--help)
      echo "Usage: $0 [--wipe-volumes] [--no-cache]"
      exit 0
      ;;
    *)
      echo "Неизвестный аргумент: $arg"
      exit 1
      ;;
  esac
done

echo "=== 1/3 Остановка контейнеров ==="
if $wipe_volumes; then
  echo "Внимание: удаляются volumes (данные Postgres)!"
  compose down -v --remove-orphans
else
  compose down --remove-orphans
  echo "Volumes сохранены (postgres_data и т.д.)"
fi

echo ""
echo "=== 2/3 Очистка кэша сборки и неиспользуемых образов ==="
docker builder prune -af
docker image prune -af

SERVICES=(
  postgres
  rabbitmq
  identity
  groups
  tasks
  gamification
  bff
  bot
)
if $USE_PROD; then
  SERVICES+=(caddy)
fi

echo ""
echo "=== 3/3 Сборка и запуск по одному сервису ==="
for svc in "${SERVICES[@]}"; do
  echo ""
  echo "------------------------------------------"
  echo "  → $svc"
  echo "------------------------------------------"
  if $no_cache; then
    compose build --no-cache "$svc" 2>/dev/null || true
  fi
  compose up -d --build "$svc"
done

echo ""
echo "=== Готово ==="
compose ps
