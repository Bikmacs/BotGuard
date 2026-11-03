using BotGuard.BanConsumer;
using BotGuard.Gateway.Middleware;
using BotGuard.Gateway.Services;
using MassTransit;
using StackExchange.Redis;
using BotGuard.Shared.Events;
using BotGuard.Shared.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSingleton<UserAgentInspector>();

var rabbitOptions = builder.Configuration
    .GetSection(RabbitMqOptions.RabbitMq)
    .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AbuseDetectedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username(rabbitOptions.Username);
            h.Username(rabbitOptions.Password);
        });
        cfg.ReceiveEndpoint(
            "ban-queue", e => { e.ConfigureConsumer<AbuseDetectedConsumer>(context); });
    });
});

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var redisOptions = ConfigurationOptions.Parse(redisConnectionString);

redisOptions.AbortOnConnectFail = false;

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddSingleton<RedisRateLimitService>();

var app = builder.Build();

app.UseMiddleware<BotGuardMiddleware>();

app.MapReverseProxy();

app.Run();