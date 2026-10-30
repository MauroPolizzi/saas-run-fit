# Registro de decisiones de arquitectura

> Plataforma SaaS para entrenadores de running y fuerza. Fase 1, aprobada el 29/10/2026 con cambios (D9, D10, D11; D6 en evaluación). Datos consultados el 26/10/2026.

## Registro de decisiones de arquitectura (ADR)

Propuestas el 26/10/2026. Resueltas por Mauro el 29/10/2026 salvo ADR-006, que sigue en evaluación.

### ADR-001. Frontend con Angular 22 — Aceptada

- **Decisión:** Angular 22 con componentes standalone, signals, Angular Material y CDK. Web responsive, sin SSR ni PWA en el MVP.
- **Alternativas:** React con Vite; Angular 21 LTS.
- **Ventajas:** experiencia previa, framework completo y opinado, formularios oficiales robustos, actualizaciones asistidas.
- **Desventajas:** ecosistema de componentes de calendario más chico que React; versión mayor anual que exige mantenimiento.
- **Justificación:** el cuello de botella es el tiempo de un desarrollador; React no aporta una ventaja específica para este producto.
- **Consecuencias:** hay que construir la vista semanal propia; Signal Forms se prototipa al inicio.
- **Cambiaría si:** se decide una app nativa con React Native y se quiere compartir lógica, o si se suma un equipo con experiencia en React.

### ADR-002. Backend ASP.NET Core 10 con Minimal APIs por módulo — Aceptada

- **Decisión:** .NET 10 LTS, Minimal APIs agrupadas por módulo, una clase por caso de uso, sin MediatR ni CQRS formal.
- **Alternativas:** controladores; .NET 11 (STS); Clean Architecture con MediatR.
- **Ventajas:** menos ceremonia, organización natural por funcionalidad, OpenAPI y validación integrados, soporte hasta noviembre de 2028.
- **Desventajas:** curva corta de aprendizaje; menos familiar que los controladores.
- **Justificación:** encaja con el monolito modular sin agregar dependencias.
- **Consecuencias:** convenciones propias para organizar endpoints (diseño técnico, Fase 2).
- **Cambiaría si:** la validación integrada o los filtros de endpoint resultan insuficientes; es reversible endpoint por endpoint.

### ADR-003. PostgreSQL 18 con EF Core 10 (Npgsql) — Aceptada

- **Decisión:** PostgreSQL 18 administrado, EF Core 10 code-first, migraciones aplicadas como paso del pipeline.
- **Alternativas:** SQL Server o Azure SQL; SQLite para el MVP.
- **Ventajas:** más proveedores y planes baratos con backups, portabilidad, open source, restricciones avanzadas (índices parciales, FK compuestas).
- **Desventajas:** menor experiencia de Mauro; cambian herramientas y algunos tipos.
- **Justificación:** el costo y la portabilidad pesan más que la familiaridad cuando EF Core absorbe la mayoría de las diferencias. Con el modelo normalizado (ADR-007) `jsonb` deja de ser un motivo central, pero los demás siguen vigentes.
- **Consecuencias:** desarrollo local con Docker; convención *snake_case*.
- **Cambiaría si:** aparecen créditos de Azure significativos o un requisito comercial de SQL Server.

### ADR-004. Monolito modular pragmático en un solo proyecto — Aceptada

- **Decisión:** un proyecto `Api` con módulos por carpeta, un `AppDbContext`, esquemas de PostgreSQL por módulo, fachadas (`Contracts`) entre módulos y tests de arquitectura.
- **Alternativas:** capas clásicas; un proyecto por módulo; Clean Architecture completa.
- **Ventajas:** casi tan simple como capas, con límites de negocio claros.
- **Desventajas:** las fronteras dependen de disciplina y tests, no del compilador.
- **Justificación:** mejor puntuación ponderada (sección 7.3).
- **Consecuencias:** tests de arquitectura desde el primer día.
- **Cambiaría si:** los tests de arquitectura empiezan a necesitar excepciones frecuentes.

### ADR-005. Autenticación con ASP.NET Core Identity y cookies, mismo origen — Aceptada

- **Decisión:** Identity en la propia aplicación, cookie HttpOnly + Secure + SameSite, protección antiforgery, Angular servido desde el mismo origen.
- **Alternativas:** JWT en `localStorage`; proveedor administrado (Microsoft Entra External ID, Auth0, Clerk); Keycloak autogestionado.
- **Ventajas:** sin costo por usuario, sin tokens accesibles desde JavaScript, revocación de sesión inmediata, sin dependencia externa.
- **Desventajas:** se opera el envío de emails y la recuperación de contraseñas.
- **Justificación:** Microsoft recomienda cookies para aplicaciones de navegador; los tokens de Identity no son JWT estándar.
- **Consecuencias:** API y SPA comparten origen; una app nativa futura necesitará tokens.
- **Cambiaría si:** se necesita login social como requisito comercial, SSO o apps nativas.

