-- attack.lua
local user_agents = {
  "python-requests/2.31",            -- Тупой бот
  "curl/7.68.0",                     -- Тупой бот
  "Mozilla/5.0 (Windows NT 10.0)",   -- Притворяется браузером
  "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)",
  "Googlebot/2.1 (+http://www.google.com/bot.html)" -- Поисковый бот
}

local paths = {
  "/",              -- Главная
  "/admin",         -- Сканер уязвимостей
  "/wp-login.php",  -- Сканер WordPress
  "/api/v1/users",  -- API
  "/.env"           -- Украсть секреты
}

function request()
  local ua = user_agents[math.random(#user_agents)]
  local path = paths[math.random(#paths)]
  
  -- Каждый запрос: случайный путь + случайный User-Agent
  return wrk.format("GET", path, {
    ["User-Agent"] = ua,
    ["Accept-Language"] = "en-US,en;q=0.9" -- Иногда добавляем заголовок, чтобы обмануть скоринг
  })
end
