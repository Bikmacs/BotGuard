using System;
using BotGuard.BanConsumer;
using BotGuard.BanConsumer.Data;
using BotGuard.Shared.Options;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
redisOptions.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));


var pgConnection = builder.Configuration.GetConnectionString("Postgres")
                   ?? throw new InvalidOperationException(
                       "ConnectionStrings:Postgres is not configured. In Docker set ConnectionStrings__Postgres.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(pgConnection));

var rabbitOptions = builder.Configuration
    .GetSection(RabbitMqOptions.RabbitMq)
    .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AbuseDetectedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitOptions.Host, rabbitOptions.Port, "/", h =>
        {
            h.Username(rabbitOptions.Username);
            h.Password(rabbitOptions.Password);
        });
        cfg.ReceiveEndpoint("ban-queue", e =>
        {
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15)
            ));
            e.ConfigureConsumer<AbuseDetectedConsumer>(context);
        });
    });
});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

host.Run();