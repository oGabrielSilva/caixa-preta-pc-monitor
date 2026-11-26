// /llms.txt: a plain, factual summary for AI assistants and answer engines (https://llmstxt.org).
import { absoluteUrl } from './seo'
import { RELEASES_PAGE, REPO_URL, SITE_URL } from './site'

/** @param download Direct installer link, or the releases page while nothing is published. */
export function llmsTxt(download: string): string {
  return `# Caixa-Preta

> Caixa-Preta ("black box" in Portuguese) is a free, open-source (MIT) hardware monitor for Windows 11 that records the PC's sensors once per second in a way that survives power loss, detects every unexpected shutdown and explains its likely cause: power supply, overheating, motherboard power delivery or software.

Like a flight recorder, it records all the time so the crash can be explained afterwards. It was built by Gabriel Silva after his cat knocked his desktop off the desk (a fall of more than 80 cm) and the PC kept restarting on its own: he wanted to monitor the voltages to tell whether the motherboard or the power supply was at fault.

## Facts

- Platform: Windows 11, 64-bit. Installed by a single self-contained installer (CaixaPreta-Setup-<version>.exe); administrator rights required; no .NET needed.
- Records once per second: CPU temperature, load and power; the +12 V, +5 V and +3.3 V power supply rails as measured by the motherboard's sensor chip; fans; memory use; NVMe disk temperatures. Sensors are read through LibreHardwareMonitor; what is available depends on the motherboard.
- Storage: a local SQLite database in WAL mode, so the last seconds before a power cut are kept.
- Detection: after the PC comes back, it finds the unexpected shutdown in the Windows Event Log (Kernel-Power 41 and related events).
- Diagnosis: explainable and rule-based. It ranks hypotheses (power supply, motherboard power delivery, overheating, software crash, CPU or memory errors, forced power-off) by the evidence, shows the evidence for and against and the limits of each case. It gives a likely cause, never a certainty.
- Interface: a dashboard served on 127.0.0.1:5280, a tray app and an always-on-top mini panel that turns red when a reading leaves its normal range. Export to CSV, Excel and PDF.
- Privacy: everything stays on the machine. No cloud, no account, no telemetry.
- The installer is not code-signed: Windows SmartScreen warns on first run; each release publishes the SHA-256 hash of the installer.
- Architecture: three processes with least privilege (a collector Windows service, a read-only dashboard service and the tray app), .NET 10 and Nuxt 4, layered architecture enforced by tests, decisions recorded as ADRs (in Portuguese).
- Licence: MIT.

## Links

- [Website (Portuguese)](${SITE_URL})
- [Website (English)](${absoluteUrl('/en')})
- [Download the latest installer](${download})
- [Release history](${RELEASES_PAGE})
- [Source code](${REPO_URL})
- [Architecture decisions (ADRs)](${REPO_URL}/tree/main/docs/adr)
`
}
