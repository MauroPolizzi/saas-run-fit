# Roadmap de implementación

> Plataforma SaaS para entrenadores de running y fuerza. Fase 1, aprobada el 29/10/2026 con cambios (D9, D10, D11; D6 en evaluación). Datos consultados el 26/10/2026.

## 15. Roadmap de implementación

Los tamaños son relativos (S, M, L). No se dan semanas porque falta saber la dedicación de Mauro. Como referencia [Estimación], con 10 a 15 horas semanales, cada tamaño M equivale aproximadamente a 2 o 3 semanas.

### 15.1. Fase 2: diseño técnico (tras aprobar esta fase)

1. Registrar las decisiones aprobadas y resolver las pendientes (D1 a D13).
2. Estructura de directorios, límites de módulos, dependencias permitidas y convenciones de nombres.
3. Configuración por entorno y gestión de secretos.
4. Especificación de autenticación y autorización: endpoints de cuenta, invitaciones, políticas y matriz de permisos.
5. Contratos iniciales de la API (OpenAPI) para los hitos 1 y 2.
6. Criterios de validación por recurso.
7. Modelo de datos inicial (tablas, índices, restricciones únicas, filtros globales) y esquema JSON del contenido de sesiones v1.
8. Estrategia de migraciones y datos semilla.
9. Estrategia de pruebas detallada, con la matriz de autorización.
10. Pipeline CI/CD inicial.
11. Requisitos de logging, health checks y manejo de errores.
12. **Validación del modelo de sesiones con 2 o 3 entrenadores reales** (maqueta o papel).

### 15.2. Construcción

| Hito | Contenido | Tamaño | Resultado verificable |
|---|---|---|---|
| **0. Base técnica** | Solución .NET, Angular, Docker, PostgreSQL local, CI, deploy a producción de un "hola mundo" con `/health`, Identity con login, logout y reseteo de contraseña, envío de email | M | Login real en la URL de producción |
| **1. Entrenadores, alumnos y administración** | Panel de administración mínimo (2FA, invitar entrenadores, suscripción manual, auditoría), perfil del entrenador, invitaciones de alumnos, relación entrenador-alumno, grupos, perfil deportivo, datos de salud con consentimiento, matriz de autorización | L | El administrador invita a un entrenador, que invita a 3 alumnos y los agrupa |
| **2. Planificación** | Catálogo de ejercicios, plantillas de running y fuerza (modelo normalizado), editor, asignación individual y a grupo con copia profunda, ajuste individual, versiones, vista semanal del entrenador | L | Semana planificada para un grupo con ajustes por alumno |
| **3. Alumno y registro** | Calendario semanal del alumno (móvil), registro de lo realizado (running y fuerza), comentarios del entrenador | M | El alumno registra y el entrenador comenta |
| **4. Seguimiento** | Plan vs realizado, historial individual, gráficos de volumen, cumplimiento y tonelaje | M | El entrenador revisa la evolución de un alumno en 8 semanas |
| **5. Preparación del piloto** | Checklist de seguridad (10.5), restauración de backup probada, términos y privacidad, plan y derechos (trial), monitoreo básico | S | Listo para usuarios externos |
| **Piloto** | 2 a 5 entrenadores reales durante 4 a 8 semanas | | Feedback y métricas de uso |

### 15.3. Después del piloto (reevaluar con evidencia)

- Suscripciones con cobro (Mercado Pago u otro): cuando haya entrenadores dispuestos a pagar.
- PWA instalable: si los alumnos lo piden.
- Importación desde Strava o Garmin: es la integración más probable de aportar valor (registro automático de running). Reevaluar cuando el registro manual sea la queja principal.
- `TrainingBlock` y periodización: si los entrenadores planifican por bloques.
- 2FA, Row Level Security y auditoría: antes de escalar comercialmente.
- App nativa: solo con evidencia de que la web no alcanza.
