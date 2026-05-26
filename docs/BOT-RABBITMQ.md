# Очередь Telegram-апдейтов в RabbitMQ

Бот может принимать апдейты (webhook или long polling) и складывать их в **durable-очередь RabbitMQ**. Обработка идёт отдельными consumer-каналами с `prefetch=1` — апдейты переживают рестарт процесса бота (пока сообщения не подтверждены `ack`).

Если `Bot:RabbitMq:HostName` **пустой**, используется прежняя **in-memory** очередь (`Channel`) — удобно для локальной отладки без RabbitMQ.

---

## Схема

```text
Telegram ──► webhook / polling ──► TgTodo.Bot (publish)
                                        │
                                        ▼
                              RabbitMQ queue
                         (tgbot.telegram.updates)
                                        │
                                        ▼
                    N consumer channels (prefetch 1)
                                        │
                                        ▼
                              BotUpdateHandler → BFF
```

---

## 1. RabbitMQ в Docker (уже в `deploy/docker-compose.yml`)

Сервис `rabbitmq` (образ `rabbitmq:3-management-alpine`):

| Параметр | Значение по умолчанию |
|----------|------------------------|
| AMQP | `5672` |
| Management UI | `http://localhost:15672` |
| Логин | `tgtodo` |
| Пароль | `tgtodo` |

Очередь **`tgbot.telegram.updates`** создаётся ботом при старте (`durable`, без auto-delete).

---

## 2. Переменные для бота (`deploy/.env`)

Минимум для Docker (в compose уже подставлены дефолты):

```env
BOT_TOKEN=...
BOT_INTERNAL_KEY=...

# Включить RabbitMQ (в compose по умолчанию host = rabbitmq)
BOT_RABBITMQ_HOST=rabbitmq
BOT_RABBITMQ_USER=tgtodo
BOT_RABBITMQ_PASSWORD=tgtodo

# Опционально
BOT_RABBITMQ_QUEUE=tgbot.telegram.updates
BOT_RABBITMQ_CONSUMERS=16
```

Чтобы **отключить** RabbitMQ и вернуть in-memory очередь:

```env
BOT_RABBITMQ_HOST=
```

---

## 3. Конфигурация в appsettings / переменные окружения

Секция `Bot:RabbitMq`:

| Ключ | Env (Docker) | Описание |
|------|----------------|----------|
| `HostName` | `Bot__RabbitMq__HostName` / `BOT_RABBITMQ_HOST` | Пусто → in-memory. `rabbitmq` в compose. |
| `Port` | `Bot__RabbitMq__Port` | 5672 |
| `UserName` | `Bot__RabbitMq__UserName` | |
| `Password` | `Bot__RabbitMq__Password` | |
| `VirtualHost` | `Bot__RabbitMq__VirtualHost` | `/` |
| `QueueName` | `Bot__RabbitMq__QueueName` | `tgbot.telegram.updates` |
| `MaxConsumerChannels` | `Bot__RabbitMq__MaxConsumerChannels` | Параллельных consumer (≈ `Bot__MaxParallelUpdateHandlers`) |

Параллелизм обработки в RabbitMQ задаётся **`MaxConsumerChannels`** (отдельный канал на consumer, у каждого `BasicQos prefetch=1`).

---

## 4. Webhook + RabbitMQ (прод)

1. Поднять стек: `docker compose up -d` (postgres, rabbitmq, сервисы, bff, **bot**).
2. В `.env`:
   - `BOT_DELIVERY_MODE=Webhook`
   - `BOT_WEBHOOK_PUBLIC_BASE_URL=https://ваш-домен` (HTTPS, без пути)
   - `BOT_WEBHOOK_SECRET_TOKEN=` длинная случайная строка
   - `BOT_RABBITMQ_HOST=rabbitmq`
3. Прокси (Caddy/Nginx): HTTPS → контейнер `bot:8080`, путь `BOT_WEBHOOK_PATH` (по умолчанию `/telegram/webhook`).
4. При старте бот вызывает `setWebhook` на публичный URL.

Поведение webhook:

- Успешная публикация в RabbitMQ → **200 OK** (Telegram доволен).
- Ошибка publish / RabbitMQ недоступен → **503** (Telegram повторит доставку).

---

## 5. Локальная разработка

**С RabbitMQ (как в проде):**

```powershell
cd deploy
docker compose up -d rabbitmq bff bot
# BOT_RABBITMQ_HOST=rabbitmq уже в compose
```

**Без RabbitMQ:**

```powershell
$env:Bot__RabbitMq__HostName = ""
$env:BOT_TOKEN = "..."
dotnet run --project src/Bot/TgTodo.Bot
```

---

## 6. Мониторинг

- UI: http://localhost:15672 → Queues → `tgbot.telegram.updates` (ready / unacked).
- Логи бота: `RabbitMQ connection open`, `RabbitMQ dispatch: N consumer channel(s)`.

При падении обработчика сообщение **nack + requeue** и будет обработано снова.

---

## 7. Продакшен: рекомендации

| Тема | Рекомендация |
|------|----------------|
| Пароль | Сменить `RABBITMQ_DEFAULT_PASS` и `BOT_RABBITMQ_PASSWORD`, не светить в git |
| Сеть | Порт `5672` наружу не публиковать; только внутри Docker/VPC |
| Один бот на токен | Не запускать два процесса с одним `BOT_TOKEN` и одним webhook |
| Несколько реплик бота | Возможно, если все consumer читают **одну** очередь; webhook — один URL (балансировщик → один приёмник или один инстанс на publish) |
| Ресурсы | Увеличить `BOT_RABBITMQ_CONSUMERS` при медленном BFF; следить за unacked в UI |

---

## 8. Связанные файлы

| Файл | Назначение |
|------|------------|
| `Services/RabbitMqTelegramUpdateIngress.cs` | Publish + consumers |
| `Services/ChannelTelegramUpdateIngress.cs` | Fallback in-memory |
| `Services/ITelegramUpdateIngress.cs` | Абстракция |
| `BotRabbitMqOptions.cs` | Модель настроек |
| `docs/BOT.md` | Команды бота, webhook, inline |
