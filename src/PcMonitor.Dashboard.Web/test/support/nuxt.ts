import { mountSuspended } from '@nuxt/test-utils/runtime'

type Mount = typeof mountSuspended

/** Mounts a component in the Nuxt test runtime with the dashboard's default language (pt-BR). */
export const mountPtBR: Mount = async (component, options) => {
  await useNuxtApp().$i18n.setLocale('pt-BR')
  return mountSuspended(component, options)
}
