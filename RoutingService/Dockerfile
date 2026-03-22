FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/RoutingService.Application/RoutingService.Application.csproj src/RoutingService.Application/
COPY src/RoutingService.Infrastructure/RoutingService.Infrastructure.csproj src/RoutingService.Infrastructure/
COPY src/RoutingService.Api/RoutingService.Api.csproj src/RoutingService.Api/

RUN dotnet restore src/RoutingService.Api/RoutingService.Api.csproj

COPY src/ src/
RUN dotnet publish src/RoutingService.Api/RoutingService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=5 \
  CMD curl -fsS http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "RoutingService.Api.dll"]
