using BotGuard.Gateway.Middleware;
using BotGuard.Gateway.Services;
using BotGuard.Shared.Options;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration.GetConnectionString("Seq") ?? "http://seq:5341"));

builder.Services.AddSingleton<UserAgentInspector>();

var rabbitOptions = builder.Configuration
    .GetSection(RabbitMqOptions.RabbitMq)
    .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((_, cfg) =>
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


builder.Services.AddSingleton<HoneypotInspector>();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddSingleton<RedisRateLimitService>();

var app = builder.Build();

var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedOptions);
app.UseMiddleware<BotGuardMiddleware>();

app.MapReverseProxy();

app.Run();