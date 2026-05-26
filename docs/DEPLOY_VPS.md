# Деплой на VPS (DuckDNS + GitHub Actions)

## 1. На сервере (один раз)

```bash
ssh deploy@109.120.140.25   # или: ssh -i deploy_key deploy@IP

# Docker (если ещё нет)
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# перелогиниться

git clone https://github.com/FelhageDike/TelegramTodoApp.git ~/TelegramTodoApp
cd ~/TelegramTodoApp/deploy
cp env.production.example .env
nano .env
```

Заполните `.env`:

| Переменная | Пример |
|------------|--------|
| `DOMAIN` | `tgtodo.ru` |
| `MINI_APP_URL` | `https://tgtodo.ru/` |
| `BOT_TOKEN` | от BotFather |
| `BOT_INTERNAL_KEY` | как в GitHub Secret |
| `POSTGRES_PASSWORD` | сильный пароль |
| `RABBITMQ_PASSWORD` | сильный пароль |

Первый запуск:

```bash
cd ~/TelegramTodoApp/deploy
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env up -d --build
```

По одному сервису (очистка + сборка): `./scripts/rebuild-sequential.sh` — см. комментарии в файле.

Проверка (через 2–5 мин):

```bash
curl -s https://tgtodo.ru/health
```

## 2. Фаервол

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

## 3. BotFather

- Mini App URL: `https://tgtodo.ru/`
- Menu Button → тот же URL

## 4. GitHub Actions (push → деплой только изменённых сервисов)

После `git push` в `main`:

| Изменили | Пересоберётся на VPS |
|----------|----------------------|
| `src/Bot/**` | только `bot` |
| `src/Gateway/**`, `src/Web/**` | только `bff` |
| `src/Services/Identity/**` | только `identity` |
| … Groups / Tasks / Gamification | соответствующий сервис |
| `src/Shared/**` или `docker/Dockerfile.api` | все API + `bff` + `bot` **по очереди** |
| `deploy/Caddyfile` | только `caddy` |

Сборка **строго по одному сервису**: identity → groups → tasks → gamification → bff → bot → caddy. Следующий стартует после health предыдущего API.

**Не трогает:** `postgres`, `rabbitmq`, Portainer, volumes.

Смотреть прогресс: **GitHub → Actions → Deploy to VPS**.

Ручной деплой всего: **Actions → Run workflow** → галочка **deploy_all**.

Секреты: `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY`. На сервере должен быть `deploy/.env`.

## 5. Обновить MINI_APP_URL в GitHub

**Settings → Secrets → MINI_APP_URL** = `https://tgtodo.ru/`

## 6. Docker в браузере (Portainer)

См. [PORTAINER.md](./PORTAINER.md) — веб-UI через SSH-туннель, без открытия порта в интернет.
