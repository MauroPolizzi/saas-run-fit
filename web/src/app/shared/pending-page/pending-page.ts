import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Pantalla provisoria para las rutas que se implementan en los próximos hitos. */
@Component({
  selector: 'cp-pending-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="pending">
      <h1>{{ title() }}</h1>
      <p>Esta pantalla todavía no está disponible.</p>
    </section>
  `,
  styles: `
    .pending {
      padding: 24px;
    }
  `,
})
export class PendingPage {
  /** Viene de `data.title` de la ruta (withComponentInputBinding). */
  readonly title = input('Próximamente');
}
