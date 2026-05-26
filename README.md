# TgTodo

Telegram Mini App для задач с геймификацией: личные и групповые (семейные) задачи, баллы, повторения.

## Архитектура

- **Identity** (5001) — пользователи Telegram
- **Groups** (5002) — семьи/группы, invite-коды
- **Tasks** (5003) — задачи, категории, complete + Outbox → RabbitMQ
- **Gamification** (5004) — личный и групповой баланс, ledger
- **BFF** (5000) — auth `initData`, агрегация API, хост Blazor WASM

## Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Быстрый старт

### 1. Инфраструктура (Docker — всё в одном)

**Требования:** Docker Desktop + WSL2 (виртуализация включена в BIOS).

```powershell
cd deploy
.\run-docker.ps1
# или: docker compose up -d --build
```

Поднимаются: PostgreSQL, RabbitMQ, Identity, Groups, Tasks, Gamification, BFF + Mini App на **http://localhost:5000**.

Если `docker ps` выдаёт ошибку 500 — включите «Платформу виртуальной машины» и WSL в Windows, перезагрузите ПК, откройте Docker Desktop.

### 1b. Только БД (без микросервисов в Docker)

```powershell
cd deploy
docker compose up -d postgres rabbitmq
```

Создаются БД: `identity_db`, `groups_db`, `tasks_db`, `gamification_db` и RabbitMQ.

### 2. Переменные окружения

Скопируйте `.env.example` в `.env` в корне (опционально) или задайте переменные:

```powershell
$env:BOT_TOKEN = "your_bot_token"
```

Для локальной разработки **без Telegram** Mini App отправляет заголовок `X-Dev-Telegram-Id: 100000001` (включено в Development на BFF).

### 3. Запуск сервисов

В отдельных терминалах:

```powershell
dotnet run --project src/Services/Identity/TgTodo.Identity.Api
dotnet run --project src/Services/Groups/TgTodo.Groups.Api
dotnet run --project src/Services/Tasks/TgTodo.Tasks.Api
dotnet run --project src/Services/Gamification/TgTodo.Gamification.Api
dotnet run --project src/Gateway/TgTodo.Bff
```

Или скрипт (Windows):

```powershell
.\scripts\start-services.ps1
```

### 4. Открыть приложение

Браузер: [http://localhost:5000](http://localhost:5000)

Для Telegram: в [@BotFather](https://t.me/BotFather) создайте бота, укажите Mini App URL (нужен HTTPS, например ngrok → `https://xxx.ngrok.io`).

## Smoke-test (MVP)

1. Откройте http://localhost:5000
2. **Группы** → создайте «Семья», скопируйте invite-код
3. В другом браузере / инкогнито (или с `X-Dev-Telegram-Id: 100000002` через API) вступите по коду
4. **Добавить** → личная задача (+10 баллов)
5. **Добавить** → общая задача (выберите группу на главной)
6. На главной отметьте задачу ✅ → баланс растёт
7. Повторное ✅ в тот же день не даёт баллы повторно

## API (BFF)

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/bff/home?groupId=` | Задачи + баланс + группы |
| POST | `/bff/tasks` | Создать задачу |
| POST | `/bff/tasks/{id}/complete` | Выполнить |
| GET/POST | `/bff/groups` | Список / создать |
| POST | `/bff/groups/join` | Вступить |
| GET | `/bff/balance` | Баланс |
| GET | `/bff/ledger` | История баллов |

Авторизация: `Authorization: tma {initData}` или в dev `X-Dev-Telegram-Id`.

## Telegram Bot

Команды MVP + inline (`@бот купить молоко +10` → карточка в чате): **[docs/BOT.md](docs/BOT.md)**.

```powershell
# В deploy/.env задайте BOT_TOKEN и BOT_INTERNAL_KEY (тот же ключ, что Bot__InternalKey у BFF)
docker compose -f deploy/docker-compose.yml up -d --build bot
```

Локально: `dotnet run --project src/Bot/TgTodo.Bot` (нужны BFF на :5000 и `BOT_TOKEN`).

## Roadmap (не в MVP)

- Магазин наград (Shop service)
- Штрафы за просрочку
- Telegram Bot уведомления (напоминания)
- Jobs для daily/weekly reset

## Структура репозитория

```
src/
  Shared/           Contracts, BuildingBlocks
  Services/         Identity, Groups, Tasks, Gamification
  Gateway/          BFF
  Web/              Blazor MiniApp
deploy/             docker-compose
```
