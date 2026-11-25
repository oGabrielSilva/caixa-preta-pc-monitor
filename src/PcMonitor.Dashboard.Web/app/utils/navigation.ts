/** One entry of the main navigation. Labels are i18n keys, never literal text. */
export interface NavigationItem {
  readonly labelKey: string
  readonly icon: string
  readonly to: string
}

export const navigationItems: readonly NavigationItem[] = [
  { labelKey: 'nav.live', icon: 'i-material-symbols-monitoring-rounded', to: '/' },
  { labelKey: 'nav.incidents', icon: 'i-material-symbols-warning-outline-rounded', to: '/incidents' },
  { labelKey: 'nav.history', icon: 'i-material-symbols-show-chart-rounded', to: '/history' },
]
