/** Pretends the page runs inside the Caixa-Preta tray app: returns the messages posted to it. */
export function fakeWebViewHost(): unknown[] {
  const messages: unknown[] = []
  Object.assign(globalThis, { chrome: { webview: { postMessage: (message: unknown) => messages.push(message) } } })
  return messages
}

/** Back to a plain browser. */
export function removeWebViewHost(): void {
  Reflect.deleteProperty(globalThis, 'chrome')
}
