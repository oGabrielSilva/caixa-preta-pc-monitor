// Semantic colours are defined once here and used by meaning, never by hue:
// success = within the safe range, warning = attention, error = out of range.
export default defineAppConfig({
  ui: {
    colors: {
      primary: 'sky',
      success: 'emerald',
      warning: 'amber',
      error: 'rose',
      neutral: 'zinc',
    },
  },
})
