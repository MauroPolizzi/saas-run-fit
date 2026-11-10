# Infraestructura y costos

> Plataforma SaaS para entrenadores de running y fuerza. Fase 1, aprobada el 29/10/2026 con cambios (D9, D10, D11; D6 en evaluación). Datos consultados el 26/10/2026.

## 11. Infraestructura y costos

Precios consultados el **26/10/2026** en las páginas oficiales de cada proveedor, en **USD, sin impuestos**. Los proveedores cambian precios con frecuencia: se vuelven a verificar antes de contratar.

### 11.1. Forma del despliegue

- **Una imagen Docker** con la API y los archivos compilados de Angular. La imagen es portable a cualquier PaaS de contenedores, a Azure o a un VPS.
- **PostgreSQL administrado en la misma región** que la aplicación. Separar app y base entre continentes multiplica la latencia de cada consulta.
- **Email transaccional** externo (invitaciones y recuperación de contraseña).
- No hay CDN separado, colas, caché, almacenamiento de objetos ni Kubernetes en el MVP.

### 11.2. Opciones evaluadas

| Opción | App | Base | Región más cercana a Argentina | Datos verificados |
|---|---|---|---|---|
| **R. Render** | Web service USD 7/mes (512 MB) o USD 25/mes (1 CPU, 2 GB) | Postgres USD 6/mes (256 MB) o USD 19/mes (1 GB) + USD 0,30/GB | **Virginia, EE. UU.** (no hay región en Sudamérica) | Precios, regiones, PITR de 3 días en Hobby y 7 en Pro |
| **F. Fly.io + Neon** | shared-cpu-1x 512 MB: USD 3,69/mes en la región más barata (iad) | Neon Launch: USD 0,106 por CU-hora + USD 0,35/GB-mes, scale-to-zero | **São Paulo**: Neon `aws-sa-east-1` disponible; Fly `gru` [precio en esa región a verificar] | Precios base, región de Neon |
| Railway | Hobby USD 5/mes con USD 5 de uso incluido; USD 20 por vCPU-mes y USD 10 por GB-mes | Postgres sobre volumen (USD 0,15/GB) | [Regiones a verificar] | Precios |
| Azure | App Service B1 o Container Apps (concesión gratuita mensual de 180.000 vCPU-s, 360.000 GiB-s y 2 M de peticiones) | Azure Database for PostgreSQL o Azure SQL (la oferta gratuita de Azure SQL no sirve para producción) | Brazil South | Solo la concesión gratuita y los límites; **importes de pago no verificables hoy** |
| VPS autogestionado | El más barato en recursos | Postgres instalado por uno mismo | Varias | No se evaluó en detalle: suma parches, backups, TLS y monitoreo a cargo de un desarrollador solo. No recomendado para el MVP |

### 11.3. Escenarios ilustrativos

**Escenario 1. Desarrollo y pruebas**

| Servicio | Costo |
|---|---|
| PostgreSQL 18 en Docker local, .NET SDK, Node | USD 0 |
| Entorno de prueba compartido opcional: Render web Free + Neon Free | USD 0 |
| Email: Resend Free (3.000 por mes, 100 por día) [Verificado] | USD 0 |
| **Total** | **USD 0** |

Supuestos: sin usuarios reales. Riesgos: el web Free de Render se duerme tras 15 minutos sin tráfico y tarda alrededor de un minuto en despertar; Neon Free conserva solo 6 horas de historial. **Nunca usar Render Postgres Free para datos reales**: expira a los 30 días y se borra 14 días después [Verificado].

**Escenario 2. MVP con pocos entrenadores (piloto, ~200 usuarios)**

| Servicio | Opción R (recomendada) | Opción F (São Paulo) |
|---|---|---|
| App | Render 512 MB: USD 7 | Fly 512 MB: ~USD 4 a 6 [precio en gru a verificar] |
| Base | Render Postgres 256 MB: USD 6 + ~5 GB a USD 0,30 = ~USD 7,50 | Neon Launch: ~60 a 180 CU-h × USD 0,106 + 2 GB × USD 0,35 = ~USD 7 a 20 [Estimación según horas activas] |
| Backup externo diario (`pg_dump`) | Centavos en almacenamiento de objetos [A verificar proveedor] | Igual |
| Email | Resend Free: USD 0 | Igual |
| **Total estimado** | **~USD 15/mes** | **~USD 11 a 26/mes** |

**Escenario 3. Crecimiento inicial (decenas de entrenadores, ~3.000 usuarios)**

| Servicio | Opción R |
|---|---|
| App | Render 1 CPU / 2 GB: USD 25 |
| Base | Render Postgres 1 GB: USD 19 + ~10 GB a USD 0,30 = USD 22 |
| Workspace Pro (PITR de 7 días, equipo) | USD 25 (opcional) |
| Email | Resend Pro: USD 20 (50.000 por mes; probablemente necesario por el límite diario de 100 del plan gratuito) |
| **Total estimado** | **~USD 67 a 92/mes** |

Con unos 30 entrenadores pagando, el costo de infraestructura por entrenador queda en torno a USD 2 a 3 por mes [Estimación]. El precio de la suscripción se decide con criterios comerciales; la infraestructura no es la restricción.

### 11.4. Costos no incluidos

- **Impuestos argentinos** sobre servicios digitales del exterior pagados con tarjeta (IVA y posibles percepciones). [A verificar con un contador: alícuotas y regímenes vigentes]
- Tipo de cambio: los precios están en USD.
- Dominio (`.com.ar` en NIC Argentina o `.com`). [Precio a verificar]
- Asesoramiento legal (términos, privacidad, datos de salud).
- Comisiones del procesador de pagos cuando se integren suscripciones.
- Herramientas opcionales: monitoreo externo, gestor de errores.
- Minutos de GitHub Actions si el repositorio es privado y se supera la cuota gratuita. [A verificar cuota vigente]

### 11.5. Recomendación de hosting

**Opción R (Render, Virginia) para el MVP** [Opinión]: es la más simple de operar (app, base, backups y deploy en un solo panel), con precios fijos verificados y sin trucos de scale-to-zero. Desde Buenos Aires agrega latencia de red a cada petición. Estimo una ida y vuelta de 120 a 180 ms [Estimación no medida], aceptable para una app de formularios y consultas. **Medirla en las primeras semanas.** Si molesta, pasar a la Opción F: misma imagen Docker, `pg_dump`/`pg_restore`, cambio de DNS.

**Azure** es la opción con más valor de portfolio para un perfil .NET, pero hoy no se pudieron verificar sus precios y su oferta gratuita de base no sirve para producción. Reevaluar si Mauro consigue créditos (programas para startups o suscripciones de desarrollador).

La región también tiene implicancias legales (transferencia internacional, sección 10.3). **Decisión pendiente D6.**

### 11.6. CI/CD

**GitHub Actions** (asumiendo GitHub como host del repositorio [a confirmar]):

- **En cada pull request:** compilar y testear el backend (unitarios + integración con PostgreSQL real vía Testcontainers), verificar formato y analizadores; lint, tests y build del frontend; comparar el documento OpenAPI generado; construir la imagen Docker.
- **Al hacer merge a `master`:** construir y publicar la imagen; ejecutar las migraciones como paso explícito; desplegar; *smoke test* contra `/health`.
- **Entornos:** local y producción. Un entorno de staging se suma solo cuando haya usuarios que no deban ver errores, y puede ser un preview efímero por PR.
- Rollback: volver a la imagen anterior. Por eso las migraciones son compatibles hacia atrás.