### ADR-006. Un contenedor y una base administrada en el mismo proveedor y región — Propuesta (en evaluación)

- **Decisión propuesta:** imagen Docker única (API + Angular) y PostgreSQL administrado en la misma región. Candidatos: Render (Virginia); Fly.io + Neon (São Paulo); Azure.
- **Estado:** Mauro está evaluando. El diseño técnico no depende del proveedor; solo el job de despliegue y la configuración de proxy inverso (IP del cliente, HTTPS) se completan cuando se decida.
- **Cuándo hace falta:** antes de cerrar el Hito 0 (primer despliegue a producción).

### ADR-007. Contenido de la sesión totalmente normalizado con versiones — Aceptada (reemplaza la propuesta JSON)

- **Decisión:** pasos de running, ejercicios de fuerza y series en tablas relacionales. Las plantillas y las sesiones planificadas apuntan a un `workout` (raíz del contenido). Asignar copia el `workout` completo. Una sesión planificada tiene **versiones**: cuando un registro realizado referencia una versión, esa versión queda bloqueada y cualquier edición posterior crea una nueva.
- **Alternativas:** JSON tipado con instantáneas (propuesta original); modelo genérico entidad-atributo-valor.
- **Ventajas:** integridad referencial completa (cada serie apunta a un ejercicio real), consultas directas de progreso por ejercicio y por paso, restricciones en la base, historial inmutable sin duplicar JSON.
- **Desventajas:** más tablas (6 en vez de 2) y más código de mapeo; copiar un `workout` es una copia profunda de varias filas; el árbol de pasos con repeticiones necesita un `parent_step_id`.
- **Justificación:** elegida por Mauro. Encaja con el objetivo de mostrar progreso por ejercicio y con su experiencia en modelado relacional.
- **Consecuencias:** límite de anidamiento (una repetición no contiene otra repetición) validado en la aplicación; el contrato de la API sigue siendo un árbol anidado, independiente del almacenamiento.
- **Cambiaría si:** aparecen tipos de entrenamiento muy variables (por ejemplo, ciclismo o natación con estructuras distintas) que vuelvan rígidas las tablas.

### ADR-008. Relación entrenador-alumno como frontera de datos — Aceptada

- **Decisión:** toda planificación y registro cuelga de una relación `CoachAthlete`. El modelo admite varios entrenadores por alumno; el MVP limita a uno activo.
- **Alternativas:** alumno como propiedad de un entrenador (1:N rígido); alumno global sin relación explícita.
- **Ventajas:** aislamiento claro, fin de relación sin perder historial, permite crecer a varios entrenadores.
- **Desventajas:** una tabla y una verificación más en cada consulta.
- **Consecuencias:** la autorización se basa en la relación, no solo en el rol; FK compuestas garantizan en la base que grupo, alumno y sesión pertenezcan al mismo entrenador.

### ADR-009. Panel mínimo de administración — Aceptada

- **Decisión:** módulo `Admin` con un área web propia para un alcance cerrado: invitar entrenadores, gestionar el estado de su suscripción, deshabilitar y rehabilitar cuentas, gestionar invitaciones, procesar bajas de datos y consultar la auditoría.
- **Alternativas:** runbook de scripts SQL (propuesta original); panel completo con acceso a todos los datos; herramienta de terceros sobre la base.
- **Ventajas:** operación sin acceso directo a la base, menos errores manuales, acciones auditadas, soporte delegable en el futuro.
- **Desventajas:** es la superficie más sensible de la aplicación; hay que construirla y probarla.
- **Mitigaciones:** rol `PlatformAdmin` asignable solo por línea de comandos; 2FA obligatoria; sesión corta; todas las acciones auditadas; **sin acceso a sesiones, registros ni datos de salud**; sin suplantación de usuarios; tests de autorización propios.
- **Cambiaría si:** hace falta soporte con visibilidad de datos de entrenamiento. Requeriría consentimiento y revisión legal.

### ADR-010. Datos de salud con consentimiento explícito — Aceptada (sujeta a revisión legal)

- **Decisión:** los datos de salud del alumno (FC máxima y en reposo, peso, lesiones, observaciones médicas) se guardan en una tabla separada, solo con consentimiento explícito, versionado y revocable. Revocar borra los datos.
- **Alternativas:** no pedir datos de salud en el MVP (propuesta original); incluirlos en el perfil general sin consentimiento.
- **Ventajas:** el entrenador tiene información útil para planificar; el tratamiento sensible queda aislado y trazable.
- **Desventajas:** más obligaciones legales y de seguridad; flujo de consentimiento adicional.
- **Consecuencias:** endpoints propios, exclusión total de logs y del panel de administración, acceso del entrenador solo con relación activa, auditoría de consentimientos y cambios. **Requisito antes del piloto:** validar con un abogado el texto del consentimiento, la base legal y las obligaciones de registro.
- **Cambiaría si:** la revisión legal indica requisitos desproporcionados para el MVP; en ese caso se desactiva el módulo de salud sin afectar el resto.
