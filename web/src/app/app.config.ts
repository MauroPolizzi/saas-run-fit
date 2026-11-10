import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withXsrfConfiguration } from '@angular/common/http';
import localeEsAr from '@angular/common/locales/es-AR';
import {
  ApplicationConfig,
  LOCALE_ID,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { loadAntiforgeryToken } from './core/api/antiforgery';

registerLocaleData(localeEsAr);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Mismo origen que la API: rutas relativas /api, cookie de sesión y cabecera X-XSRF-TOKEN.
    provideHttpClient(
      withFetch(),
      withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }),
    ),
    provideAppInitializer(loadAntiforgeryToken),
    { provide: LOCALE_ID, useValue: 'es-AR' },
  ],
};
