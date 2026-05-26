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
| `DOMAIN` | `tgtodo.duckdns.org` (ваш DuckDNS) |
| `MINI_APP_URL` | `https://tgtodo.duckdns.org/` |
| `BOT_TOKEN` | от BotFather |
| `BOT_INTERNAL_KEY` | как в GitHub Secret |
| `POSTGRES_PASSWORD` | сильный пароль |
| `RABBITMQ_PASSWORD` | сильный пароль |

Первый запуск:

```bash
cd ~/TelegramTodoApp/deploy
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env up -d --build
```

Проверка (через 2–5 мин):

```bash
curl -s https://tgtodo.duckdns.org/health
```

## 2. Фаервол

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

## 3. BotFather

- Mini App URL: `https://ВАШ.duckdns.org/`
- Menu Button → тот же URL

## 4. GitHub

Секреты уже добавлены → каждый `git push` в `main` деплоит автоматически.

Ручной деплой: **Actions → Deploy to VPS → Run workflow**.

## 5. Обновить MINI_APP_URL в GitHub

**Settings → Secrets → MINI_APP_URL** = `https://ваш.duckdns.org/`

## 6. Docker в браузере (Portainer)

См. [PORTAINER.md](./PORTAINER.md) — веб-UI через SSH-туннель, без открытия порта в интернет.
