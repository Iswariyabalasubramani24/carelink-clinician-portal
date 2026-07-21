# Build context: CareLink.CleanArchitecture/
# docker build -t carelink-api -f Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first with only the project files so the package graph is cached
# across source-only changes.
COPY CareLink.Domain/CareLink.Domain.csproj CareLink.Domain/
COPY CareLink.Application/CareLink.Application.csproj CareLink.Application/
COPY CareLink.Infrastructure/CareLink.Infrastructure.csproj CareLink.Infrastructure/
COPY CareLink.API/CareLink.API.csproj CareLink.API/
RUN dotnet restore CareLink.API/CareLink.API.csproj

COPY . .
RUN dotnet publish CareLink.API/CareLink.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# libfontconfig1: QuestPDF renders via SkiaSharp, whose Linux native library
# needs fontconfig at runtime (report generation crashes without it).
# curl: used by container/orchestrator health probes (base image has none).
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The aspnet base image listens on 8080 and ships a non-root "app" user.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CareLink.API.dll"]
