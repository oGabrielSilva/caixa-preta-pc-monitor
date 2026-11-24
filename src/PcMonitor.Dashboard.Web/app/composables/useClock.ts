/** The current time (ms), refreshed every `intervalMs` while the component is mounted. */
export function useClock(intervalMs: number) {
  const now = ref(Date.now())
  let timer: ReturnType<typeof setInterval> | undefined
  onMounted(() => {
    timer = setInterval(() => (now.value = Date.now()), intervalMs)
  })
  onScopeDispose(() => clearInterval(timer))
  return now
}
