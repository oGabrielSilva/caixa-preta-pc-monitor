/** The WebView2 host (the Caixa-Preta tray app), when the page runs inside it. */
interface WebViewHost {
  postMessage(message: unknown): void
}

function host(): WebViewHost | undefined {
  return (globalThis as { chrome?: { webview?: WebViewHost } }).chrome?.webview
}

/** Asks the tray app to move the always-on-top panel with the mouse (no-op in a plain browser). */
export function startHostDrag(): void {
  host()?.postMessage({ type: 'drag' })
}

/** Asks the tray app to resize the always-on-top panel from its bottom-right corner (no-op in a plain browser). */
export function startHostResize(): void {
  host()?.postMessage({ type: 'resize' })
}

/** Ctrl + mouse wheel over the panel: asks the tray app for one step bigger (wheel up) or smaller. */
export function scaleHostWithWheel(event: WheelEvent): void {
  if (!event.ctrlKey || event.deltaY === 0) {
    return
  }
  event.preventDefault()
  host()?.postMessage({ type: 'scale', step: event.deltaY < 0 ? 1 : -1 })
}

/** Tells the tray app the panel's size in CSS pixels, rounded up, so its window fits the content exactly. */
export function reportHostSize(width: number, height: number): void {
  host()?.postMessage({ type: 'size', width: Math.ceil(width), height: Math.ceil(height) })
}

/** Reports the size of `target` to the tray app now and whenever it changes (new reading, warning shown, language). */
export function useHostSize(target: Readonly<Ref<HTMLElement | null>>): void {
  let observer: ResizeObserver | undefined
  onMounted(() => {
    if (!target.value || typeof ResizeObserver === 'undefined') {
      return
    }
    observer = new ResizeObserver(([entry]) => {
      const box = entry?.borderBoxSize[0]
      if (box) {
        reportHostSize(box.inlineSize, box.blockSize)
      }
    })
    observer.observe(target.value)
  })
  onBeforeUnmount(() => observer?.disconnect())
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
