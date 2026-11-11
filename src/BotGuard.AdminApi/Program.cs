using BotGuard.AdminApi.Features.Incidents;
using Microsoft.EntityFrameworkCore;
using BotGuard.Shared.Data;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

var redisConnection= builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));


builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGetIncidentsEndpoint();

app.MapGet("/health", () => "AdminAPI is running");

app.UseHttpsRedirection();


app.Run();