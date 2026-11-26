# assets/brand

The Caixa-Preta icon (ADR 0017). `caixa-preta.svg` is the single source; every other file here and the dashboard's `public/favicon.ico` and `public/icon.svg` are generated from it.

- Change the icon only in `caixa-preta.svg`, then run `corepack pnpm icons` in `src/PcMonitor.Dashboard.Web` and commit the generated files.
- Generated: `caixa-preta.ico` (16–256 px, PNG entries; app, tray and installer), `caixa-preta-256.png`.
