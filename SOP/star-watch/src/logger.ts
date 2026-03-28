import fs from 'fs'
import { paths } from './config'

// ─── Logger ───────────────────────────────────────────────────────────────
// Writes to stdout (in foreground mode) and ~/.star/logs/star-watch.log.
// Uses simple ANSI colour codes — chalk is not imported here to keep this
// file dependency-free so it can be used before config is loaded.

const RESET  = '\x1b[0m'
const DIM    = '\x1b[2m'
const GREEN  = '\x1b[32m'
const CYAN   = '\x1b[36m'
const YELLOW = '\x1b[33m'
const RED    = '\x1b[31m'
const TEAL   = '\x1b[38;2;45;212;191m'

type Level = 'info' | 'success' | 'warn' | 'error' | 'event' | 'match'

function write(level: Level, message: string) {
  const ts     = new Date().toISOString()
  const prefix = {
    info:    `${DIM}${ts}${RESET} ${CYAN}info${RESET}   `,
    success: `${DIM}${ts}${RESET} ${GREEN}ok${RESET}     `,
    warn:    `${DIM}${ts}${RESET} ${YELLOW}warn${RESET}   `,
    error:   `${DIM}${ts}${RESET} ${RED}error${RESET}  `,
    event:   `${DIM}${ts}${RESET} ${TEAL}event${RESET}  `,
    match:   `${DIM}${ts}${RESET} ${TEAL}match${RESET}  `,
  }[level]

  const line = `${prefix}${message}`
  process.stdout.write(line + '\n')

  // Append plain text (no ANSI) to log file
  try {
    const plain = `[${ts}] [${level.toUpperCase().padEnd(7)}] ${message}\n`
    fs.appendFileSync(paths.log, plain)
  } catch {
    // Log dir may not exist yet at startup
  }
}

export const log = {
  info:    (msg: string) => write('info', msg),
  success: (msg: string) => write('success', msg),
  warn:    (msg: string) => write('warn', msg),
  error:   (msg: string) => write('error', msg),
  event:   (source: string, action: string, actor: string) =>
    write('event', `${source} · ${action} · ${actor}`),
  match:   (stepName: string, confidence: number, action: string) =>
    write('match', `"${stepName}" · ${Math.round(confidence * 100)}% · ${action}`),
}
