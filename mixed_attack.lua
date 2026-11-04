local counter = 0
local user_agents = {
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
    "Googlebot/2.1 (+http://www.google.com/bot.html)",
    "python-requests/2.31",
    "curl/7.68.0"
}

request = function()
    counter = counter + 1
    local ua = user_agents[math.random(#user_agents)]
    
    -- Имитируем 100 разных IP-адресов (они будут повторяться)
    local ip_id = math.random(1, 100)
    local ip = "192.168.1." .. ip_id
    
    return wrk.format("GET", "/", {
        ["User-Agent"] = ua,
        ["X-Forwarded-For"] = ip
    })
end