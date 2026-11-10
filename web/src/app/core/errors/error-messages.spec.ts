import { DEFAULT_ERROR_MESSAGE, messageForErrorCode } from './error-messages';

describe('messageForErrorCode', () => {
  it('traduce un código conocido', () => {
    expect(messageForErrorCode('auth.unauthenticated')).toBe('Tu sesión expiró. Volvé a ingresar.');
  });

  it('usa el mensaje por defecto para un código desconocido', () => {
    expect(messageForErrorCode('planning.codigo_inexistente')).toBe(DEFAULT_ERROR_MESSAGE);
  });

  it('usa el mensaje por defecto si no hay código', () => {
    expect(messageForErrorCode(undefined)).toBe(DEFAULT_ERROR_MESSAGE);
  });
});
