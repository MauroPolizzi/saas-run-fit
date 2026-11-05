# CLAUDE.md

Guía para trabajar en este repositorio. Resume las decisiones aprobadas por Mauro el 29/10/2026 y las reglas que se aplican a todo cambio. El detalle y la justificación están en `docs/`; si algo de este archivo contradice a `docs/`, avisá y no elijas por tu cuenta.

## Producto

SaaS web para entrenadores independientes de running que planifican entrenamientos de running y fuerza para sus alumnos. Mercado inicial: Argentina. El entrenador paga una suscripción mensual; el alumno usa la plataforma gratis.

- **Entrenador:** perfil, grupos, invitar alumnos, plantillas, planificación semanal, asignación individual o a grupo, revisión de lo realizado, progreso.
- **Alumno:** su semana, registrar lo realizado, historial, perfil deportivo, datos de salud (con consentimiento), comentarios.
- **Administrador de plataforma:** panel mínimo (invitar entrenadores, suscripciones, cuentas, bajas, auditoría).

Idioma de la interfaz y la documentación: **español**. Código, base de datos y nombres técnicos: **inglés**. Sistema métrico, ritmo en min/km, zona horaria `America/Argentina/Buenos_Aires`. Sin i18n en el MVP.

## Estado del proyecto

- Fase 1 (arquitectura) y Fase 2 (diseño técnico): documentadas en `docs/`.
- Fase 3 (estructura inicial): pendiente. **No implementes funcionalidades de negocio hasta que la estructura inicial esté aprobada y mergeada.**
- Pendientes: ver "Decisiones abiertas" al final.

## Fuentes de verdad

| Tema | Documento |
|---|---|
| Decisiones y su porqué | `docs/architecture/architecture-decision-records.md` (ADR-001 a ADR-010) |
| Evaluación completa | `docs/architecture/architecture-evaluation.md` |
| Dominio conceptual | `docs/architecture/domain-model.md` |
| Seguridad y privacidad | `docs/architecture/security-and-privacy.md` |
| Estructura, módulos, convenciones, configuración, errores | `docs/design/technical-design.md` |
| Autenticación, autorización, admin, salud | `docs/design/auth-and-authorization.md` |
| Tablas, restricciones, filtros, migraciones | `docs/design/data-model.md` |
| Endpoints y validación | `docs/design/api-contracts.md` |
| Pruebas | `docs/design/testing-strategy.md` |
| CI/CD | `docs/design/ci-cd.md` |
| Hitos | `docs/architecture/implementation-roadmap.md` |

Antes de un cambio grande, leé el documento del área. Si el cambio altera una decisión, **proponé una ADR nueva o una actualización y esperá aprobación**; no cambies una decisión aprobada en silencio.

## Stack y versiones

| Componente | Versión |
|---|---|
| .NET / ASP.NET Core | 10 (LTS), fijado en `global.json` |
| EF Core | 10.x con `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x y `EFCore.NamingConventions` 10.x |
| PostgreSQL | 18 (imagen `postgres:18` en local y tests) |
| Angular | 22 (standalone, signals, Angular Material + CDK) |
| Node.js | 24 LTS |
| Tests | xUnit v3, Testcontainers.PostgreSql, `WebApplicationFactory`, Vitest, Playwright |
| CI | GitHub Actions |

Reglas de dependencias:

- Versiones NuGet centralizadas en `Directory.Packages.props`; npm con `package-lock.json`.
- Cada versión nueva se fija exacta y con **al menos dos semanas de publicada**.
- **No agregues dependencias sin justificarlo en el PR.** Prohibidas por decisión: MediatR, AutoMapper, librerías de CQRS o event sourcing, repositorios genéricos, NgRx. FluentValidation solo si la validación integrada no alcanza, y con aprobación.

## Comandos

Se confirman al crear la estructura (Fase 3); actualizá esta sección si cambian.

```bash
docker compose up -d                       # PostgreSQL 18 + Mailpit
dotnet build                               # Solución completa
dotnet test                                # Unitarios + integración (requiere Docker)
dotnet format --verify-no-changes          # Formato, igual que en CI
dotnet run --project src/CoachPlatform.Api # API en https://localhost
dotnet ef migrations add <Nombre> --project src/CoachPlatform.Api --output-dir Persistence/Migrations
cd web && npm ci && npm start              # Angular con proxy a la API
cd web && npm run lint && npm test         # Lint + Vitest
cd web && npx playwright test              # E2E
```

## Estructura del repositorio

```text
src/CoachPlatform.Api/          Único proyecto de producción
  Modules/<Módulo>/             Accounts, Coaching, Planning, TrainingLog, Progress, Billing, Admin
    <Módulo>Module.cs           Registro de servicios y endpoints
    Contracts/                  Lo único visible para otros módulos
    Domain/                     Entidades y reglas; sin ASP.NET ni EF
    Features/<Área>/<CasoDeUso>.cs   Request, Response, Handler y Map en un archivo
    Persistence/                Configuraciones de EF del módulo
  Shared/                       AppDbContext, seguridad, errores, email, auditoría, CLI
  Persistence/Migrations/       Historial único de migraciones
  openapi/v1.json               Contrato generado en build y versionado
