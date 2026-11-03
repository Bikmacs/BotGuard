using BotGuard.Gateway.Middleware;
using BotGuard.Gateway.Services;
using MassTransit;
using StackExchange.Redis;
using BotGuard.Shared.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSingleton<UserAgentInspector>();

builder.Services.AddMassTransit(x =>
    x.UsingRabbitMq((context, cfg) =>
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        })));

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var redisOptions = ConfigurationOptions.Parse(redisConnectionString);

redisOptions.AbortOnConnectFail = false;

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddSingleton<RedisRateLimitService>();

var app = builder.Build();

app.UseMiddleware<BotGuardMiddleware>();

app.MapReverseProxy();

app.Run();