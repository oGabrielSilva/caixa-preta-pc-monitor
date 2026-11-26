import { defineVitestConfig } from '@nuxt/test-utils/config'

// Unit tests (test/unit) run in plain Node; page tests (test/nuxt) opt in with `// @vitest-environment nuxt`.
export default defineVitestConfig({
  test: {
    environment: 'node',
    include: ['test/**/*.spec.ts'],
    // Booting the Nuxt test environment can take longer than the 10 s default on Windows.
    hookTimeout: 60_000,
  },
})
