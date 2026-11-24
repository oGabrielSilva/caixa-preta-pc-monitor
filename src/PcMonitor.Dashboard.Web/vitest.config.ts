import { defineVitestConfig } from '@nuxt/test-utils/config'

export default defineVitestConfig({
  test: {
    // Pure functions run in Node. Tests that need the Nuxt runtime opt in per file
    // with `// @vitest-environment nuxt`.
    environment: 'node',
    include: ['test/**/*.spec.ts'],
    // Booting the Nuxt test environment can take longer than the 10 s default on Windows.
    hookTimeout: 60_000,
  },
})
