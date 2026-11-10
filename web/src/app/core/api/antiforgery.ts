import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

/** Endpoint que emite la cookie legible XSRF-TOKEN. */
export const ANTIFORGERY_URL = '/api/auth/antiforgery';

/**
 * Pide el token antiforgery al iniciar la aplicación. También hay que llamarlo después de login y logout,
 * porque el token queda ligado a la identidad. Si la API no responde, la aplicación arranca igual:
 * el token se vuelve a pedir antes de la primera operación que lo necesite.
 */
export async function loadAntiforgeryToken(): Promise<void> {
  const http = inject(HttpClient);
  try {
    await firstValueFrom(http.get(ANTIFORGERY_URL));
  } catch {
    // Sin API disponible no hay nada que proteger todavía.
  }
}
