# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first so `dotnet restore` is cached across builds unless a
# dependency actually changes.
COPY src/CurrencyWatchlist.Domain/CurrencyWatchlist.Domain.csproj src/CurrencyWatchlist.Domain/
COPY src/CurrencyWatchlist.Application/CurrencyWatchlist.Application.csproj src/CurrencyWatchlist.Application/
COPY src/CurrencyWatchlist.Infrastructure/CurrencyWatchlist.Infrastructure.csproj src/CurrencyWatchlist.Infrastructure/
COPY src/CurrencyWatchlist.Api/CurrencyWatchlist.Api.csproj src/CurrencyWatchlist.Api/
RUN dotnet restore src/CurrencyWatchlist.Api/CurrencyWatchlist.Api.csproj

COPY src/ src/
RUN dotnet publish src/CurrencyWatchlist.Api/CurrencyWatchlist.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Matches the base image's default non-root user and HTTP port (ASPNETCORE_HTTP_PORTS=8080)
# for local/docker-compose use, but honors a platform-injected $PORT (Render, and most other
# PaaS providers) when present, so the same image works unmodified in both places.
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet CurrencyWatchlist.Api.dll"]
