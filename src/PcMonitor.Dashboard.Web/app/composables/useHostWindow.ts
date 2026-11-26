/** The WebView2 host (the Caixa-Preta tray app), when the page runs inside it. */
interface WebViewHost {
  postMessage(message: unknown): void
}

function host(): WebViewHost | undefined {
  return (globalThis as { chrome?: { webview?: WebViewHost } }).chrome?.webview
}

/**
 * Opens a page of the dashboard. Inside the tray app the always-on-top panel asks the host to show the main window on
 * that page; in a plain browser it just navigates.
 */
export function openInMainWindow(path: string): void {
  const webview = host()
  if (webview) {
    webview.postMessage({ type: 'open', path })
  }
  else {
    void navigateTo(path)
  }
}
