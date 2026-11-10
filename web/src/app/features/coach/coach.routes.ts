import { Routes } from '@angular/router';

import { PendingPage } from '../../shared/pending-page/pending-page';

/** Entrenador: inicio, alumnos, grupos, invitaciones y perfil. Planificación y progreso se suman como hijas. */
export const COACH_ROUTES: Routes = [
  { path: '', component: PendingPage, data: { title: 'Inicio del entrenador' } },
];
