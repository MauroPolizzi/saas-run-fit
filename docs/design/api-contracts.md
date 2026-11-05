# Contratos de la API

> Plataforma SaaS para entrenadores de running y fuerza. Fase 2, diseño técnico pendiente de aprobación. Versiones verificadas el 29/10/2026.

## 10. Contratos iniciales de la API

Contrato inicial para los hitos 0 a 4. El documento OpenAPI generado en build (`openapi/v1.json`) es la fuente de verdad una vez implementado; esta tabla es el diseño de partida.

### 10.1. Convenciones transversales

- **Listas acotadas por fechas** (calendario, historial): `from` y `to` obligatorios, rango máximo de 62 días.
- **Listas paginadas** (administración, biblioteca): `page` (desde 1) y `pageSize` (máximo 100); respuesta `{ items, page, pageSize, totalCount }`.
- **Concurrencia:** los recursos editables devuelven `version` (valor de `xmin`); el `PUT` debe enviarla y recibe 409 si cambió.
- **Creación:** 201 con `Location` y el recurso creado. **Acciones** (revocar, archivar, terminar): `POST /recurso/{id}/accion`, 204.
- **Sin `DELETE` físico** para datos de negocio, salvo consentimiento de salud y comentarios propios.

### 10.2. Accounts (`/api/auth`, `/api/invitations`)

| Método y ruta | Anónimo | Cuerpo / parámetros | Respuesta |
|---|---|---|---|
| `GET /api/auth/antiforgery` | Sí | | 204 + cookie `XSRF-TOKEN` |
| `POST /api/auth/login` | Sí | `{ email, password, rememberMe }` | 204; 200 `{ requiresTwoFactor: true }` si falta el segundo factor; 401 genérico |
| `POST /api/auth/login/two-factor` | Sí (con cookie intermedia de Identity) | `{ code, rememberMachine }` o `{ recoveryCode }` | 204 / 401 |
| `POST /api/auth/logout` | No | | 204 |
| `GET /api/auth/me` | No | | `{ userId, email, displayName, roles[], twoFactorEnabled, activeCoach?: { coachAthleteId, coachId, coachName } }` |
| `POST /api/auth/forgot-password` | Sí | `{ email }` | 202 siempre |
| `POST /api/auth/reset-password` | Sí | `{ userId, token, newPassword }` | 204 / 400 |
| `POST /api/auth/change-password` | No | `{ currentPassword, newPassword }` | 204 |
| `GET /api/auth/two-factor/setup` | No | | `{ sharedKey, authenticatorUri }` |
| `POST /api/auth/two-factor/enable` | No | `{ code }` | `{ recoveryCodes[] }` |
| `POST /api/invitations/preview` | Sí | `{ token }` | `{ kind ("athlete", "coach" o "platformAdmin"), inviterName?, emailMasked, expiresAt, accountExists }` / 404 |
| `POST /api/invitations/accept-new` | Sí | `{ token, displayName, password, acceptTerms: true }` | 201 + sesión iniciada |
| `POST /api/invitations/accept` | No (email de la sesión debe coincidir) | `{ token }` | 204 |

### 10.3. Coaching

**Entrenador (`/api/coach`, política `Coach`)**

| Método y ruta | Cuerpo / parámetros | Respuesta |
|---|---|---|
| `GET /api/coach/profile` · `PUT` | `{ displayName, bio, city, version }` | Perfil |
| `GET /api/coach/athletes` | `status` (`active` o `ended`), `groupId?`, `search?` | `[{ coachAthleteId, athleteName, email, status, groups[], startedAt, lastLoggedOn? }]` |
| `GET /api/coach/athletes/{coachAthleteId}` | | Perfil deportivo, marcas, notas, grupos y `health?` (solo con relación activa y consentimiento) |
| `PUT /api/coach/athletes/{coachAthleteId}/notes` | `{ notes, version }` | 204 |
| `POST /api/coach/athletes/{coachAthleteId}/end` | `{ reason? }` | 204 |
| `GET /api/coach/invitations` · `POST` | `{ email, groupId? }` | 201 |
| `POST /api/coach/invitations/{id}/resend` · `/revoke` | | 204 |
| `GET /api/coach/groups` · `POST` | `{ name, description }` | 201 |
| `GET /api/coach/groups/{id}` · `PUT` · `POST .../archive` | `{ name, description, version }` | |
| `PUT /api/coach/groups/{id}/members` | `{ coachAthleteIds[] }` (reemplaza la lista) | 204 |

**Alumno (`/api/me`, política `Athlete`)**

