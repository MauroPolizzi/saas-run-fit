# Seguridad y privacidad

> Plataforma SaaS para entrenadores de running y fuerza. Fase 1, aprobada el 29/10/2026 con cambios (D9, D10, D11; D6 en evaluación). Datos consultados el 26/10/2026.

## 10. Seguridad y privacidad

### 10.1. Autenticación: alternativas

| Opción | Costo | Ventajas | Desventajas |
|---|---|---|---|
| **ASP.NET Core Identity en la app, con cookies** | Sin costo por usuario | Hash de contraseñas, bloqueo por intentos, confirmación de email, 2FA TOTP y reseteo de contraseña incluidos; sesión revocable; sin dependencia externa | Hay que operar emails y diseñar las pantallas; login social requiere configuración |
| Identity con tokens (`MapIdentityApi` en modo bearer) | Sin costo | Sirve para clientes no navegador | Microsoft aclara que **no son JWT estándar** y que no pretende ser un servidor de tokens completo; el token queda accesible para JavaScript [Verificado] |
| Proveedor administrado (Microsoft Entra External ID, Auth0, Clerk) | Planes gratuitos con límites de usuarios activos [A verificar precios vigentes] | MFA, login social y pantallas listas; menos código de seguridad propio | Integración OIDC más compleja para un SPA, dependencia del proveedor, migrar usuarios después es caro |
| Keycloak autogestionado | Servidor adicional | Estándar y potente | Operar otro servicio crítico: desproporcionado para el MVP |

**Recomendación:** **Identity + cookie**. Microsoft recomienda cookies para aplicaciones de navegador porque el navegador las gestiona sin exponerlas a JavaScript [Verificado]. Como Angular se sirve desde el mismo origen que la API, no hace falta JWT ni BFF.

`MapIdentityApi` puede acelerar los endpoints de login y reseteo, pero su registro es genérico. Como el alta del alumno pasa por una invitación, conviene **endpoints propios** que usen `UserManager` y `SignInManager`, reaprovechando todo lo que Identity ya resuelve. Detalle a definir en Fase 2.

### 10.2. Medidas por tema

| Tema | Medida en el MVP |
|---|---|
| Contraseñas | Solo Identity (hash PBKDF2 integrado). Nunca almacenamiento ni criptografía propios. Longitud mínima 10 o más, sin reglas de composición absurdas; bloqueo temporal tras intentos fallidos |
| Recuperación de contraseña | Token de Identity de un solo uso y vencimiento corto, enviado por email; respuesta idéntica exista o no la cuenta (evita enumerar usuarios); se invalidan las sesiones al cambiarla (*security stamp*) |
| Cookies | `HttpOnly`, `Secure`, `SameSite=Lax` (o `Strict` si el flujo lo permite), expiración deslizante razonable |
| CSRF | Antiforgery de ASP.NET Core con token en cookie legible + cabecera `X-XSRF-TOKEN`, que `HttpClient` de Angular envía automáticamente; validado en toda petición que modifica datos |
| Roles | Dos roles de aplicación (`Coach`, `Athlete`) derivados de los perfiles. **El rol no alcanza**: cada acción valida además la relación concreta |
| Autorización en backend | Políticas por endpoint + **autorización por recurso**: "¿esta sesión pertenece a una relación activa de este entrenador?" o "¿este registro es del alumno autenticado?". La UI solo oculta botones |
| Aislamiento entre entrenadores | Todas las tablas del negocio llevan `coach_id`. **Filtro global de EF Core** por entrenador actual como red de seguridad, más verificación explícita en cada caso de uso. Para alumnos, filtro por sus relaciones. Row Level Security de PostgreSQL se evalúa después como segunda capa |
| Manipulación de identificadores (IDOR) | Cargar siempre con `WHERE id = @id AND coach_id = @actual`; **404** (no 403) si no corresponde, para no confirmar existencia; GUID v7 no enumerables como defensa adicional |
| Invitaciones | Token aleatorio de 32 bytes o más; en la base se guarda solo su hash; un solo uso; vencimiento de 7 días; ligado al email invitado; revocable; aceptar requiere iniciar sesión o crear cuenta con **ese** email; límite de invitaciones por hora |
| Validación de entradas | Validación de forma en el endpoint (longitudes, rangos: ritmo, distancia, RPE 1 a 10, cantidad de pasos y series) y de reglas en el caso de uso; contenido JSON validado contra el tipo C# antes de guardar; tamaño máximo de petición |
| XSS | Angular escapa por defecto; prohibido `innerHTML` con texto de usuario y `bypassSecurityTrust*`; cabecera Content-Security-Policy estricta |
| Cabeceras | HSTS, CSP, `X-Content-Type-Options`, `Referrer-Policy`, `frame-ancestors 'none'` |
| Rate limiting | Middleware integrado de ASP.NET Core sobre login, recuperación de contraseña e invitaciones |
| Secretos | Nunca en el repositorio. User Secrets en desarrollo; variables de entorno o secretos del proveedor en producción; secretos de GitHub Actions para el pipeline; escaneo de secretos en el repo |
| Data Protection | Claves de Data Protection persistidas (base de datos o almacenamiento del proveedor). Sin esto, cada redeploy invalida cookies y tokens de reseteo |
| Logging | Logs estructurados sin contraseñas, tokens, cookies ni datos de salud; emails enmascarados; `ProblemDetails` sin trazas en producción; id de correlación por petición |
| Backups | Los del proveedor (PITR de 3 a 7 días según el plan) **más** un `pg_dump` diario cifrado a un almacenamiento externo con retención de 30 días. **Restauración de prueba antes del piloto y luego trimestral** |
| Transporte | HTTPS obligatorio (TLS del proveedor); conexión a la base con TLS |
| Dependencias | Dependabot o Renovate, `dotnet list package --vulnerable` y `npm audit` en CI |

