using BotGuard.BanConsumer;
using BotGuard.BanConsumer.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// 1. Подключаем Redis (чтобы воркер мог банить)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));


var pgConnection = builder.Configuration.GetConnectionString("PostgresSQL") ??
                   "Host=localhost;Database=botguard;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(pgConnection));


// 2. Настраиваем MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AbuseDetectedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Создаем очередь ban-queue (как в PDF)
        cfg.ReceiveEndpoint("ban-queue", e => { e.ConfigureConsumer<AbuseDetectedConsumer>(context); });
    });
});


var host = builder.Build();
host.Run();