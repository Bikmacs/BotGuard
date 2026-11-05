local counter = 0
local user_agents = {
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64)", 
    "Googlebot/2.1 (+http://www.google.com/bot.html)",
    "python-requests/2.31",
    "curl/7.68.0"
}

request = function()
    counter = counter + 1
    local ua = user_agents[math.random(#user_agents)]
    
    -- 80% запросов идут с 10 повторяющихся IP (триггерят бан)
    -- 20% запросов идут со случайных IP (имитация ботнета)
    local ip
    if math.random() < 0.8 then
        ip = "10.0.0." .. math.random(1, 10)
    else
        ip = string.format("192.168.%d.%d", math.random(1, 255), math.random(1, 255))
    end

    return wrk.format("GET", "/", {
        ["User-Agent"] = ua,
        ["X-Forwarded-For"] = ip
    })
end