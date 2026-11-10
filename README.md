# Run Fit Platform

SaaS web para entrenadores independientes de running que planifican entrenamientos de running y fuerza para sus alumnos.

- **Backend:** ASP.NET Core 10 (Minimal APIs), monolito modular en `src/RunFitPlatform.Api`.
- **Base de datos:** PostgreSQL 18 con EF Core 10.
- **Frontend:** Angular 22 en `web/`.
- **Documentación:** `docs/` (arquitectura, diseño técnico y runbooks). Las reglas de trabajo están en `CLAUDE.md`.

## Requisitos

| Herramienta | Versión |
|---|---|
| SDK de .NET | 10.0.401 o un parche posterior de la banda 10.0.4xx |
| Node.js | 24 LTS (24.15 o posterior) |
| Docker Desktop | Para PostgreSQL, Mailpit y los tests de integración |

Certificado HTTPS de desarrollo (una sola vez por máquina):

```bash
dotnet dev-certs https --trust
```

## Primer arranque

```bash
# 1. Infraestructura local: PostgreSQL 18 y Mailpit
docker compose up -d

# 2. Herramientas y API (aplica las migraciones al arrancar en Development)
dotnet tool restore
dotnet run --project src/RunFitPlatform.Api

# 3. Angular, en otra terminal
cd web
npm ci
npm start
```

| Servicio | URL |
|---|---|
| Aplicación (Angular con proxy a la API) | http://localhost:4200 |
| API | https://localhost:5001 |
| Referencia de la API (solo Development) | https://localhost:5001/scalar/v1 |
| Health checks | https://localhost:5001/health/live y `/health/ready` |
| Mailpit (emails capturados) | http://localhost:8025 |

La configuración de desarrollo está en `src/RunFitPlatform.Api/appsettings.Development.json`, con credenciales que solo sirven en la máquina local. Cualquier credencial real va en User Secrets (`dotnet user-secrets`), nunca en el repositorio.

## Comandos habituales

```bash
dotnet build                                   # Compila y regenera openapi/v1.json (commitealo si cambia)
dotnet test                                    # Tests unitarios y de integración (requiere Docker)
dotnet format --verify-no-changes              # Formato, igual que en CI

dotnet ef migrations add <Nombre> --project src/RunFitPlatform.Api --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project src/RunFitPlatform.Api   # Revisar el SQL antes del PR

cd web
npm run lint && npm run format:check && npm test   # ESLint, Prettier y Vitest
npm run build                                       # Build de producción
npm run e2e                                         # Playwright (la primera vez: npx playwright install chromium)

docker build -t run-fit-platform .             # Imagen de producción (Angular + API)
```

## Estructura

```text
src/RunFitPlatform.Api/      API: Modules/<Módulo>, Shared/, Persistence/Migrations, openapi/v1.json
tests/                       Tests unitarios (con arquitectura) y de integración (Testcontainers)
web/                         Angular: src/app/core, shared, features/<área>; e2e/ con Playwright
docs/                        architecture/, design/, runbooks/
.github/                     CI (ci.yml), despliegue (deploy.yml) y Dependabot
```

## Flujo de trabajo

- Rama principal `master`. Ramas cortas `feat/`, `fix/`, `chore/` y `docs/`; Conventional Commits; merge por squash con CI en verde.
- Todo endpoint nuevo necesita su fila en la matriz de autorización (`tests/RunFitPlatform.Api.IntegrationTests/Authorization`).
- Las reglas de módulos se verifican con tests de arquitectura: si uno falla, se corrige el código, no el test.
