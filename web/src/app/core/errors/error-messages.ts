/**
 * Mensajes en español para los códigos de ProblemDetails (`code`). El texto del servidor nunca se
 * muestra tal cual. Los módulos agregan sus códigos (`<módulo>.<motivo>`) a medida que se implementan.
 */
const MESSAGES: Readonly<Record<string, string>> = {
  'validation.failed': 'Revisá los datos ingresados.',
  'auth.unauthenticated': 'Tu sesión expiró. Volvé a ingresar.',
  'auth.forbidden': 'No tenés permiso para realizar esta acción.',
  'auth.antiforgery_invalid':
    'La sesión de seguridad expiró. Recargá la página e intentá de nuevo.',
  'resource.not_found': 'No encontramos lo que buscabas.',
  'request.too_large': 'Los datos enviados superan el tamaño permitido.',
  'concurrency.conflict':
    'Alguien modificó estos datos mientras los editabas. Recargá para ver los cambios.',
  'conflict.duplicate': 'Ya existe un registro con esos datos.',
  'rate_limit.exceeded': 'Hiciste demasiados intentos. Esperá unos minutos y volvé a probar.',
  'server.error': 'Ocurrió un error inesperado. Intentá de nuevo más tarde.',
};

export const DEFAULT_ERROR_MESSAGE = 'Ocurrió un error inesperado. Intentá de nuevo más tarde.';

/** Mensaje para el usuario a partir del `code` de un ProblemDetails. */
export function messageForErrorCode(code: string | null | undefined): string {
  return (code && MESSAGES[code]) || DEFAULT_ERROR_MESSAGE;
}
