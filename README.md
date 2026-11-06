<div align="center">

# BotGuard

**High-Performance Anti-Abuse API Gateway**

[![.NET CI](https://github.com/Bikmacs/BotGuard/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Bikmacs/BotGuard/actions/workflows/dotnet.yml)

Защита веб-приложений от ботов, парсеров и L7-спама на уровне HTTP-трафика с разделением на синхронный Hot Path и асинхронный Cold Path.

</div>

> [!IMPORTANT]
> ### АРХИТЕКТУРА HOT / COLD PATH И ПРИНЦИП FAIL-OPEN
> Проект спроектирован по принципу **Event-Driven Architecture**. Критический путь проксирования трафика (**Hot Path**) изолирован от тяжелых дисковых операций (**Cold Path**) через брокер сообщений RabbitMQ. Все обращения к внешним хранилищам реализованы по принципу **Fail-Open**: если Redis или RabbitMQ недоступны, шлюз фиксирует сбой в журнале, но продолжает пропускать легитимный трафик на целевой сервер без возврата ошибки `500 Internal Server Error`.

> [!NOTE]
> ### ИНЖЕНЕРНЫЙ КЕЙС: ЛИКВИДАЦИЯ ШТОРМА СОБЫТИЙ (529 RPS → 20 000+ RPS)
> В первой ревизии шлюз публиковал событие `AbuseDetectedEvent` в RabbitMQ на **каждый** заблокированный запрос. При стресс-тесте (500 соединений) это вызвало шторм событий: пропускная способность упала до **529 RPS** (latency 912 ms, 433 таймаута), а БД получила **32 308 дублирующихся записей**. После внедрения атомарной дедупликации `TryMarkReported` (`SET key value EX 60 NX` в Redis) и отключения публикаций для уже забаненных IP пропускная способность выросла до **20 000–39 620 RPS**, средняя задержка снизилась до **3.43 ms** (таймаутов 0), а 927 000+ запросов от нарушителя создают ровно **1 целевой инцидент** за окно.

## Архитектура

Система разделена на два независимых контура и разворачивается в изолированной сети из 6 Docker-контейнеров:

```mermaid
flowchart LR
    Client([HTTP Client / Bot]) --> Gateway

    subgraph HotPath [Hot Path — BotGuard.Gateway]
        Gateway[YARP Reverse Proxy<br/>+ BotGuardMiddleware]
    end

    Gateway -->|1. Blacklist & Rate Limit<br/>Lua: INCR + PEXPIRE| Redis[(Redis 7<br/>In-Memory Cache)]
    Gateway -->|2. Чистый трафик| Target[Target Site<br/>Nginx]
    Gateway -.->|3. Первое нарушение за окно<br/>SafePublishAsync| Rabbit[(RabbitMQ 3<br/>ban-queue)]

    subgraph ColdPath [Cold Path — BotGuard.BanConsumer]
        Worker[AbuseDetectedConsumer<br/>MassTransit Worker]
    end

    Rabbit ==>|Consume Event| Worker
    Worker -->|Бан IP на 10 мин<br/>SET EX| Redis
    Worker -->|История инцидентов<br/>EF Core| Postgres[(PostgreSQL 17<br/>Incidents DB)]
```

1. **Горячий путь (Hot Path — `BotGuard.Gateway`):** Обратный прокси на базе YARP. Извлекает реальный IP клиента (`UseForwardedHeaders`), проверяет заголовок `User-Agent` (с учетом белого списка поисковых роботов `AllowedBots`), проверяет наличие IP в черном списке Redis и контролирует лимит запросов (Fixed Window: 5 запросов за 10 секунд на Lua-скрипте). Чистый запрос проксируется на целевой сервер.
2. **Холодный путь (Cold Path — `BotGuard.BanConsumer`):** Фоновый воркер на базе MassTransit. Слушает очередь `ban-queue` в RabbitMQ, записывает IP-адрес нарушителя в черный список Redis с TTL 10 минут и сохраняет карточку инцидента в PostgreSQL.

## Принятые инженерные решения

- **Атомарный Rate Limiting на Lua с самоисцелением TTL:** Выполнение `INCR` и `PEXPIRE` двумя отдельными вызовами из C# создает риск гонки: если процесс шлюза перезапустится между командами, ключ в Redis останется без таймера жизни (`PTTL = -1`), и IP получит вечный бан. Lua-скрипт выполняется в Redis атомарно и проверяет условие `PTTL < 0`, принудительно восстанавливая TTL.
- **Дедупликация событий (`TryMarkReported`):** Пока сообщение о нарушении летит через RabbitMQ в воркер, атакующий бот успевает отправить сотни новых запросов. Маркер `TryMarkReported` (`SET NX EX`) блокирует повторную отправку жалоб на один и тот же IP и тип нарушения в течение 1 минуты, защищая RabbitMQ и PostgreSQL от перегрузки.
- **Белый список поисковых систем (`AllowedBots`):** Перед проверкой черного списка сигнатур (`curl`, `python-requests`, `headlesschrome`, `bot`) инспектор сверяет заголовок с белым списком (`Googlebot`, `YandexBot`, `Bingbot`), чтобы не заблокировать индексацию сайта поисковиками.
- **Защита схемы БД и индексация (EF Core):** Поля `IpAddress` и `UserAgent` ограничены на уровне БД (`HasMaxLength(64)` и `HasMaxLength(512)`), что предотвращает атаки на переполнение диска гигантскими заголовками. Для быстрой выборки истории создан составной B-Tree индекс `(IpAddress, DetectedAtUtc)`.
- **Кооперативная отмена (`RequestAborted`):** Токен отмены HTTP-запроса проброшен во все асинхронные операции чтения кэша, публикации событий и записи ответа, а фильтры исключений игнорируют штатный разрыв соединения со стороны клиента (`when (ex is not OperationCanceledException)`).

## Технологический стек

- **Платформа:** .NET 10 (C# 14), ASP.NET Core
- **Reverse Proxy:** YARP (Microsoft)
- **Брокер сообщений:** RabbitMQ 3 + MassTransit 8
- **Кэш / Rate Limiter / Blacklist:** Redis 7 (`StackExchange.Redis`, Lua)
- **База данных:** PostgreSQL 17 + Entity Framework Core 10
- **Инфраструктура и CI:** Docker Compose, GitHub Actions, `wrk` (Lua scripts)

## Использование (Локальный запуск)

### Требования

- Docker Desktop (или Docker Engine + Compose v2)
- `curl` для базовой проверки

### Запуск стенда

```bash
git clone [https://github.com/Bikmacs/BotGuard.git](https://github.com/Bikmacs/BotGuard.git)
cd BotGuard
docker compose up -d --build
```

Контейнеры баз данных и брокера проверяются через `healthcheck`, миграции PostgreSQL применяются автоматически при старте `banconsumer` (через `MigrateAsync` с циклом retry). Шлюз принимает запросы на `http://localhost:5233`.

### Проверка работы

```bash
# 1. Легитимный запрос — проксируется на целевой контейнер Nginx
curl -i -A "Mozilla/5.0 (Windows NT 10.0)" http://localhost:5233/
# → HTTP/1.1 200 OK

# 2. Поисковый бот из белого списка — проходит без бана
curl -i -A "Mozilla/5.0 (compatible; Googlebot/2.1; +[http://www.google.com/bot.html](http://www.google.com/bot.html))" http://localhost:5233/
# → HTTP/1.1 200 OK

# 3. Подозрительный User-Agent — блокируется, событие уходит в RabbitMQ
curl -i -A "python-requests/2.31" http://localhost:5233/
# → HTTP/1.1 403 Forbidden

# 4. Rate limit: 10 запросов подряд — 5 пройдут (200), 6-й → 429, далее бан → 403
for i in {1..10}; do
  curl -s -o /dev/null -w "%{http_code} " \
    -A "Mozilla/5.0 (Windows NT 10.0)" http://localhost:5233/
done; echo
# → 200 200 200 200 200 429 403 403 403 403
```

## Результаты нагрузочного тестирования

Тестирование проводилось утилитой `wrk` (Docker Desktop for Mac, Apple Silicon) по трем сценариям (`attack.lua`, `mixed_attack.lua`, `distributed_attack.lua`).

| Сценарий | RPS | p99 Latency | Δ Incidents | ban-queue |
| :--- | :--- | :--- | :--- | :--- |
| **1. До фикса Event Storm** (500 conn) | 529 | 912 ms | +32 308 | забита |
| **2. Single IP Flood** (`attack.lua`, 927K reqs) | 30 900 | 180 ms | **+1** | **0** |
| **3. Mixed Attack** (`mixed_attack.lua`, 100 IPs, 2.38M reqs) | 39 620 | 586 ms | **+100** | **0** |
| **4. Distributed Attack** (`distributed_attack.lua`, Random IPs) | 15 835 | 337 ms | 0 | 0 |

**Что доказывают замеры:**
- **Подавление шторма (Сценарии 2 и 3):** 100 атакующих IP-адресов за 2.38 млн запросов сгенерировали ровно 100 инцидентов в БД. Механизм дедупликации `TryMarkReported` (`SET NX EX`) отфильтровал 99.995% дублирующихся событий, сохранив нулевую очередь в RabbitMQ.
- **Границы применимости (Сценарий 4):** Атака с миллиона случайных IP-адресов (имитация крупного ботнета через `X-Forwarded-For`) проходит сквозь per-IP лимитер, так как каждый новый адрес делает меньше 5 запросов. Это честная граница применимости прикладного L7-фильтра.

## Распространенные вопросы и диагностика

### Ошибка StackExchange.Redis.RedisConnectionException в логах
> **Решение:** Проверьте статус контейнера командой `docker compose ps`. Благодаря паттерну **Fail-Open** и настройке `AbortOnConnectFail = false` шлюз продолжит проксировать трафик и автоматически восстановит связь после запуска Redis.

### Воркер не читает сообщения из RabbitMQ
> **Решение:** Откройте панель RabbitMQ Management (`http://localhost:15672`, логин/пароль из `compose.yaml`). Если очередь `ban-queue` растет при `Consumers = 0`, перезапустите воркер командой `docker compose restart banconsumer`.

### Как разбанить IP вручную?
> **Решение:** Бан истекает автоматически через 10 минут (TTL). Для досрочного удаления ключа выполните:
> ```bash
> # 1. Посмотреть все активные баны
> docker compose exec redis redis-cli --scan --pattern '*list:*'
>
> # 2. Удалить конкретный ключ из вывода выше
> docker compose exec redis redis-cli DEL "blacklist:127.0.0.1"
> ```

## Ограничения

`BotGuard` — прикладной L7-фильтр для защиты от скриптов, парсеров и одиночных флуд-атак, **не замена** провайдерским сетям уровня Cloudflare или Qrator. Per-IP лимиты не останавливают распределенные ботнеты, где каждый узел делает по 1 запросу с чистого IP. Для такой защиты требуются фильтрация по ASN/GeoIP, JS Proof-of-Work челленджи и очистка трафика на уровне сетевого оборудования (L3/L4 Anycast).

## Roadmap

- [x] Проксирование трафика через YARP Reverse Proxy и поддержка `X-Forwarded-For`
- [x] Rate Limiting и Blacklist в Redis на атомарном Lua-скрипте
- [x] Асинхронная шина RabbitMQ + MassTransit и дедупликация Event Storm
- [x] Воркер `BanConsumer` с записью истории в PostgreSQL и авто-миграциями
- [x] Отказоустойчивость Fail-Open и белый список поисковых роботов (`AllowedBots`)
- [x] Оркестрация в Docker Compose и CI-пайплайн в GitHub Actions
- [x] Нагрузочное тестирование (`wrk`, 3 Lua-сценария)
- [ ] Покрытие логики модульными тестами (`xUnit`)
- [ ] Структурированное логирование (`Serilog` + `Seq`)
- [ ] Стриминг телеметрии трафика в Apache Kafka через `System.Threading.Channels`
- [ ] Отправка алертов об инцидентах в Telegram API

## Лицензирование

Проект распространяется на условиях лицензии **MIT**.