tests/CoachPlatform.Api.UnitTests/
tests/CoachPlatform.Api.IntegrationTests/
web/                            Angular: core/, shared/, features/<área>/, e2e/
docs/                           architecture/, design/, runbooks/
```

**No crees proyectos .NET nuevos** (por módulo, por capa, "Common", etc.) sin una ADR aprobada.

## Arquitectura: reglas de módulos

Monolito modular en un solo proyecto (ADR-004). Las reglas se verifican con tests de arquitectura; si un test falla, corregí el código, no el test.

1. Un módulo usa de otro **solo** los tipos de su carpeta `Contracts`.
2. Dependencias permitidas: Coaching → Accounts, Billing · Planning → Coaching, Billing · TrainingLog → Planning, Coaching · Progress → Coaching · Admin → Accounts, Coaching, Billing · todos → Shared.
3. Excepción única: **Progress** puede leer entidades de `TrainingLog.Domain` y `Planning.Domain` con `AsNoTracking`. Nunca escribe.
4. **Admin no depende de Planning, TrainingLog, Progress ni de los tipos de salud de Coaching.**
5. `Domain` no referencia `Microsoft.AspNetCore.*` ni `Microsoft.EntityFrameworkCore.*`.
6. Sin propiedades de navegación de EF entre módulos: se referencian por id.
7. Cada módulo modifica solo sus tablas (esquema PostgreSQL propio).
8. Sin ciclos, sin eventos de dominio ni bus en el MVP: llamadas en proceso a `Contracts` dentro de la misma transacción.

Running y fuerza viven en el mismo módulo **Planning** (no los separes).

## Backend: convenciones

- **Minimal APIs** con `MapGroup` por módulo (ADR-002). Un archivo por caso de uso en `Features/`, con clase estática contenedora (`AssignTemplate`) que anida `Request`, `Response`, `Handler` y `Map`.
- `DbContext` directo en los handlers; sin capa de repositorio.
- Resultados esperados con `TypedResults` (`Results<Ok<T>, NotFound, ...>`). **No uses excepciones para el flujo normal.**
- Errores siempre como `ProblemDetails` con `traceId` y `code` (`<módulo>.<motivo>`, por ejemplo `planning.relationship_inactive`). Estados: 400 forma, 422 regla de negocio, 404 inexistente **o ajeno**, 409 concurrencia o unicidad, 403 rol o plan.
- Validación de forma con la validación integrada de .NET 10 (`AddValidation`, `[ValidatableType]`, DataAnnotations, `IValidatableObject`). Reglas de negocio en el handler o el dominio. Límites en `docs/design/api-contracts.md`.
- JSON: camelCase, enums como string camelCase, propiedades desconocidas rechazadas, cuerpo máximo 256 KB.
- Fechas de calendario `DateOnly` (`yyyy-MM-dd`); instantes `DateTimeOffset` UTC. Usá `TimeProvider`, nunca `DateTime.Now`.
- Unidades del contrato: metros, segundos, s/km, kg. El formateo es del frontend.
- Ids: `Guid.CreateVersion7()` generados en la aplicación.
- Concurrencia optimista con `xmin` en recursos editables; el cliente envía `version`.
- Rutas: `/api/coach/...` (entrenador), `/api/me/...` (alumno), `/api/admin/...` (administración), `/api/auth/...`, `/api/invitations/...`. Plural y kebab-case.
- OpenAPI integrado (`Microsoft.AspNetCore.OpenApi`); la UI visual solo en Development. Si cambiás un contrato, regenerá y commiteá `openapi/v1.json`.

## Datos y persistencia

- PostgreSQL 18, un `AppDbContext`, esquema por módulo (`identity`, `coaching`, `planning`, `training_log`, `billing`, `admin`), *snake_case*.
- **Contenido de sesiones normalizado** (ADR-007): `workouts` → `workout_steps` (árbol de un nivel con `parent_step_id`; una repetición no contiene otra), `workout_exercises` → `workout_sets`. La API expone el workout como árbol anidado; el almacenamiento es interno.
- **Asignar copia en profundidad** el workout de la plantilla para cada alumno. Nunca compartas filas de contenido entre sesiones o alumnos.
- **Historial inmutable:** un registro realizado bloquea la `planned_session_version` que referencia. Editar una sesión con versión bloqueada crea una versión nueva. Nunca modifiques una versión bloqueada ni un registro por cambios del plan.
- Toda tabla de negocio lleva `coach_id`. FK compuestas `(x_id, coach_id)` impiden mezclar datos de entrenadores; no las quites.
- Filtros globales **nombrados** de EF Core: `DataScope` y `NotArchived`. **Prohibido `IgnoreQueryFilters()` sin nombres**; `IgnoreQueryFilters(["DataScope"])` solo en casos revisados y listados.
- Sin borrado físico de datos de negocio (archivar o cambiar estado), salvo baja de cuenta, revocación de consentimiento de salud y comentarios propios.
- Migraciones: EF Core code-first, historial único, nombre descriptivo (`AddPlanningWorkouts`). Revisá el SQL generado. **Nunca migrar al arrancar en producción** (solo `Database:MigrateOnStartup` en Development). Cambios *expand/contract*; sin `Down` en producción. Restricciones que EF no expresa, con `migrationBuilder.Sql` dentro de la migración.
- No edites una migración ya mergeada; creá una nueva.

## Seguridad (no negociable)

- **ASP.NET Core Identity + cookie** `__Host-cp.session` (HttpOnly, Secure, SameSite=Strict). **Sin JWT, sin tokens en `localStorage`** (ADR-005). Angular y API en el mismo origen.
- La autenticación en `/api` devuelve 401/403, nunca redirige.
- Antiforgery: cookie `XSRF-TOKEN` desde `GET /api/auth/antiforgery`, cabecera `X-XSRF-TOKEN`, filtro de endpoint en el grupo `/api` que valida POST, PUT, PATCH y DELETE (login incluido).
- `FallbackPolicy` = usuario autenticado. Un endpoint anónimo debe estar en la lista permitida del test de autorización.
- **El rol no alcanza:** cada handler verifica el recurso concreto (relación activa entrenador-alumno, dueño del registro). Recurso ajeno → **404**, no 403.
- **Todo endpoint nuevo necesita su caso en la matriz de autorización** (test de integración). Sin eso, CI falla.
- Invitaciones: token de 32 bytes, solo se guarda su hash SHA-256, un uso, 7 días, el email de la cuenta debe coincidir. Tokens en el **fragmento** del enlace (`#t=...`), nunca en la ruta ni en la query.
- Rol `PlatformAdmin` solo por comando de línea. Admin con **2FA obligatoria**, sesión de 8 h, todas sus acciones en `admin.audit_events`. Admin **no ve** sesiones, registros, comentarios ni datos de salud, y no suplanta usuarios.
- **Datos de salud** (ADR-010): tabla `coaching.athlete_health_info`, solo con consentimiento vigente; revocar borra los datos; entrenador solo lectura y con relación activa; nunca en logs, errores, emails ni panel de administración; auditar consentimiento y cambios sin valores.
- Secretos: nunca en el repositorio ni en `appsettings*.json`. User Secrets en desarrollo, variables de entorno en producción, secretos de GitHub en CI.
- Logs: sin contraseñas, tokens, cookies, cuerpos de petición, datos de salud ni notas de entrenamiento; emails enmascarados. `EnableSensitiveDataLogging` solo en Development.
- No escribas criptografía propia ni almacenes contraseñas fuera de Identity.
- Rate limiting en login, recuperación, aceptación de invitaciones y administración.

