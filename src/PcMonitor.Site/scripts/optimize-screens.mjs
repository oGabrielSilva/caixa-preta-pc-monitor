// Converts the raw screenshots (screenshots/raw/*.png, not versioned) into the WebP files the site serves
// (public/screens/<id>-<width>.webp), at the widths declared in app/utils/screens.ts. Needs ffmpeg on the PATH
// (`winget install Gyan.FFmpeg`). Run with `pnpm screens` and commit public/screens.
import { execFileSync } from 'node:child_process'
import { mkdirSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const path = relative => fileURLToPath(new URL(relative, import.meta.url))

// Keep in step with SCREENS in app/utils/screens.ts (images.spec.ts checks the files against it).
const screens = { overview: [1600, 800], history: [1600, 800], incident: [1600, 800], mini: [804, 402] }

mkdirSync(path('../public/screens'), { recursive: true })
for (const [id, widths] of Object.entries(screens)) {
  for (const width of widths) {
    execFileSync('ffmpeg', [
      '-loglevel', 'error', '-y',
      '-i', path(`../screenshots/raw/${id}.png`),
      '-vf', `scale=${width}:-2:flags=lanczos`,
      '-c:v', 'libwebp', '-quality', '86', '-compression_level', '6',
      path(`../public/screens/${id}-${width}.webp`),
    ])
    console.log(`public/screens/${id}-${width}.webp`)
  }
}
