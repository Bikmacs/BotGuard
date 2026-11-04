FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/BotGuard.BanConsumer/BotGuard.BanConsumer.csproj", "src/BotGuard.BanConsumer/"]
COPY ["src/BotGuard.Shared/BotGuard.Shared.csproj", "src/BotGuard.Shared/"]
RUN dotnet restore "src/BotGuard.BanConsumer/BotGuard.BanConsumer.csproj"
COPY . .
WORKDIR "/src/src/BotGuard.BanConsumer"
RUN dotnet build "./BotGuard.BanConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BotGuard.BanConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BotGuard.BanConsumer.dll"]
