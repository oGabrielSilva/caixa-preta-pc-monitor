/** Whether the navigation is expanded (desktop) or open (mobile menu), shared by the layout and each page's title bar. */
export function useSidebar() {
  return useState<boolean>('sidebar-open', () => true)
}
