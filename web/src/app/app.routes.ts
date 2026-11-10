import { Routes } from '@angular/router';

import { PendingPage } from './shared/pending-page/pending-page';

/**
 * Rutas por rol, todas con carga diferida. Los guards que se agreguen son solo de experiencia de usuario:
 * la seguridad está en la API.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'ingresar' },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: 'entrenador',
    loadChildren: () => import('./features/coach/coach.routes').then((m) => m.COACH_ROUTES),
  },
  {
    path: 'alumno',
    loadChildren: () => import('./features/athlete/athlete.routes').then((m) => m.ATHLETE_ROUTES),
  },
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
  },
  { path: '**', component: PendingPage, data: { title: 'Página no encontrada' } },
];
