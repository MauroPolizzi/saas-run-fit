import { Routes } from '@angular/router';

import { PendingPage } from '../../shared/pending-page/pending-page';

/** Panel de administración. Se carga de forma diferida y solo para PlatformAdmin. */
export const ADMIN_ROUTES: Routes = [
  { path: '', component: PendingPage, data: { title: 'Administración' } },
];
