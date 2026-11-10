# Autenticación y autorización

> Plataforma SaaS para entrenadores de running y fuerza. Fase 2, diseño técnico aprobado (Fase 3 iniciada el 05/11/2026). Versiones verificadas el 29/10/2026.

## 7. Autenticación y autorización

### 7.1. Componentes

- **ASP.NET Core Identity** "core" (`AddIdentityCore<AppUser>` con roles, almacenamiento EF Core, `SignInManager` y proveedores de tokens por defecto) + esquema de cookies de aplicación de Identity. Sin la UI de Identity: las pantallas las hace Angular.
- **Endpoints propios** en `Accounts` en lugar de `MapIdentityApi`: el alta pasa siempre por una invitación y necesitamos controlar respuestas, auditoría y límites.
- **Roles:** `Coach`, `Athlete`, `PlatformAdmin`. `Coach` y `Athlete` se asignan al aceptar la invitación correspondiente. `PlatformAdmin` solo por comando de línea.
- `CoachId` y `AthleteId` **coinciden con el id del usuario** (los perfiles usan el id de usuario como clave), lo que evita claims adicionales.

### 7.2. Cookie de sesión

| Propiedad | Valor | Motivo |
|---|---|---|
| Nombre | `__Host-cp.session` | El prefijo `__Host-` obliga a `Secure`, `Path=/` y sin `Domain`: la cookie no se comparte con subdominios |
| `HttpOnly` | Sí | Inaccesible para JavaScript |
| `SameSite` | `Strict` | La SPA hace sus llamadas desde el mismo sitio, así que la cookie viaja; un sitio externo no puede provocar peticiones con sesión. Al llegar desde un enlace de email, la navegación inicial no lleva cookie, pero la SPA llama a `/api/auth/me` desde el propio sitio y la sesión se recupera |
| Duración | 14 días deslizantes con "Recordarme"; de sesión sin él | Alumnos que entran desde el móvil varias veces por semana |
| Administrador | 8 horas absolutas, sin renovación deslizante | Superficie más sensible |
| Revalidación | *Security stamp* cada 5 minutos | Cambiar la contraseña o deshabilitar una cuenta cierra las sesiones abiertas en minutos |
| Respuesta sin sesión | 401/403 en `/api` (sin redirección) | Evita respuestas HTML en una API |

En desarrollo local se usa HTTPS (`dotnet dev-certs`), necesario por el prefijo `__Host-`.

### 7.3. Protección CSRF

Según la documentación de ASP.NET Core 10 [Verificado]:

1. `AddAntiforgery(o => o.HeaderName = "X-XSRF-TOKEN")`.
2. `GET /api/auth/antiforgery` emite la cookie legible `XSRF-TOKEN`. Angular la llama al iniciar y **después de login y logout**, porque el token queda ligado a la identidad.
3. Angular envía la cabecera `X-XSRF-TOKEN` automáticamente (`provideHttpClient(withXsrfConfiguration(...))`) en peticiones que modifican datos a rutas relativas.
4. Un **filtro de endpoint en el grupo `/api`** llama a `IAntiforgery.ValidateRequestAsync` en POST, PUT, PATCH y DELETE. Es necesario porque el middleware de antiforgery solo valida automáticamente endpoints con datos de formulario, no los que reciben JSON.
5. El login también valida el token (protección contra *login CSRF*).

Al migrar a .NET 11 se reevalúa la nueva protección CSRF automática que describe la documentación para esa versión.

### 7.4. Flujos

| Flujo | Pasos clave |
|---|---|
| **Invitar entrenador** (administrador) | Crea `PlatformInvitation` → email con enlace `{PublicBaseUrl}/invitacion#t={token}` → el entrenador define nombre y contraseña → se crean usuario (email confirmado), rol `Coach`, `CoachProfile` y `CoachSubscription` en `Trial` → inicia sesión |
| **Invitar alumno** (entrenador) | Crea `Invitation` (grupo opcional) → email → el alumno crea su cuenta, o inicia sesión si ya existe con ese email → se crean `AthleteProfile` si falta, `CoachAthlete` activa y membresía al grupo → inicia sesión |
| Login | Email + contraseña (+ código TOTP si la cuenta tiene 2FA) → cookie. Mensaje de error idéntico para usuario inexistente o contraseña incorrecta |
| Recuperación | `forgot-password` responde siempre 202 → email con enlace `{PublicBaseUrl}/restablecer#u={userId}&t={token}` → nueva contraseña → se invalidan las sesiones |
| 2FA | Configuración con app autenticadora (clave + URI para QR) → confirmación con código → códigos de recuperación. **Obligatoria** para `PlatformAdmin`, opcional para entrenadores |
| Logout | Borra la cookie de sesión y renueva el token antiforgery |

