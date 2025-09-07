// @ts-check
const eslint = require('@eslint/js');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

module.exports = tseslint.config(
  {
    files: ['**/*.ts'],
    ignores: ['**/generated-apis.ts'],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'app',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'app',
          style: 'kebab-case',
        },
      ],
      // Magic value rules
      '@typescript-eslint/no-magic-numbers': [
        'warn',
        {
          ignore: [
            -1, 0, 1, 2, 10, 12, 50, 100, 123, 300, 400, 401, 403, 404, 409,
            422, 429, 500, 502, 503, 1000, 2000, 4000, 5000, 10000, 60000,
            300000,
          ],
          ignoreArrayIndexes: true,
          ignoreDefaultValues: true,
          ignoreClassFieldInitialValues: true,
          ignoreNumericLiteralTypes: true,
          ignoreReadonlyClassProperties: true,
          ignoreEnums: true,
          ignoreTypeIndexes: true,
        },
      ],
      'no-magic-numbers': 'off', // Use TypeScript version instead
      '@typescript-eslint/prefer-literal-enum-member': 'warn',
      // Standard ESLint rules for magic values and strings
      'prefer-template': 'warn',
      'no-implicit-coercion': 'warn',
      'prefer-named-capture-group': 'warn',
      // Additional code quality rules
      'no-duplicate-case': 'error',
      'no-fallthrough': 'error',
      'no-unreachable': 'error',
      eqeqeq: ['warn', 'always'],
      // String and array rules
      'prefer-const': 'warn',
      'no-var': 'error',
      // Prevent common issues with magic strings/numbers
      radix: 'warn', // Require radix parameter in parseInt()
      // File size and complexity rules
      'max-lines': [
        'warn',
        { max: 500, skipBlankLines: true, skipComments: true },
      ],
      'max-lines-per-function': [
        'warn',
        { max: 50, skipBlankLines: true, skipComments: true },
      ],
      '@typescript-eslint/max-params': ['warn', { max: 4 }],
      complexity: ['warn', { max: 10 }],
    },
  },
  {
    files: ['**/*.html'],
    extends: [
      ...angular.configs.templateRecommended,
      ...angular.configs.templateAccessibility,
    ],
    rules: {
      '@angular-eslint/template/label-has-associated-control': 'warn',
    },
  },
);
