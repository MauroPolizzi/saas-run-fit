# syntax=docker/dockerfile:1

# ---- Etapa 1: Angular -------------------------------------------------------
FROM node:24.22.2-bookworm-slim AS web
WORKDIR /src/web
COPY web/package.json web/package-lock.json web/.npmrc ./
RUN npm ci --no-audit --no-fund
COPY web/ ./
RUN npm run build

# ---- Etapa 2: API .NET ------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0.402-noble AS api
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/RunFitPlatform.Api/RunFitPlatform.Api.csproj src/RunFitPlatform.Api/packages.lock.json src/RunFitPlatform.Api/
RUN dotnet restore src/RunFitPlatform.Api/RunFitPlatform.Api.csproj --locked-mode
COPY src/ src/
COPY --from=web /src/web/dist/web/browser/ src/RunFitPlatform.Api/wwwroot/
# openapi/v1.json ya está versionado: no se regenera al publicar.
RUN dotnet publish src/RunFitPlatform.Api/RunFitPlatform.Api.csproj \
      --configuration Release --no-restore --output /app \
      -p:OpenApiGenerateDocuments=false

# ---- Etapa 3: runtime -------------------------------------------------------
# Chiseled: sin shell ni gestor de paquetes, usuario no root por defecto.
# Variante "extra": incluye ICU y tzdata (zona America/Argentina/Buenos_Aires).
FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-noble-chiseled-extra AS runtime
WORKDIR /app
COPY --from=api /app ./
USER app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
EXPOSE 8080
# Sin curl en la imagen: el propio binario consulta /health/live.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD ["dotnet", "RunFitPlatform.Api.dll", "healthcheck"]
ENTRYPOINT ["dotnet", "RunFitPlatform.Api.dll"]