**Tokens en el fragmento (`#`) del enlace:** el navegador no envía el fragmento al servidor, así que los tokens no quedan en logs de servidores, proxies ni en la cabecera `Referer`. Angular lo lee y lo envía en el cuerpo de una petición POST.

**Email confirmado:** las cuentas se crean siempre desde un enlace enviado por email, así que poseer el token prueba la propiedad del correo y la cuenta nace confirmada. Un futuro cambio de email sí requerirá confirmación.

**Enlace copiable para WhatsApp** (sugerido en la Fase 1): **se posterga.** Si el entrenador puede copiar el enlace, también podría crear la cuenta del alumno en su nombre. Se reevalúa con confirmación de email posterior.

### 7.5. Invitaciones

| Regla | Valor |
|---|---|
| Token | 32 bytes aleatorios (generador criptográfico), codificados en base64url |
| Almacenamiento | Solo el hash SHA-256 del token; la búsqueda es por hash |
| Vencimiento | 7 días |
| Uso | Una vez; estados `Pending`, `Accepted`, `Revoked`, `Expired` |
| Destinatario | La cuenta que acepta debe tener **el mismo email** que la invitación |
| Reenvío | Genera un token nuevo e invalida el anterior; máximo 5 reenvíos |
| Límites | 30 invitaciones por hora por entrenador; 10 intentos de aceptación por minuto por IP |
| Plan | Invitar respeta el límite de alumnos activos del plan (Billing) |

### 7.6. Autorización

**Política por defecto:** `FallbackPolicy = RequireAuthenticatedUser`. Todo endpoint exige sesión salvo los marcados explícitamente como anónimos (login, recuperación, vista previa y aceptación de invitaciones, antiforgery, health checks). Un test falla si aparece un endpoint anónimo fuera de esa lista.

| Política | Requisito | Se aplica a |
|---|---|---|
| `Coach` | Rol `Coach` | Grupo `/api/coach` |
| `Athlete` | Rol `Athlete` | Grupo `/api/me` (perfil deportivo, sesiones, registros, salud) |
| `PlatformAdmin` | Rol `PlatformAdmin` **y** sesión iniciada con 2FA | Grupo `/api/admin` |
| `ActiveSubscription` | Suscripción del entrenador en `Trial` o `Active` | Escrituras de `/api/coach` (la lectura sigue permitida con la suscripción vencida) |

Cómo verificar que la sesión del administrador usó 2FA: Identity agrega el claim `amr` con valor `mfa` al completar el segundo factor; la política `PlatformAdmin` ya lo exige desde la Fase 3. Se confirma con un test de integración al implementar el login con 2FA (Hito 1); si no estuviera disponible, se agrega un claim propio al completar el login con 2FA.

**El rol no alcanza.** Cada caso de uso verifica además el recurso concreto:

1. **Alcance de datos por petición (`IDataScope`):** se resuelve desde la sesión. Entrenador → `CoachId`. Alumno → `AthleteId` y los ids de sus relaciones activas. Administrador → sin acceso a tablas de entrenamiento. Anónimo → nada.
2. **Filtro global nombrado de EF Core 10 (`"DataScope"`)** sobre cada entidad con `coach_id` o `coach_athlete_id`: `e.CoachId == scope.CoachId || scope.CoachAthleteIds.Contains(e.CoachAthleteId)`. Es la red de seguridad: aunque un caso de uso olvide filtrar, la consulta no devuelve datos ajenos. Desactivarlo exige `IgnoreQueryFilters(["DataScope"])`, prohibido fuera de una lista corta de casos revisados (validado por un analizador o test de búsqueda en el código).
3. **Verificación explícita en el caso de uso:** se cargan los recursos con su dueño en la consulta (`WHERE id = @id AND coach_id = @coach`). Si no hay fila → **404**, nunca 403, para no confirmar que el recurso existe.
4. **Restricciones en la base:** FK compuestas garantizan que grupo, relación y sesión pertenezcan al mismo entrenador aunque el código falle (sección 8).
5. **Row Level Security** de PostgreSQL: postergada; se evalúa antes de escalar comercialmente.

### 7.7. Matriz de permisos (resumen)

