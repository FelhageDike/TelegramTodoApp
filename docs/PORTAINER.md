# Portainer — Docker в браузере

[Portainer](https://www.portainer.io/) — веб-интерфейс: контейнеры, логи, рестарты, образы, volumes.

Порт **9000** слушает только **localhost на VPS** (не открыт в интернет).

## Установка на сервере

```bash
cd ~/TelegramTodoApp/deploy
docker compose -f docker-compose.tools.yml up -d
```

Первый заход: создайте **admin**-логин и пароль (8+ символов).

В Portainer: **Environments → local** → **Connect** — увидите все контейнеры `deploy-*`.

## Открыть в браузере с Windows

Окно PowerShell **оставьте открытым** (пока туннель активен):

```powershell
ssh -i d:\TgTodo\deploy_key -L 9000:127.0.0.1:9000 deploy@ВАШ_IP_VPS
```

В браузере: **http://localhost:9000**

## Что смотреть в UI

| Раздел | Зачем |
|--------|--------|
| **Containers** | статус, Restart, Stop |
| **Logs** | логи без `docker logs` |
| **Images** | что собрано, удалить старое |
| **Volumes** | данные Postgres |

Стек приложения: контейнеры с префиксом `deploy-` (bff, bot, caddy, postgres…).

## Без sudo

Portainer работает от пользователя в группе `docker`. `sudo` не нужен.

## Не выставляйте 9000 в интернет

Открытый Portainer = полный доступ к серверу. Только SSH-туннель или VPN.
