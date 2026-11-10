# Runbooks

Procedimientos operativos. Cada uno indica cuándo se usa, los pasos exactos y cómo verificar el resultado.

| Runbook | Estado | Depende de |
|---|---|---|
| Crear el primer administrador (`dotnet RunFitPlatform.Api.dll admin create --email <email>`) | Pendiente: el comando se implementa en el Hito 1 | D6, para saber cómo ejecutar un comando en el entorno de producción |
| Restaurar un backup de la base | Pendiente | D6 (proveedor de PostgreSQL y herramienta de backup) |
| Revertir un despliegue a la imagen anterior | Pendiente | D6 |

Hasta definir D6, el desarrollo local está documentado en el `README.md` de la raíz.