### 10.3. Datos personales en Argentina (no es asesoramiento legal)

Esto es un análisis técnico. Todo lo que sigue requiere **verificación con un abogado especializado** antes de abrir el servicio a usuarios externos.

- **Marco vigente:** la **Ley 25.326** de Protección de Datos Personales sigue vigente en 2026, con proyectos de reforma en discusión. La autoridad de control es la **Agencia de Acceso a la Información Pública (AAIP)** [Verificado en fuente periodística especializada; confirmar el estado de la reforma].
- **Datos sensibles:** la ley da protección reforzada a los datos de salud. FC, lesiones, peso u observaciones médicas en un perfil deportivo **podrían** considerarse datos de salud. [A verificar jurídicamente] Esto condiciona el consentimiento, las medidas de seguridad y qué campos conviene pedir.
- **Registro de bases de datos:** la ley prevé la inscripción de bases de datos ante la autoridad de control. [A verificar si aplica y cómo se tramita hoy]
- **Medidas de seguridad:** la Resolución AAIP 47/2018 establece medidas de seguridad recomendadas para el tratamiento de datos personales. [A verificar su vigencia y aplicabilidad; sirve como lista de control técnica]
- **Transferencia internacional:** alojar datos en EE. UU. (Render) o Brasil (Fly.io, Neon São Paulo) es una transferencia internacional. La ley la restringe a países con protección adecuada o con garantías contractuales. [A verificar qué país y qué cláusulas corresponden; puede influir en la elección de región]
- **Roles:** probablemente el entrenador sea responsable de los datos de sus alumnos y la plataforma, encargada del tratamiento, o ambos responsables según el caso. [A verificar] Esto define los términos y condiciones y el acuerdo con el entrenador.
- **Derechos de los titulares:** acceso, rectificación y supresión. El diseño debe permitir exportar los datos de un alumno y eliminarlos.

### 10.4. Panel de administración (D10)

- Rol `PlatformAdmin` asignable **solo** por un comando de línea ejecutado en el servidor; nunca desde la interfaz.
- **2FA (TOTP) obligatoria** para entrar al área de administración; sesión de 8 horas sin renovación deslizante.
- Alcance cerrado: entrenadores, invitaciones de entrenador, estado de suscripción, deshabilitar y rehabilitar cuentas, bajas de datos y auditoría.
- **Sin acceso** a sesiones, registros, comentarios ni datos de salud; sin suplantación de usuarios. Los tests de arquitectura impiden que el módulo Admin use Planning, TrainingLog o los datos de salud.
- Toda acción queda en `AuditEvent` (quién, qué, sobre qué, cuándo, motivo). Las acciones destructivas piden confirmación escribiendo el email afectado.
- Límite de peticiones propio y tests de autorización específicos.

### 10.5. Datos de salud con consentimiento (D11)

- Tabla separada (`athlete_health_info`), endpoints propios y consentimiento previo explícito, con versión del texto aceptado y fecha.
- Revocar el consentimiento borra los datos de salud inmediatamente (y de los backups cuando roten).
- Acceso: el alumno (lectura y escritura) y sus entrenadores con relación **activa** (solo lectura). Nunca el administrador.
- Nunca en logs, mensajes de error, exportaciones a terceros ni emails.
- Se auditan el otorgamiento y la revocación del consentimiento y cada modificación (sin guardar los valores en la auditoría).
- Cifrado en reposo: el del proveedor de base de datos [A verificar según el proveedor elegido en D6]. El cifrado a nivel de columna se posterga, pero el aislamiento en tabla propia lo deja preparado.
- **Antes del piloto:** revisión legal del texto de consentimiento, la base legal del tratamiento y la eventual inscripción de la base de datos.

### 10.6. Conservación y eliminación (propuesta a validar)

- Cuenta eliminada por el usuario: anonimización o borrado de datos personales en un plazo de 30 días; los registros agregados sin identificación pueden mantenerse.
- Relación terminada: el entrenador conserva acceso de solo lectura durante un período a definir (por ejemplo, 12 meses) y luego se anonimiza; el alumno conserva su historial.
- Suscripción vencida: solo lectura; si no se renueva en un plazo a definir, aviso y luego eliminación.
- Backups: la eliminación llega a los backups cuando estos rotan (documentarlo en la política de privacidad).
- Logs: 14 a 30 días.

### 10.7. Antes de usuarios externos y qué puede esperar

**Imprescindible antes del piloto:** HTTPS; Identity con confirmación de email y reseteo; cookies seguras + antiforgery; autorización por recurso con **tests automáticos de aislamiento**; filtro global por entrenador; invitaciones seguras; rate limiting en autenticación; secretos fuera del repo; claves de Data Protection persistidas; backups verificados con una restauración real; cabeceras de seguridad; términos y política de privacidad revisados por un abogado; consentimiento explícito para datos de salud (D11); 2FA obligatoria y auditoría en el panel de administración (D10).

**Puede esperar:** 2FA (ofrecerla opcional a entrenadores después del piloto); Row Level Security en PostgreSQL; auditoría detallada; login social; WAF; pentest externo (antes de escalar comercialmente).
