# Estrategia de pruebas

> Plataforma SaaS para entrenadores de running y fuerza. Fase 2, diseño técnico aprobado (Fase 3 iniciada el 05/11/2026). Versiones verificadas el 29/10/2026.

## 11. Estrategia de pruebas

Concreta la estrategia de la Fase 1 (sección 12 de `architecture-evaluation.md`) para este diseño.

### 11.1. Proyectos y herramientas

| Proyecto | Herramientas | Qué contiene |
|---|---|---|
| `RunFitPlatform.Api.UnitTests` | xUnit v3 sobre Microsoft.Testing.Platform, ArchUnitNET (elegida en la Fase 3: NetArchTest no publica versiones desde 2021) | Reglas de dominio y tests de arquitectura |
| `RunFitPlatform.Api.IntegrationTests` | xUnit v3, `WebApplicationFactory`, Testcontainers.PostgreSql con `postgres:18.6` | API real contra PostgreSQL real; matriz de autorización |
| `web` (unitarios) | Vitest, runner por defecto de `ng new` y del builder `@angular/build:unit-test` en Angular 22 [Verificado en la Fase 3] | Editor de workout, conversiones de ritmo, formateo |
| `web/e2e` | Playwright | Flujos críticos de punta a punta |

### 11.2. Unitarias (dominio)

- Cálculo de totales de un workout (incluye repeticiones anidadas: 6 × (1000 m + 2 min) = 6.000 m de trabajo + trote).
- Validación del árbol de pasos: profundidad máxima, coherencia duración y objetivo, límites.
- Copia profunda: ids nuevos, misma estructura, `parent_step_id` remapeado, nombres de ejercicio copiados.
- Versionado: editar versión libre la modifica; editar versión bloqueada crea `n + 1`.
- Derechos del plan: límite de alumnos activos, solo lectura con suscripción vencida.
- Estados de invitación: vencida, revocada, usada, reenvío.

### 11.3. Integración

- **Base por ejecución:** un contenedor PostgreSQL por corrida de tests, migraciones aplicadas desde cero; limpieza de datos entre tests (con una librería de limpieza o un `TRUNCATE` generado; se decide en el Hito 1, cuando existan tablas de negocio).
- **Usuarios reales:** cada test crea entrenadores y alumnos a través de los endpoints de invitación y hace login con cookies reales. Así se prueban de paso la cookie, el antiforgery y las políticas.
- **Emails:** el `IEmailSender` en memoria permite leer el enlace de invitación o reseteo desde el test.
- **Restricciones de la base:** tests que intentan saltear el dominio (insertar una membresía con otro entrenador) y verifican que la FK compuesta lo rechaza.

### 11.4. Matriz de autorización

1. Un test enumera **todos los endpoints** registrados (`EndpointDataSource`) y falla si alguno no tiene un caso en la matriz o es anónimo sin estar en la lista permitida.
2. Para cada endpoint con id de recurso: se ejecuta con los actores **anónimo, alumno propio, alumno ajeno, entrenador propio, entrenador ajeno, administrador** y se compara con el resultado esperado (sección 7.7).
3. Casos específicos: datos de salud sin consentimiento, con relación terminada, desde el panel de administración; endpoints de administración sin 2FA.

### 11.5. Escenarios críticos automatizados

Los 11 escenarios de la sección 12.2 de la evaluación de arquitectura, más:

- Asignar una plantilla a un grupo crea una sesión independiente por alumno; editar una no cambia las otras.
- Registrar una sesión bloquea su versión; editar la sesión después crea la versión 2 y el registro sigue apuntando a la 1.
- Un alumno con relación terminada ya no ve su plan futuro, pero conserva su historial.
- Una invitación aceptada con un email distinto falla.
- Cambiar la contraseña cierra las demás sesiones (dentro del intervalo de revalidación).

### 11.6. Contrato

El build genera `openapi/v1.json`. CI falla si el archivo generado difiere del versionado: cualquier cambio de contrato queda visible en el diff del PR. Los tipos TypeScript del frontend se generan desde ese archivo (herramienta solo de tipos; `openapi-typescript` 7.13.0 exige TypeScript 5 y Angular 22 usa TypeScript 6, así que se define al implementar el primer contrato con cuerpo).

### 11.7. E2E (Playwright)

1. Administrador invita a un entrenador; el entrenador acepta y entra.
2. Entrenador invita a un alumno a un grupo; el alumno acepta.
3. Entrenador crea una plantilla de intervalos y la asigna al grupo con un ajuste individual.
4. Alumno ve su semana en viewport móvil, registra la sesión y el entrenador comenta.
5. Entrenador consulta plan vs realizado y el progreso de un ejercicio.

Se ejecutan en CI al hacer merge a `master` (no en cada PR, para mantener los PR rápidos) y antes de cada despliegue.

### 11.8. Calidad estática

- `Directory.Build.props`: `Nullable` habilitado, analizadores de .NET en nivel recomendado, `TreatWarningsAsErrors` en CI.
- `dotnet format --verify-no-changes` en CI.
- Angular: ESLint (angular-eslint), Prettier, TypeScript `strict`, reglas de imports entre features.
- Sin meta de cobertura; el informe de cobertura se publica como dato, no como compuerta.
