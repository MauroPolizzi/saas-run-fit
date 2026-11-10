import { routes } from './app.routes';

describe('routes', () => {
  it('carga cada área por rol de forma diferida', () => {
    const lazyPaths = routes.filter((r) => r.loadChildren).map((r) => r.path);
    expect(lazyPaths).toEqual(['', 'entrenador', 'alumno', 'admin']);
  });

  it('el panel de administración no está en el bundle inicial', () => {
    const admin = routes.find((r) => r.path === 'admin');
    expect(admin?.loadChildren).toBeDefined();
    expect(admin?.component).toBeUndefined();
  });
});
