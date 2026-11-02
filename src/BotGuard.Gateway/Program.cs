using BotGuard.Gateway.Middleware;
using BotGuard.Gateway.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSingleton<UserAgentInspector>();
builder.Services.AddSingleton<InMemoryRateLimitService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddSingleton<RedisRateLimitService>();

var app = builder.Build();

app.UseMiddleware<BotGuardMiddleware>();

app.MapReverseProxy();

app.Run();