| Acción | Anónimo | Alumno (propio) | Alumno (otro) | Entrenador (propio) | Entrenador (otro) | Administrador |
|---|---|---|---|---|---|---|
| Ver plan semanal de un alumno | 401 | ✔ | 404 | ✔ | 404 | 403 |
| Crear o editar sesión planificada | 401 | 403 | 403 | ✔ | 404 | 403 |
| Registrar sesión realizada | 401 | ✔ | 404 | 403 | 403 | 403 |
| Comentar un registro | 401 | ✔ | 404 | ✔ | 404 | 403 |
| Ver datos de salud | 401 | ✔ | 404 | ✔ (relación activa, con consentimiento) | 404 | 403 |
| Editar datos de salud | 401 | ✔ (con consentimiento) | 404 | 403 | 403 | 403 |
| Gestionar grupos e invitaciones de alumnos | 401 | 403 | 403 | ✔ | 404 | 403 |
| Invitar entrenadores, cambiar suscripción, deshabilitar cuentas | 401 | 403 | 403 | 403 | 403 | ✔ (con 2FA) |

"Propio" significa que existe una relación activa entre ese alumno y ese entrenador. La matriz completa, endpoint por endpoint, se implementa como test (sección 11).

### 7.8. Panel de administración

| Capacidad | Incluida | Notas |
|---|---|---|
| Listar y buscar entrenadores (nombre, email, estado, alumnos activos, suscripción, última actividad) | Sí | Sin datos de entrenamiento |
| Invitar entrenadores, reenviar y revocar invitaciones | Sí | D8 |
| Cambiar plan, estado, fin de prueba y fin de período de una suscripción | Sí | Cobro manual durante el piloto; motivo obligatorio |
| Deshabilitar y rehabilitar una cuenta | Sí | Cierra sus sesiones (security stamp) |
| Procesar una baja de datos de un usuario | Sí | Confirmación escribiendo el email; anonimiza según la política de conservación |
| Consultar la auditoría | Sí | Filtros por fecha, actor y acción |
| Ver sesiones, registros, comentarios o datos de salud | **No** | ADR-009 |
| Suplantar usuarios | **No** | |
| Asignar el rol `PlatformAdmin` | **No** desde la UI | Solo comando de línea |

**Creación del primer administrador:** comando `admin create --email <email>` incluido en la misma aplicación (`dotnet RunFitPlatform.Api.dll admin create ...`), que crea una invitación de administrador y envía el enlace; al aceptarla, la persona debe configurar 2FA antes de poder usar el panel. Cómo ejecutarlo en producción depende del proveedor (D6); se documenta en `docs/runbooks/`.

### 7.9. Datos de salud

- Endpoints `/api/me/health` y `/api/me/health/consent` (alumno) y lectura dentro del detalle del alumno para el entrenador.
- Sin consentimiento vigente, `PUT /api/me/health` devuelve 422 (`coaching.health_consent_required`).
- Revocar el consentimiento borra la fila de `athlete_health_info` en la misma transacción.
- El texto del consentimiento tiene versión (`HealthConsentVersion` en configuración); si cambia, se pide aceptar la nueva versión antes de editar.
- Auditoría: otorgamiento, revocación y modificación (quién y cuándo, sin valores).

### 7.10. Otras medidas

| Medida | Configuración |
|---|---|
| Contraseñas | Mínimo 10 caracteres, máximo 128, sin reglas de composición; bloqueo de 15 minutos tras 5 intentos fallidos |
| Rate limiting (integrado) | Login: 10 por minuto por IP. Recuperación: 5 cada 15 minutos por IP. Aceptación de invitaciones: 10 por minuto por IP. Administración: 60 por minuto por usuario |
| IP real del cliente | `ForwardedHeaders` limitado a las redes del proxy del proveedor (D6) |
| Cabeceras | HSTS; `Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'`; `X-Content-Type-Options: nosniff`; `Referrer-Policy: no-referrer` |
| Data Protection | Claves persistidas en la base (`PersistKeysToDbContext`), para que un redeploy no invalide sesiones ni tokens. Acceso restringido a la base como única protección en el MVP [A verificar si conviene además cifrar las claves según el proveedor] |
| Tamaño de petición | Máximo 256 KB en `/api` |
| JSON | Propiedades desconocidas rechazadas (`UnmappedMemberHandling.Disallow`) |

`'unsafe-inline'` en `style-src` se necesita por los estilos que Angular inserta en línea; se evalúa reemplazarlo por nonce (soporte `ngCspNonce` de Angular) antes de escalar.
