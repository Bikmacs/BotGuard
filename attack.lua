local thread_counter = 1
function setup(thread)
    thread:set("seed", os.time() + thread_counter * 1000 + math.random(100000))
    thread_counter = thread_counter + 1
end

local methods, uas, paths, accepts, languages, referers
local random_strings, random_jsons, random_cookies, random_paths

function init(args)
    math.randomseed(seed)

    methods = {"GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"}
    
    uas = {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
        "Mozilla/5.0 (X11; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/119.0",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15",
        "Mozilla/5.0 (Linux; Android 13; SM-S908B) AppleWebKit/537.36",
        "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
        "Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)",
        "Mozilla/5.0 (compatible; YandexBot/3.0; +http://yandex.com/bots)",
        "facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)",
        "python-requests/2.31.0", "curl/7.68.0", "Wget/1.21.3", "Go-http-client/1.1",
        "HeadlessChrome/120.0.0.0", "Selenium/4.14.0", "Puppeteer/21.4.1", "Playwright/1.39.0",
        "Scrapy/2.11.0 (+https://scrapy.org)", "sqlmap/1.7.2#stable", "Nikto/2.5.0", 
        "AhrefsBot/7.0", "SemrushBot/7~bl", "DotBot/1.1"
    }

    paths = {
        "/", "/index.html", "/api/v1/users", "/api/v1/auth/login", "/login", "/admin",
        "/images/logo.png", "/css/style.css", "/js/app.js", "/robots.txt",
        "/.env", "/.git/config", "/wp-admin", "/wp-login.php", "/phpmyadmin", 
        "/backup.zip", "/dump.sql", "/config.json", "/phpinfo.php", "/swagger-ui.html",
        "/a7f8g2", "/test", "/api/v3/secret"
    }

    accepts = {"text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8", "application/json, text/plain, */*", "*/*"}
    languages = {"en-US,en;q=0.9", "ru-RU,ru;q=0.9", "zh-CN,zh;q=0.9", "de-DE,de;q=0.9"}
    referers = {"https://google.com/", "https://yandex.ru/", "https://bing.com/", ""}

    -- ПРЕДГЕНЕРАЦИЯ: создаем пулы строк ОДИН РАЗ при старте
    random_strings = {}
    random_cookies = {}
    random_jsons = {}
    random_paths = {}

    for i = 1, 100 do
        local str = ""
        for j = 1, 20 do
            str = str .. string.char(math.random(97, 122))
        end
        random_strings[i] = str
        random_cookies[i] = "session=" .. str .. "; csrftoken=" .. str
        random_jsons[i] = '{"username":"admin","password":"' .. str .. '"}'
        random_paths[i] = "/" .. str
    end
end

local function get_random(arr)
    return arr[math.random(#arr)]
end

local function random_ip()
    return string.format("%d.%d.%d.%d", math.random(1,255), math.random(1,255), math.random(1,255), math.random(1,255))
end

request = function()
    local method = get_random(methods)
    local path = get_random(paths)
    local ua = get_random(uas)
    local ip = random_ip()
    
    local roll = math.random()
    if roll < 0.3 then
        path = path .. "?id=" .. get_random(random_strings) .. "&debug=true"
    elseif roll < 0.5 then
        path = get_random(random_paths)
    end

    local headers = {
        ["User-Agent"] = ua,
        ["X-Forwarded-For"] = ip,
        ["Accept"] = get_random(accepts),
        ["Accept-Language"] = get_random(languages),
        ["Referer"] = get_random(referers),
        ["Connection"] = "keep-alive",
        ["Cookie"] = get_random(random_cookies)
    }

    local body = ""
    if method == "POST" or method == "PUT" or method == "PATCH" then
        headers["Content-Type"] = "application/json"
        body = get_random(random_jsons)
    end

    return wrk.format(method, path, headers, body)
end

done = function(summary, latency, requests)
    io.write("\n================ ИТОГИ ХАОТИЧНОЙ DDOS-АТАКИ ================\n")
    local total = summary.requests
    local errors_http = summary.errors.status
    local ok_2xx = total - errors_http
    io.write(string.format("Всего запросов:             %d\n", total))
    io.write(string.format("Успешно проксировано (2xx): %d (%.1f%%)\n", ok_2xx, (ok_2xx / total) * 100))
    io.write(string.format("Отбито защитой (3xx/4xx/5xx): %d (%.1f%%)\n", errors_http, (errors_http / total) * 100))
    io.write(string.format("Ошибки сокетов / Таймауты:  connect=%d, read=%d, write=%d, timeout=%d\n",
        summary.errors.connect, summary.errors.read, summary.errors.write, summary.errors.timeout))
    io.write("===============================================================\n")
end