## Frontend

- Angular 22, componentes standalone, signals; `httpResource` para lecturas. Sin NgRx.
- Formularios: Signal Forms para el editor de workout; Reactive Forms tipados si Signal Forms no alcanza (avisá antes de cambiar).
- Angular Material + CDK. Vista semanal propia con CSS Grid (sin librería de calendario). Gráficos con Chart.js.
- Sin SSR, sin PWA, sin service worker en el MVP.
- Llamadas a rutas relativas `/api/...`; en desarrollo, `proxy.conf.json`. XSRF con `withXsrfConfiguration`.
- Tipos de la API generados desde `openapi/v1.json`; no los escribas a mano.
- Los guards son solo UX; la seguridad está en la API.
- Mensajes al usuario en español, mapeados desde el `code` de `ProblemDetails`.
- `features/*` no se importan entre sí; lo común va a `shared/` o `core/`. `features/admin` con carga diferida.
- Pantallas del alumno mobile-first; editor del entrenador desktop-first. Accesibilidad: contraste AA, teclado, etiquetas con texto.

## Pruebas

- Cada cambio de comportamiento viene con tests. Prioridad: aislamiento entre entrenadores, permisos de alumnos, historial inmutable, asignación solo con relación activa, datos de salud y administración.
- Tests de integración contra **PostgreSQL real** (Testcontainers), con migraciones aplicadas desde cero. Prohibido el proveedor en memoria de EF.
- Usuarios de test creados por los flujos reales de invitación y login con cookies.
- Sin meta de cobertura; no agregues tests triviales para subirla.
- Nunca saltees, deshabilites ni marques como omitido un test para que CI pase.
- E2E con Playwright para los flujos críticos (ver `docs/design/testing-strategy.md`).

