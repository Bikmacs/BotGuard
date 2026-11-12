local thread_counter = 1
function setup(thread)
    thread:set("seed", os.time() + thread_counter * 1000)
    thread_counter = thread_counter + 1
end

local happy_uas, bad_uas, honeypot_paths
local happy_ips, waf_ips, rate_ips, botnet_ips
local cookies, jsons

function init(args)
    math.randomseed(seed)
    
    happy_uas = {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15",
        "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
        "Mozilla/5.0 (compatible; YandexBot/3.0; +http://yandex.com/bots)"
    }
    
    bad_uas = {
        "python-requests/2.31",
        "curl/7.68.0",
        "Nikto/2.5.0",
        "sqlmap/1.7.2"
    }
    
    honeypot_paths = {
        "/.env", "/wp-admin/setup.php", "/backup.zip", 
        "/phpmyadmin", "/.git/config", "/dump.sql"
    }
    
    happy_ips = {}
    for i = 1, 20000 do
        happy_ips[i] = string.format("10.%d.%d.%d", (i / 65536) % 256, (i / 256) % 256, i % 256)
    end
    
    botnet_ips = {}
    for i = 1, 20000 do
        botnet_ips[i] = string.format("40.%d.%d.%d", (i / 65536) % 256, (i / 256) % 256, i % 256)
    end
    
    waf_ips = {}
    for i = 1, 50 do
        waf_ips[i] = "20.20.0." .. i
    end
    
    rate_ips = {}
    for i = 1, 10 do
        rate_ips[i] = "30.30.0." .. i
    end
    
    cookies = {"session=a1b2c3d4e5f6; csrftoken=xyz123"}
    jsons = {'{"username":"admin","password":"test"}'}
end

local function pick(arr)
    return arr[math.random(#arr)]
end

request = function()
    local roll = math.random(100)
    
    if roll <= 40 then
        return wrk.format("GET", "/", {
            ["User-Agent"] = pick(happy_uas),
            ["X-Forwarded-For"] = pick(happy_ips)
        })
    elseif roll <= 65 then
        return wrk.format("GET", pick(honeypot_paths), {
            ["User-Agent"] = pick(bad_uas),
            ["X-Forwarded-For"] = pick(waf_ips)
        })
    elseif roll <= 85 then
        return wrk.format("GET", "/", {
            ["User-Agent"] = "Mozilla/5.0",
            ["X-Forwarded-For"] = pick(rate_ips)
        })
    else
        return wrk.format("GET", "/", {
            ["User-Agent"] = "Mozilla/5.0",
            ["X-Forwarded-For"] = pick(botnet_ips)
        })
    end
end