| Método y ruta | Cuerpo | Respuesta |
|---|---|---|
| `GET /api/me/athlete-profile` · `PUT` | `{ birthDate?, sex?, heightCm?, level, mainGoal, weeklyDaysAvailable?, personalBests: [{ distanceM, timeS, achievedOn? }], version }` | Perfil |
| `GET /api/me/health/consent` | | `{ currentVersion, granted: bool, grantedAt?, textVersionToAccept }` |
| `POST /api/me/health/consent` | `{ consentVersion }` | 204 |
| `DELETE /api/me/health/consent` | | 204 (borra los datos de salud) |
| `GET /api/me/health` · `PUT` | `{ maxHr?, restingHr?, weightKg?, injuries?, medicalNotes? }` | Datos / 422 sin consentimiento |

### 10.4. Planning (`/api/coach`)

| Método y ruta | Cuerpo / parámetros | Respuesta |
|---|---|---|
| `GET /api/coach/exercises` · `POST` | `search?`, `category?`, `page`; `{ name, category, equipment?, description?, videoUrl? }` | Catálogo global + propios |
| `PUT /api/coach/exercises/{id}` · `POST .../archive` | Solo ejercicios propios | |
| `GET /api/coach/templates` · `POST` | `kind?`, `search?`; `{ title, workout }` | 201 |
| `GET /api/coach/templates/{id}` · `PUT` · `POST .../archive` · `POST .../duplicate` | `{ title, workout, version }` | |
| `POST /api/coach/assignments` | `{ templateId, scheduledOn, positionInDay?, coachAthleteIds[] }` o `{ templateId, scheduledOn, groupId, excludeCoachAthleteIds[] }` | 201 `{ batchId, plannedSessionIds[] }` |
| `GET /api/coach/planned-sessions` | `from`, `to`, `groupId?`, `coachAthleteId?` | Matriz semanal: `[{ id, coachAthleteId, scheduledOn, positionInDay, kind, title, status, totals, hasLog, planChangedAfterLog }]` |
| `POST /api/coach/planned-sessions` | `{ coachAthleteId, scheduledOn, positionInDay?, title, workout }` | 201 (sesión individual sin plantilla) |
| `GET /api/coach/planned-sessions/{id}` | | Detalle con `workout`, `versionNumber`, `log?` |
| `PUT /api/coach/planned-sessions/{id}` | `{ title, scheduledOn, positionInDay, workout, version }` | 200; crea versión nueva si la actual está registrada |
| `POST /api/coach/planned-sessions/{id}/cancel` | | 204 |

**Contrato de `workout`** (árbol anidado; el almacenamiento normalizado es un detalle interno):

```json
{
  "kind": "running",
  "estimatedDurationS": 3600,
  "targetRpe": 7,
  "notes": "Series en pista",
  "steps": [
    { "type": "warmUp", "duration": { "type": "time", "seconds": 900 }, "target": { "type": "rpe", "rpe": 3 } },
    { "type": "repeat", "times": 6, "steps": [
      { "type": "work", "duration": { "type": "distance", "meters": 1000 },
        "target": { "type": "pace", "minSecondsPerKm": 250, "maxSecondsPerKm": 260 } },
      { "type": "recovery", "duration": { "type": "time", "seconds": 120 }, "target": { "type": "none" } }
    ]},
    { "type": "coolDown", "duration": { "type": "time", "seconds": 600 }, "target": { "type": "none" } }
  ]
}
```

```json
{
  "kind": "strength",
  "exercises": [
    { "exerciseId": "0192f6c1-…", "groupLabel": "A1", "notes": "",
      "sets": [ { "reps": 5, "load": { "type": "kg", "value": 80 }, "restS": 120, "tempo": "3010" } ] }
  ]
}
```

Las respuestas agregan `id` a cada paso, ejercicio y serie, y `totals: { distanceM, durationS, sets }`.

### 10.5. TrainingLog

| Método y ruta | Cuerpo / parámetros | Respuesta |
|---|---|---|
| `GET /api/me/sessions` | `from`, `to` | Semana del alumno: sesiones con estado y resumen |
| `GET /api/me/sessions/{plannedSessionId}` | | Detalle con `workout` vigente y `log?` |
| `POST /api/me/sessions/{plannedSessionId}/log` | `{ performedOn, completion, actualDistanceM?, actualDurationS?, avgHr?, rpe?, notes?, runningSegments?: [{ plannedStepId?, repetitionIndex?, distanceM?, durationS?, avgHr? }], strengthSets?: [{ exerciseId, plannedSetId?, reps?, durationS?, loadKg?, rpe?, completed }] }` | 201; bloquea la versión |
| `POST /api/me/sessions/{plannedSessionId}/skip` | `{ reason? }` | 204 |
| `POST /api/me/logs` | Igual que el anterior más `kind` (entrenamiento no planificado) | 201 |
| `GET /api/me/logs` · `GET /api/me/logs/{id}` · `PUT /api/me/logs/{id}` | `from`, `to` | Historial / corrección propia |
| `GET /api/coach/athletes/{coachAthleteId}/logs` · `GET /api/coach/session-logs/{id}` | `from`, `to` | Vista del entrenador |
| `GET /api/session-logs/{id}/feedback` · `POST` | `{ body }` | Comentarios; entrenador de la relación o alumno dueño |

