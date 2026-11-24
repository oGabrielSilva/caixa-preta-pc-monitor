/** One entry of the main navigation. Labels are i18n keys, never literal text. */
export interface NavigationItem {
  readonly labelKey: string
  readonly icon: string
  readonly to: string
}

export const navigationItems: readonly NavigationItem[] = [
  { labelKey: 'nav.live', icon: 'i-lucide-activity', to: '/' },
  { labelKey: 'nav.incidents', icon: 'i-lucide-triangle-alert', to: '/incidents' },
  { labelKey: 'nav.history', icon: 'i-lucide-chart-line', to: '/history' },
]
