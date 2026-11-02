using BotGuard.Gateway.Middleware;
using BotGuard.Gateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSingleton<UserAgentInspector>();
builder.Services.AddSingleton<InMemoryRateLimitService>();

var app = builder.Build();

app.UseMiddleware<BotGuardMiddleware>();

app.MapReverseProxy();

app.Run();