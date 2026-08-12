# syntax=docker/dockerfile:1.7
#
# ═══════════════════════════════════════════════════════════════════════
# Gestor Financiero — production image
#
# Multi-stage build:
#   1. `build`   — .NET SDK 10 restores + publishes the Web project
#   2. `runtime` — Alpine-based ASP.NET runtime with a non-root user
#
# Final image is ~110 MB base + published output. Optimised for Cloud Run:
#   - Listens on $PORT (defaults to 8080)
#   - Runs as UID 1000 (Cloud Run drops root anyway, but explicit is safer)
#   - HEALTHCHECK for local docker/docker-compose runs
#   - Server GC + tiered PGO for warm-up perf under sustained load
#
# Build:  docker build -t gestor-financiero:local .
# Run:    docker run --rm -p 8080:8080 --env-file .env gestor-financiero:local
# ═══════════════════════════════════════════════════════════════════════


# ═══════════════════════════════════════════════════════════════════════
# Stage 1 — restore + build + publish
#
# Using the Debian-based image (not Alpine) because .NET 10 + MudBlazor
# + record types with many parameters were tripping metadata loading on
# musl libc (TypeLoadException: 'Invalid_Token.0x02000040'). Debian is
# ~90 MB bigger but rock-solid.
# ═══════════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first so the restore layer is cached until
# a .csproj changes. Big win on iterative builds.
COPY GestorFinanciero.slnx ./
COPY src/GestorFinanciero.Domain/*.csproj         src/GestorFinanciero.Domain/
COPY src/GestorFinanciero.Application/*.csproj    src/GestorFinanciero.Application/
COPY src/GestorFinanciero.Infrastructure/*.csproj src/GestorFinanciero.Infrastructure/
COPY src/GestorFinanciero.Web/*.csproj            src/GestorFinanciero.Web/

RUN dotnet restore src/GestorFinanciero.Web/GestorFinanciero.Web.csproj \
    /p:TargetLatestRuntimePatch=true

# Now copy the sources and publish. NOTE: we intentionally do NOT pass
# --no-restore here. Even though we restored above, `dotnet publish` also
# runs an internal restore pass that regenerates project.assets.json against
# the sources just copied. Skipping that pass with --no-restore has been
# causing "Could not load type 'Invalid_Token.0x02000040' from assembly
# 'GestorFinanciero.Application'" TypeLoadExceptions at runtime — the restore
# cache is out of sync with the fresh compile output.
COPY src/ src/

RUN dotnet publish src/GestorFinanciero.Web/GestorFinanciero.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=false


# ═══════════════════════════════════════════════════════════════════════
# Stage 2 — minimal runtime
# ═══════════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# krb5-user silences the "libgssapi_krb5.so.2 not found" warning that
# Npgsql prints at cold start (it opportunistically tries to load
# Kerberos even under SCRAM/password auth).
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# The aspnet:10.0 image ships with a non-root `app` user (UID 1654 via
# APP_UID). We just switch to it — no adduser needed. Cloud Run enforces
# non-root anyway, but this makes the same image safe on any runtime.
#
# ICU is included in the base image — don't set
# DOTNET_SYSTEM_GLOBALIZATION_INVARIANT (needed for es-ES formatting).
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=1 \
    DOTNET_TieredPGO=1 \
    DOTNET_ReadyToRun=1 \
    DOTNET_NOLOGO=true

COPY --from=build --chown=app:app /app/publish/ ./

USER app

EXPOSE 8080

# Health check for local docker runs — Cloud Run uses its own probe.
# --spider makes wget exit non-zero on non-2xx responses.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "GestorFinanciero.Web.dll"]
