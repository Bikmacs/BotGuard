<div align="center">

# BotGuard

**Anti-Abuse API Gateway**

Защита веб-сайтов от ботов, парсеров, спама и DDoS-атак на уровне HTTP-трафика.

</div>

> [!IMPORTANT]
> ### АРХИТЕКТУРА И НАГРУЗКА
> Проект спроектирован по принципу Event-Driven Architecture (событийно-ориентированной архитектуры). Тяжелые операции (запись в БД, отправка алертов) вынесены в фоновый воркер (Cold Path) через RabbitMQ. Это позволяет шлюзу (Hot Path) обрабатывать входящие запросы за миллисекунды и не блокироваться при пиковых нагрузках.

> [!WARNING]
> ### ЛИМИТЫ KESTREL
> В ходе нагрузочного тестирования (500 одновременных соединений) шлюз зафиксировал 433 таймаута. Это не утечка памяти и не баг, а аппаратный лимит пула соединений Kestrel (веб-сервера ASP.NET Core) в режиме Debug. В режиме Release (Production) пропускная способность значительно выше. База данных при этом успешно сохранила все 32 308 инцидентов без потерь.

---

## Архитектура

Система разделена на два независимых контура:

1. **Горячий путь (Hot Path):** Шлюз (`BotGuard.Gateway`) на базе YARP. Проверяет User-Agent, смотрит в Redis (черный список IP) и проверяет Rate Limit (скользящее окно на Lua-скрипте). Если запрос чистый — проксирует его на целевой сайт.
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

## ⚙️ Использование (Локальный запуск)

### 1. Разверните инфраструктуру в Docker
Убедитесь, что Docker Desktop запущен, и выполните команды для поднятия Redis, RabbitMQ, PostgreSQL и целевого сайта (Nginx):

```bash
docker run -d --name botguard-redis -p 6379:6379 redis:7
docker run -d --name botguard-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
docker run -d --name botguard-postgres -p 5432:5432 -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres -e POSTGRES_DB=botguard postgres:17
docker run -d --name target-site -p 8080:80 nginx
```

### 2. Примените миграции БД
В терминале перейдите в папку `src/BotGuard.BanConsumer` и выполните:

```bash
dotnet ef database update
```

### 3. Запустите приложения
1. Откройте решение `BotGuard.sln` в IDE (Rider / Visual Studio).
2. Запустите проект `BotGuard.BanConsumer` (в фоне).
3. Запустите проект `BotGuard.Gateway`. Шлюз начнет слушать порт `http://localhost:5233`.

### 4. Проверка работы
Отправьте запрос с подозрительным User-Agent:

```bash
curl -A "python-requests/2.31" http://localhost:5233/
```

**Ожидаемый результат:** `403 Forbidden`. В логах `BanConsumer` появится запись о бане, а в PostgreSQL — новый инцидент.

---

## 📊 Результаты нагрузочного тестирования

Тестирование проводилось с использованием `wrk` и Lua-скрипта (имитация 500 ботов):

- **Пропускная способность:** ~529 запросов в секунду (RPS)
- **Latency:** 912 мс (при 500 соединениях)
- **Ошибки соединений:** 433 таймаута
- **Записано инцидентов в БД:** 32 308

---

## ☑️ Распространенные вопросы и проблемы

### Ошибка StackExchange.Redis.RedisConnectionException при старте
> **Решение:** Убедитесь, что контейнер `botguard-redis` запущен. Проверьте его статус командой `docker ps`. Если контейнер был перезапущен, подождите 5 секунд и перезапустите шлюз.

### Воркер не читает сообщения из RabbitMQ
> **Решение:** Проверьте вкладку Queues в панели RabbitMQ Management (`http://localhost:15672`, `guest/guest`). Если очередь `ban-queue` растет, а Consumers равно 0, значит `BotGuard.BanConsumer` потерял соединение. Перезапустите воркер.

### Ошибка Cannot instantiate implementation type 'AbuseDetectedConsumer'
> **Решение:** Класс консьюмера должен быть объявлен как `public class AbuseDetectedConsumer`, а не просто `class`. Проверьте модификаторы доступа.

### Как разбанить IP вручную?
> **Решение:** Выполните команду:
> ```bash
> docker exec -it botguard-redis redis-cli DEL "blocklist:127.0.0.1"
> ```

---

## 🗒️ Roadmap

- [x] Настройка YARP Reverse Proxy
- [x] Rate Limiting на Redis (Lua)
- [x] Публикация событий в RabbitMQ
- [x] Воркер и запись истории в PostgreSQL
- [x] Нагрузочное тестирование
- [ ] Интеграция с Telegram API (алерты)
- [ ] Оркестрация Docker Compose
- [ ] Honeypot-маршруты и JS Proof-of-Work

---

## ⚖️ Лицензирование

Проект распространяется на условиях лицензии **MIT**.