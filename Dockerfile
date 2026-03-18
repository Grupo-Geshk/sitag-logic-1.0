# ── Stage 1: build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (layer cache for restore)
COPY SITAG_1.0.slnx ./
COPY SITAG_1.0/src/SITAG.Api/SITAG.Api.csproj               SITAG_1.0/src/SITAG.Api/
COPY SITAG_1.0/src/SITAG.Application/SITAG.Application.csproj SITAG_1.0/src/SITAG.Application/
COPY SITAG_1.0/src/SITAG.Domain/SITAG.Domain.csproj         SITAG_1.0/src/SITAG.Domain/
COPY SITAG_1.0/src/SITAG.Infrastructure/SITAG.Infrastructure.csproj SITAG_1.0/src/SITAG.Infrastructure/

RUN dotnet restore SITAG_1.0/src/SITAG.Api/SITAG.Api.csproj

# Copy the rest of the source and publish
COPY SITAG_1.0/src/ SITAG_1.0/src/

RUN dotnet publish SITAG_1.0/src/SITAG.Api/SITAG.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Railway injects PORT at runtime; the app reads it in Program.cs
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SITAG.Api.dll"]