## Calidad, Git y PR

- `Nullable` habilitado; warnings como errores en CI; `dotnet format`, ESLint y Prettier sin cambios pendientes.
- Ramas `feat/`, `fix/`, `chore/`, `docs/` desde `main`; Conventional Commits; merge por squash con CI en verde.
- Un PR por cambio coherente. La descripción explica el porqué, las migraciones incluidas y cómo se probó.
- Si un cambio toca seguridad, datos de salud, migraciones o contratos de API, decilo explícitamente en el PR.
- Mantené `docs/` al día cuando cambie lo que describen.

## Fuera del MVP (no implementar sin aprobación)

Integraciones con Strava o Garmin, apps nativas, chat en tiempo real, notificaciones push, nutrición, recomendaciones con IA, algoritmos avanzados de rendimiento, pagos reales, microservicios, Kubernetes, colas o mensajería, caché distribuida, SSR, PWA, login social, enlace copiable de invitación, entidad `TrainingBlock`, Row Level Security.

## Cómo trabajar

- Ante una duda de producto o una decisión no cubierta aquí ni en `docs/`, preguntá antes de asumir.
- No agregues complejidad "por si acaso": resolvé el caso de uso actual con la estructura existente.
- Informá con precisión qué comandos ejecutaste y su resultado; no afirmes que algo compila o pasa los tests si no lo ejecutaste.
- No ejecutes comandos destructivos ni cambies datos de producción.

## Decisiones abiertas

| Tema | Estado | Impacto |
|---|---|---|
| D6. Hosting y región (Render en EE. UU., Fly.io + Neon en São Paulo, o Azure) | En evaluación | Paso de despliegue de `deploy.yml`, proxy inverso, ejecución de migraciones y del comando de administrador |
| Nombre técnico de la solución | Provisorio: `CoachPlatform` | Namespaces y nombres de proyectos; definir antes de crear la estructura |
| Repositorio público o privado | Sin definir | Cuota de GitHub Actions y escaneo de secretos |
| Proveedor de email | Recomendado: Resend | Implementación de `IEmailSender` (Hito 0) |
| Revisión legal (datos de salud, privacidad, términos) | Pendiente | Requisito antes del piloto con usuarios reales |
