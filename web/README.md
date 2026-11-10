# Run Fit Platform: aplicación web

Aplicación Angular 22 (componentes standalone, signals, Angular Material). Los pasos para levantar todo el entorno están en el `README.md` de la raíz.

```bash
npm ci
npm start              # http://localhost:4200, con proxy de /api a https://localhost:5001
npm run lint           # ESLint (incluye la regla que impide importar entre features)
npm run format:check   # Prettier
npm test               # Vitest
npm run build          # Build de producción en dist/web/browser
npm run e2e            # Playwright, escritorio y móvil
```

Estructura de `src/app`:

| Carpeta            | Contenido                                                                                      |
| ------------------ | ---------------------------------------------------------------------------------------------- |
| `core/`            | Sesión, llamadas a la API, manejo de errores (códigos de ProblemDetails a mensajes en español) |
| `shared/`          | Componentes y pipes reutilizables                                                              |
| `features/<área>/` | `auth`, `coach`, `planning`, `athlete`, `progress`, `admin`. No se importan entre sí           |
