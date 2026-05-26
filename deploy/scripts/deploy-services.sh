#!/usr/bin/env bash
# Деплой только указанных сервисов, строго по одному (без параллельной сборки).
# Usage: ./scripts/deploy-services.sh "bot bff"
set -euo pipefail

SERVICES="${1:-}"
if [[ -z "$SERVICES" ]]; then
  echo "Нет сервисов для деплоя — выход."
  exit 0
fi

APP_DIR="${APP_DIR:-$HOME/TelegramTodoApp}"
COMPOSE=(docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env)

# Порядок важен: API → bff → bot → caddy
DEPLOY_ORDER=(identity groups tasks gamification bff bot caddy)

cd "$APP_DIR"
git fetch origin main
git reset --hard origin/main
cd deploy

read -ra REQUESTED <<< "$SERVICES"

is_requested() {
  local svc="$1"
  for r in "${REQUESTED[@]}"; do
    [[ "$r" == "$svc" ]] && return 0
  done
  return 1
}

echo "=== Запрошено: $SERVICES ==="
echo "=== Поднять postgres + rabbitmq (без сборки) ==="
"${COMPOSE[@]}" up -d postgres rabbitmq

COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-deploy}"
NETWORK="${COMPOSE_PROJECT_NAME}_default"

wait_http() {
  local url="$1"
  local name="$2"
  echo "Ожидание $name..."
  for _ in $(seq 1 30); do
    if docker run --rm --network "$NETWORK" curlimages/curl:latest -sf "$url" >/dev/null 2>&1; then
      echo "$name OK"
      return 0
    fi
    sleep 2
  done
  echo "WARN: $name не ответил на $url, продолжаем"
  return 0
}

for svc in "${DEPLOY_ORDER[@]}"; do
  is_requested "$svc" || continue

  echo ""
  echo "=========================================="
  echo "  → $svc (build + up)"
  echo "=========================================="

  if [[ "$svc" == "caddy" ]]; then
    "${COMPOSE[@]}" up -d "$svc"
  else
    "${COMPOSE[@]}" build "$svc"
    "${COMPOSE[@]}" up -d --no-build "$svc"
  fi

  case "$svc" in
    identity) wait_http "http://identity:8080/health" "identity" ;;
    groups)   wait_http "http://groups:8080/health" "groups" ;;
    tasks)    wait_http "http://tasks:8080/health" "tasks" ;;
    gamification) wait_http "http://gamification:8080/health" "gamification" ;;
    bff)      wait_http "http://bff:8080/health" "bff" ;;
  esac
done

echo ""
echo "=== Готово ==="
"${COMPOSE[@]}" ps "${REQUESTED[@]}"
