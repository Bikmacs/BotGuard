-- mixed_attack.lua
local counter = 0

-- Список User-Agent для имитации
local user_agents = {
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", -- Нормальный
    "python-requests/2.31", -- Бот (попадёт под UA-фильтр)
    "curl/7.68.0",          -- Бот (попадёт под UA-фильтр)
    "Googlebot/2.1 (+http://www.google.com/bot.html)" -- Поисковый бот (после фикса не должен баниться)
}

-- Генератор случайного IP для обхода блокировки по IP
local function random_ip()
    math.randomseed(os.time() + math.random()) -- Немного хаоса
    return string.format("%d.%d.%d.%d", math.random(1, 223), math.random(0, 255), math.random(0, 255), math.random(0, 255))
end

request = function()
    counter = counter + 1
    local ua = user_agents[math.random(#user_agents)]
    local path = "/"
    -- Каждый 10-й запрос шлем на несуществующий путь, чтобы проверить 404
    if counter % 10 == 0 then path = "/nonexistent" end

    -- Используем X-Forwarded-For для имитации разных IP
    return wrk.format("GET", path, {
        ["User-Agent"] = ua,
        ["X-Forwarded-For"] = random_ip()
    })
end