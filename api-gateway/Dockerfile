# Build context: api-gateway/
# docker build -t carelink-gateway -f Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CareLink.ApiGateway.csproj .
RUN dotnet restore CareLink.ApiGateway.csproj

COPY . .
RUN dotnet publish CareLink.ApiGateway.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl: used by container/orchestrator health probes (base image has none).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The aspnet base image listens on 8080 and ships a non-root "app" user.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CareLink.ApiGateway.dll"]
