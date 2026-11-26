// The dashboard screenshots shown on the landing page (public/screens), taken as described in screenshots/README.md.

export type ScreenId = 'overview' | 'history' | 'incident' | 'mini'

export interface Screen {
  id: ScreenId
  /** Size of the largest image, for width/height attributes (no layout shift). */
  width: number
  height: number
  /** Generated widths, largest first; the smaller one serves narrow screens. */
  widths: [number, number]
  /** Taken from the demo database, not from a real machine: the caption says "sample data". */
  sample: boolean
}

export const SCREENS: Screen[] = [
  { id: 'overview', width: 1600, height: 1000, widths: [1600, 800], sample: false },
  { id: 'history', width: 1600, height: 1000, widths: [1600, 800], sample: false },
  { id: 'incident', width: 1600, height: 1000, widths: [1600, 800], sample: true },
  // The always-on-top mini panel, captured at 3× its 268 × 40 px size.
  { id: 'mini', width: 804, height: 120, widths: [804, 402], sample: false },
]

export function screen(id: ScreenId): Screen {
  return SCREENS.find(s => s.id === id)!
}

/** Site path of a screenshot at one of its generated widths. */
export function screenSrc(id: ScreenId, width: number): string {
  return `/screens/${id}-${width}.webp`
}

/** `srcset` with every generated width. */
export function screenSrcset(id: ScreenId, base = ''): string {
  return screen(id).widths.map(width => `${base}${screenSrc(id, width).slice(1)} ${width}w`).join(', ')
}
