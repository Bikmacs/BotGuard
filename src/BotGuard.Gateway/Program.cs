using BotGuard.Gateway.Middleware;
using BotGuard.Gateway.Services;
using BotGuard.Shared.Options;
using MassTransit;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSingleton<UserAgentInspector>();

var rabbitOptions = builder.Configuration
    .GetSection(RabbitMqOptions.RabbitMq)
    .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitOptions.Host, rabbitOptions.Port, "/", h => 
        {
            h.Username(rabbitOptions.Username);
            h.Password(rabbitOptions.Password); 
        });
    });
});

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var redisOptions = ConfigurationOptions.Parse(redisConnectionString);

redisOptions.AbortOnConnectFail = false;

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddSingleton<RedisRateLimitService>();

var app = builder.Build();

// Для тестирования
/*var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownProxies.Clear();
forwardedOptions.KnownNetworks.Clear();
app.UseForwardedHeaders(forwardedOptions);*/

app.UseMiddleware<BotGuardMiddleware>();

app.MapReverseProxy();

app.Run();