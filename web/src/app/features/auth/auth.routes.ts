import { Routes } from '@angular/router';

import { PendingPage } from '../../shared/pending-page/pending-page';

/** Ingreso, recuperación de contraseña, invitaciones y 2FA. */
export const AUTH_ROUTES: Routes = [
  { path: 'ingresar', component: PendingPage, data: { title: 'Ingresar' } },
  { path: 'invitacion', component: PendingPage, data: { title: 'Aceptar invitación' } },
  { path: 'restablecer', component: PendingPage, data: { title: 'Restablecer contraseña' } },
];
