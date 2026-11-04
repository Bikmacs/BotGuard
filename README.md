<div align="center">

# BotGuard

**Anti-Abuse API Gateway**

Защита веб-сайтов от ботов, парсеров и «шумных» клиентов на уровне HTTP-трафика.

</div>

> [!IMPORTANT]
> ### АРХИТЕКТУРА И НАГРУЗКА
> Проект спроектирован по принципу Event-Driven Architecture (событийно-ориентированной архитектуры). Тяжёлые операции (запись инцидентов в БД, бан IP) вынесены в фоновый воркер через RabbitMQ. Это позволяет шлюзу (Hot Path) обрабатывать входящие запросы за миллисекунды и не блокироваться при пиковых нагрузках.

> [!NOTE]
> ### ДО И ПОСЛЕ: ШТОРМ СОБЫТИЙ
> В первом нагрузочном тесте (500 соединений, 30 секунд) шлюз записал 32 308 инцидентов
> и показал 433 таймаута при latency 912 ms. Причина — `Publish` в middleware
> публиковал событие на **каждый** запрос забаненного клиента. После фикса
> (публикация только на первом нарушении за окно) — **+1 инцидент на 927 018 запросов**,
> latency 3.43 ms, таймаутов 0. Подробности — в разделе Performance.

---

## Архитектура

Система разделена на два независимых контура:

1. **Горячий путь (Hot Path):** Шлюз (`BotGuard.Gateway`) на базе YARP. Проверяет User-Agent, смотрит в Redis (черный список IP) и проверяет Rate Limit (fixed window, 5 запросов / 10 секунд, Lua-скрипт). Если запрос чистый — проксирует его на целевой сайт.
2. **Холодный путь (Cold Path):** Воркер (`BotGuard.BanConsumer`). Слушает RabbitMQ, банит IP в Redis и пишет историю инцидентов в PostgreSQL.

---

## Технологический стек

- **Платформа:** .NET 10 (C#)
- **Reverse Proxy:** YARP (Microsoft)
- **Брокер сообщений:** RabbitMQ + MassTransit
- **Кэш / Blacklist:** Redis (StackExchange.Redis)
- **База данных:** PostgreSQL 17 + Entity Framework Core
- **Инфраструктура:** Docker

---

## Использование (Локальный запуск)

### Требования
- Docker Desktop (или Docker Engine + Compose v2)
- `curl` для проверки

### Запуск

```bash
git clone https://github.com/Bikmacs/BotGuard.git
cd BotGuard
docker compose up -d --build
```

Миграции БД применяются автоматически при старте `banconsumer` (через `MigrateAsync`).
Шлюз слушает `http://localhost:5233`.

### Проверка работы

```bash
# Легитимный запрос — проксируется на nginx
curl -A "Mozilla/5.0 (Windows NT 10.0)" http://localhost:5233/
# → 200 OK, HTML от nginx

# Подозрительный UA — блокируется
curl -A "python-requests/2.31" http://localhost:5233/
# → 403 Forbidden

# Rate limit: 10 запросов подряд — 5 пройдут, 6-й → 429, дальше бан → 403
for i in {1..10}; do
  curl -s -o /dev/null -w "%{http_code} " \
    -A "Mozilla/5.0 (Windows NT 10.0)" http://localhost:5233/
done; echo
# → 200 200 200 200 200 429 403 403 403 403
```

---
## Результаты нагрузочного тестирования

Тестирование проводилось с использованием `wrk` (Docker Desktop for Mac, Apple Silicon).
Абсолютные цифры занижены из-за виртуализации сети Mac, но они доказывают стабильность архитектуры.

| Сценарий | RPS | p99 Latency | Δ Incidents | ban-queue |
| :--- | :--- | :--- | :--- | :--- |
| **1. Before Fix** (10 curl) | - | - | +27 | растёт |
| **2. Mixed Attack** (100 IPs, 2.38M reqs) | 39 620 | 586 ms | **+100** | **0** |
| **3. Distributed Attack** (Random IPs) | 15 835 | 337 ms | 0 | 0 |

**Что это доказывает:**
*   **Защита от шторма (Сценарий 2):** 100 разных IP-адресов сгенерировали ровно 100 инцидентов на 2.38 миллиона запросов. Механизм `TryMarkReported` (SET NX EX) отработал с эффективностью 99.999%, не перегрузив PostgreSQL и RabbitMQ.
*   **Ограничение защиты (Сценарий 3):** Атака с миллиона случайных IP-адресов (имитация ботнета) проходит сквозь шлюз, так как per-IP rate limiter не срабатывает на каждый новый адрес. Это честное архитектурное ограничение L7-фильтра.
---

## Распространенные вопросы и проблемы

### Ошибка StackExchange.Redis.RedisConnectionException при старте
> **Решение:** Убедитесь, что контейнер `botguard-redis` запущен. Проверьте его статус командой `docker ps`. Если контейнер был перезапущен, подождите 5 секунд и перезапустите шлюз.

### Воркер не читает сообщения из RabbitMQ
> **Решение:** Проверьте вкладку Queues в панели RabbitMQ Management (http://localhost:15672, botguard / botguard-pass). Если очередь `ban-queue` растет, а Consumers равно 0, значит `BotGuard.BanConsumer` потерял соединение. Перезапустите воркер.

### Ошибка Cannot instantiate implementation type 'AbuseDetectedConsumer'
> **Решение:** Класс консьюмера должен быть объявлен как `public class AbuseDetectedConsumer`, а не просто `class`. Проверьте модификаторы доступа.

### Как разбанить IP вручную?

> **Решение:**
>
> ```bash
> # 1. Посмотреть все забаненные IP
> docker compose exec redis redis-cli --scan --pattern 'blocklist:*'
> # → "blocklist:::ffff:192.168.65.1"
>
> # 2. Удалить конкретный (подставь ключ из вывода выше)
> docker compose exec redis redis-cli DEL "blocklist:::ffff:192.168.65.1"
>
> # 3. Проверить, что удалён
> docker compose exec redis redis-cli --scan --pattern 'blocklist:*'
> # → пусто
> ```
>
> Бан истекает сам через TTL (10 минут). `DEL` нужен, только чтобы снять досрочно.
---

## Roadmap

- [x] Настройка YARP Reverse Proxy
- [x] Rate Limiting на Redis (Lua)
- [x] Публикация событий в RabbitMQ
- [x] Воркер и запись истории в PostgreSQL
- [x] Нагрузочное тестирование
- [ ] Интеграция с Telegram API (алерты)
- [x] Оркестрация Docker Compose
- [ ] Honeypot-маршруты и JS Proof-of-Work


---
## Ограничения

BotGuard — L7-фильтр для одиночных ботов и «шумных» клиентов, **не замена**
Cloudflare/Akamai. Per-IP rate limit и per-IP blocklist не защищают от
распределённой атаки: ботнет даёт 10 000 IP × 1 запрос, лимит на каждый IP
не срабатывает. Для защиты от L7-DDoS нужны geo-репутация, JS-челлендж,
поведенческий анализ, anycast-scrubbing на уровне провайдера.

---

## ⚖️ Лицензирование

Проект распространяется на условиях лицензии **MIT**.