import type { App } from '@slack/bolt'
import { readConfig } from './config'

// ─── Shared Slack App singleton ───────────────────────────────────────────
// A single Socket Mode connection handles both inbound message events AND
// outbound button action callbacks. Two separate App instances would open
// two WebSocket connections — action payloads only arrive on the one that
// registered the handler, so sign-off buttons would silently fail.

let _app: App | null = null

export async function getSlackApp(): Promise<App> {
  if (_app) return _app

  const config = readConfig()
  const slackConfig = config.connectors.slack as {
    token: string; appToken: string
  } | undefined

  if (!slackConfig?.token || !slackConfig?.appToken) {
    throw new Error('Slack not configured. Run `star add slack` first.')
  }

  const { App } = await import('@slack/bolt')
  _app = new App({
    token:      slackConfig.token,
    appToken:   slackConfig.appToken,
    socketMode: true,
  })
  return _app
}
