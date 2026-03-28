import fs   from 'fs'
import path from 'path'
import os   from 'os'
import type { StarConfig } from './types'

const STAR_DIR    = path.join(os.homedir(), '.star')
const CONFIG_PATH = path.join(STAR_DIR, 'config.json')
const LINKS_PATH  = path.join(STAR_DIR, 'links.json')
const LOG_DIR     = path.join(STAR_DIR, 'logs')

export function ensureStarDir() {
  fs.mkdirSync(STAR_DIR, { recursive: true })
  fs.mkdirSync(LOG_DIR,  { recursive: true })
}

export function configExists(): boolean {
  return fs.existsSync(CONFIG_PATH)
}

export function readConfig(): StarConfig {
  if (!configExists()) {
    throw new Error('Not connected. Run `star connect` first.')
  }
  return JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf-8')) as StarConfig
}

export function writeConfig(config: StarConfig): void {
  ensureStarDir()
  fs.writeFileSync(CONFIG_PATH, JSON.stringify(config, null, 2))
}

export function updateConfig(patch: Partial<StarConfig>): StarConfig {
  const current = configExists() ? readConfig() : ({} as StarConfig)
  const next = deepMerge(
    current as unknown as Record<string, unknown>,
    patch as unknown as Record<string, unknown>
  ) as unknown as StarConfig
  writeConfig(next)
  return next
}

// ─── Avatar link store ────────────────────────────────────────────────────

export interface AvatarLink {
  source:     string
  sourceId:   string
  sourceName?: string
  avatarId:   string
  avatarEmail: string
  linkedAt:   string
}

export function readLinks(): AvatarLink[] {
  if (!fs.existsSync(LINKS_PATH)) return []
  return JSON.parse(fs.readFileSync(LINKS_PATH, 'utf-8')) as AvatarLink[]
}

export function writeLink(link: AvatarLink): void {
  const links = readLinks().filter(
    l => !(l.source === link.source && l.sourceId === link.sourceId)
  )
  links.push(link)
  fs.writeFileSync(LINKS_PATH, JSON.stringify(links, null, 2))
}

export function resolveAvatar(source: string, sourceId: string): string | undefined {
  return readLinks().find(l => l.source === source && l.sourceId === sourceId)?.avatarId
}

// ─── Paths ────────────────────────────────────────────────────────────────

export const paths = {
  dir:    STAR_DIR,
  config: CONFIG_PATH,
  links:  LINKS_PATH,
  logs:   LOG_DIR,
  pid:    path.join(STAR_DIR, 'star-watch.pid'),
  log:    path.join(LOG_DIR, 'star-watch.log'),
}

// ─── Helpers ─────────────────────────────────────────────────────────────

function deepMerge(target: Record<string, unknown>, source: Record<string, unknown>): Record<string, unknown> {
  const result = { ...target }
  for (const key of Object.keys(source)) {
    if (source[key] && typeof source[key] === 'object' && !Array.isArray(source[key])) {
      result[key] = deepMerge(
        (target[key] as Record<string, unknown>) ?? {},
        source[key] as Record<string, unknown>
      )
    } else {
      result[key] = source[key]
    }
  }
  return result
}
