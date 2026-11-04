FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/BotGuard.Gateway/BotGuard.Gateway.csproj", "src/BotGuard.Gateway/"]
COPY ["src/BotGuard.Shared/BotGuard.Shared.csproj", "src/BotGuard.Shared/"]
RUN dotnet restore "src/BotGuard.Gateway/BotGuard.Gateway.csproj"
COPY . .
WORKDIR "/src/src/BotGuard.Gateway"
RUN dotnet build "./BotGuard.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BotGuard.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BotGuard.Gateway.dll"]
