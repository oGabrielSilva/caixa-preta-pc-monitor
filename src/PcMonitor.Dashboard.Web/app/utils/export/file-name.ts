/** `caixa-preta-<kind>-<YYYY-MM-DD-HHmm>.<ext>`, in local time. */
export function exportFileName(kind: string, extension: string, now: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  const stamp = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}-${pad(now.getHours())}${pad(now.getMinutes())}`
  return `caixa-preta-${kind}-${stamp}.${extension}`
}
