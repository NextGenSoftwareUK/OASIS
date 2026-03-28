import { readConfig } from '../config'
import { log } from '../logger'
import { startDaemon } from '../daemon'
import { startDevMode } from '../dev-mode'

const TEAL  = '\x1b[38;2;45;212;191m'
const RESET = '\x1b[0m'
const DIM   = '\x1b[2m'
const BOLD  = '\x1b[1m'

export async function startWatch(options: { daemon?: boolean; install?: boolean; verbose?: boolean; dev?: boolean }): Promise<void> {

  // Dev mode — no credentials required, fully self-contained demo
  if (options.dev) {
    await startDevMode()
    return
  }

  try {
    readConfig()  // will throw if not connected
  } catch {
    console.error('Not connected. Run `star connect` first, or use `star watch --dev` to try without credentials.')
    process.exit(1)
  }

  console.log(`
${BOLD}${TEAL}★ STAR Watch${RESET}  ${DIM}background SOP intelligence${RESET}
`)

  if (options.daemon) {
    console.log('Daemon mode not yet implemented in this scaffold. Run without --daemon for now.')
    process.exit(0)
  }

  // Foreground mode — logs to stdout
  await startDaemon({ verbose: options.verbose })

  // Keep running until interrupted
  process.on('SIGINT',  () => { log.info('Shutting down…'); process.exit(0) })
  process.on('SIGTERM', () => { log.info('Shutting down…'); process.exit(0) })
}
