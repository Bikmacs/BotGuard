local counter = 0

local function random_ip()
    counter = counter + 1
    -- гарантированно уникальный IP
    return string.format("10.%d.%d.%d",
        math.floor(counter / 65536) % 256,
        math.floor(counter / 256) % 256,
        counter % 256)
end

request = function()
    return wrk.format("GET", "/", {
        ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        ["X-Forwarded-For"] = random_ip()
    })
end
