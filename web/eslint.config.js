// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

/** Las features no se importan entre sí: lo común va a shared/ (UI) o core/ (sesión, API). */
const FEATURES = ['auth', 'coach', 'planning', 'athlete', 'progress', 'admin'];
const featureIsolation = FEATURES.map((feature) => ({
  files: [`src/app/features/${feature}/**/*.ts`],
  rules: {
    'no-restricted-imports': [
      'error',
      {
        patterns: FEATURES.filter((other) => other !== feature).map((other) => ({
          group: [
            `**/features/${other}`,
            `**/features/${other}/**`,
            `../${other}`,
            `../${other}/**`,
            `../../${other}/**`,
            `../../../${other}/**`,
          ],
          message: `features/${feature} no puede importar de features/${other}. Mové lo común a shared/ o core/.`,
        })),
      },
    ],
  },
}));

module.exports = defineConfig([
  {
    ignores: ['dist/**', '.angular/**', 'coverage/**', 'playwright-report/**', 'test-results/**'],
  },
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'cp',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'cp',
          style: 'kebab-case',
        },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
  ...featureIsolation,
]);
