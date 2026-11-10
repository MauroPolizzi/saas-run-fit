import { Routes } from '@angular/router';

import { PendingPage } from '../../shared/pending-page/pending-page';

/** Alumno: mi semana, registro, historial, perfil y salud (vista móvil primero). */
export const ATHLETE_ROUTES: Routes = [
  { path: '', component: PendingPage, data: { title: 'Mi semana' } },
];