### 10.6. Progress

| Método y ruta | Respuesta |
|---|---|
| `GET /api/coach/athletes/{coachAthleteId}/progress/weekly?from&to` · `GET /api/me/progress/weekly?from&to` | `[{ weekStart, plannedDistanceM, actualDistanceM, plannedDurationS, actualDurationS, sessionsPlanned, sessionsCompleted, sessionsSkipped, strengthTonnageKg, avgRpe }]` |
| `GET /api/coach/athletes/{coachAthleteId}/progress/exercises/{exerciseId}?from&to` · `GET /api/me/progress/exercises/{exerciseId}?from&to` | `[{ performedOn, topSetLoadKg, totalReps, volumeKg }]` (posible gracias al modelo normalizado) |
| `GET /api/coach/compliance?from&to&groupId?` | `[{ coachAthleteId, athleteName, planned, completed, skipped }]` |

### 10.7. Billing y Admin

| Método y ruta | Política | Cuerpo / respuesta |
|---|---|---|
| `GET /api/coach/subscription` | `Coach` | `{ planCode, status, trialEndsAt?, currentPeriodEnd?, limits: { maxActiveAthletes }, usage: { activeAthletes } }` |
| `GET /api/admin/coaches` · `GET /api/admin/coaches/{coachId}` | `PlatformAdmin` | Paginado; datos de cuenta, alumnos activos, suscripción, última actividad |
| `GET /api/admin/coach-invitations` · `POST` · `POST .../{id}/resend` · `/revoke` | `PlatformAdmin` | `{ email, trialDays }` |
| `PUT /api/admin/coaches/{coachId}/subscription` | `PlatformAdmin` | `{ planCode, status, trialEndsAt?, currentPeriodEnd?, reason, version }` |
| `POST /api/admin/users/{userId}/disable` · `/enable` | `PlatformAdmin` | `{ reason }` |
| `POST /api/admin/users/{userId}/erase` | `PlatformAdmin` | `{ confirmEmail, reason }` → 202 |
| `GET /api/admin/audit-events` | `PlatformAdmin` | `from`, `to`, `actorId?`, `action?`, paginado |

### 10.8. Criterios de validación

**Forma (validación integrada de Minimal APIs en .NET 10 [Verificado]: `AddValidation()` + `[ValidatableType]` + DataAnnotations e `IValidatableObject`):**

| Campo | Regla |
|---|---|
| Email | Obligatorio, ≤ 254, formato válido, se normaliza a minúsculas |
| Contraseña | 10 a 128 caracteres |
| `displayName` | 2 a 80 |
| Nombre de grupo | 1 a 60; descripción ≤ 500 |
| Título de plantilla o sesión | 1 a 100; notas ≤ 2.000 |
| `scheduledOn` | Entre hoy − 365 y hoy + 365 días (zona del entrenador) |
| Pasos de running | 1 a 60 en total; `repeat.times` 1 a 50; profundidad máxima 1 |
| Distancia de un paso | 1 a 100.000 m; tiempo 1 a 36.000 s |
| Ritmo objetivo | 120 a 900 s/km (2:00 a 15:00 min/km); mínimo ≤ máximo |
| Zona de FC | 1 a 5; RPE 1 a 10 |
| Fuerza | 1 a 30 ejercicios; 1 a 20 series por ejercicio; reps 1 a 100; tiempo 1 a 3.600 s; carga 0 a 500 kg o 1 a 120 % de 1RM; descanso 0 a 900 s; tempo `^[0-9X]{4}$` |
| Registro | `performedOn` ≤ hoy + 1; distancia 0 a 300.000 m; duración 0 a 86.400 s; FC media 30 a 230; RPE 1 a 10; notas ≤ 2.000 |
| Marcas personales | Distancia 100 a 100.000 m; tiempo 60 a 86.400 s |
| Salud | FC máxima 100 a 230; FC en reposo 25 a 120; peso 25 a 250 kg; textos ≤ 2.000 |
| Comentario | 1 a 2.000 |
| Listas por fechas | `from ≤ to`, rango ≤ 62 días |

**Reglas de negocio (en el caso de uso, 422 o 404):** la relación debe estar activa; el ejercicio debe ser global o del entrenador; el grupo y los alumnos deben ser del mismo entrenador; un registro por sesión planificada; no registrar una sesión cancelada; respetar los límites del plan; consentimiento vigente para datos de salud.

**JSON:** propiedades desconocidas rechazadas; enums desconocidos rechazados; cuerpo máximo 256 KB.
