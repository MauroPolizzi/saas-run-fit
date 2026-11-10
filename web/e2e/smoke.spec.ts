import { expect, test } from '@playwright/test';

test('la aplicación carga y muestra la pantalla de ingreso', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveURL(/\/ingresar$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Ingresar');
  await expect(page.locator('mat-toolbar')).toContainText('Run Fit');
});
