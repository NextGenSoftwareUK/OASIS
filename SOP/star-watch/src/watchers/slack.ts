import type { ObservedEvent } from '../types'
import { readConfig } from '../config'
import { getSlackApp } from '../slack-client'
import { log } from '../logger'

// ─── Slack Watcher ────────────────────────────────────────────────────────
// Uses the shared Slack App singleton (Socket Mode).
// Normalises Slack events into the shared ObservedEvent schema.

type EventCallback = (event: ObservedEvent) => void

export async function startSlackWatcher(emit: EventCallback): Promise<void> {
  const config = readConfig()
  const slackConfig = config.connectors.slack as {
    token: string
    appToken: string
    channels: string[]
  } | undefined

  if (!slackConfig?.token) {
    log.warn('Slack connector not configured. Run `star add slack` to connect.')
    return
  }

  const app = await getSlackApp()

  // ── Messages ─────────────────────────────────────────────────────────────
  app.event('message', async ({ event }) => {
    const msg = event as {
      type: string; subtype?: string; ts: string;
      user?: string; username?: string; text?: string; channel: string
    }
    if (msg.subtype || !msg.text) return
    if (!isWatchedChannel(slackConfig.channels, msg.channel)) return

    emit({
      id:        `slack-msg-${msg.ts}`,
      source:    'slack',
      timestamp: new Date(parseFloat(msg.ts) * 1000),
      actor: {
        id:   msg.user ?? msg.username ?? 'unknown',
        name: msg.username ?? msg.user ?? 'unknown',
      },
      action: 'message_sent',
      entity: { type: 'message', id: msg.ts, url: `https://slack.com/archives/${msg.channel}/p${msg.ts.replace('.', '')}` },
      payload: { text: msg.text, channel: msg.channel },
      context: [msg.text],
    })
  })

  // ── Reactions (e.g. ✅ on a task = implicit sign-off signal) ────────────
  app.event('reaction_added', async ({ event }) => {
    emit({
      id:        `slack-react-${event.item.ts}-${event.reaction}`,
      source:    'slack',
      timestamp: new Date(),
      actor: {
        id:   event.user,
        name: event.user,
      },
      action: 'reaction_added',
      entity: { type: 'message', id: event.item.ts },
      payload: { reaction: event.reaction, channel: (event.item as { channel?: string }).channel },
      context: [`Reacted with :${event.reaction}: on a message`],
    })
  })

  // ── File shares (evidence uploads) ───────────────────────────────────────
  app.event('file_shared', async ({ event }) => {
    const fe = event as { file_id: string; channel_id: string; user_id: string }
    emit({
      id:        `slack-file-${fe.file_id}`,
      source:    'slack',
      timestamp: new Date(),
      actor: { id: fe.user_id, name: fe.user_id },
      action:    'file_shared',
      entity:    { type: 'file', id: fe.file_id },
      payload:   { channel: fe.channel_id },
      context:   ['File shared in Slack channel'],
    })
  })

  await app.start()
  log.success(`Slack watcher connected (watching ${slackConfig.channels.length} channels)`)
}

function isWatchedChannel(watched: string[], channelId: string): boolean {
  if (!watched?.length) return true  // watch all if none configured
  return watched.includes(channelId) || watched.includes(`#${channelId}`)
}
