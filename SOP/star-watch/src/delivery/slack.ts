import type { MatchResult, DeliveryMessage } from '../types'
import { readConfig } from '../config'
import { getSlackApp } from '../slack-client'
import { recordSignOff, recordDeviation, writeHolon } from '../star-api'
import { log } from '../logger'

// ─── Pending match registry ───────────────────────────────────────────────
// Stores match context so the sign-off button handler can write a proof holon
// without needing to re-query the run state.

const pendingMatches = new Map<string, MatchResult>()

function matchKey(runId: string, stepId: string) { return `${runId}:${stepId}` }

export function registerPendingMatch(match: MatchResult) {
  pendingMatches.set(matchKey(match.runId, match.stepId), match)
}

// ─── Slack Delivery Adapter ───────────────────────────────────────────────

export async function initSlackDelivery(): Promise<void> {
  const config = readConfig()
  const slackConfig = config.connectors.slack as { token: string; appToken: string } | undefined
  if (!slackConfig?.token) return

  const slackApp = await getSlackApp()

  // ── Button action handlers ────────────────────────────────────────────────
  slackApp.action('star_sign_off', async ({ ack, body, action, client }) => {
    await ack()
    const payload = JSON.parse((action as { value: string }).value) as { runId: string; stepId: string }
    const userId  = (body as { user: { id: string } }).user.id
    const channel = (body as { channel?: { id: string } }).channel?.id
    const ts      = (body as { message?: { ts: string } }).message?.ts

    // Update the card immediately so the UX responds whether or not the API is up
    if (channel && ts) {
      await client.chat.update({
        channel,
        ts,
        blocks: confirmedBlocks(payload, `Signed off by <@${userId}>`),
        text:   'Step signed off',
      }).catch(e => log.warn(`Card update failed: ${e}`))
    }

    log.success(`Step ${payload.stepId} signed off via Slack by ${userId}`)

    // Write immutable proof holon to STARNET
    const match = pendingMatches.get(matchKey(payload.runId, payload.stepId))
    writeHolon({
      type:          'SOPStepCompletionHolon',
      runId:         payload.runId,
      sopId:         match?.run.sopId ?? payload.runId,
      stepId:        payload.stepId,
      completedBy:   userId,
      evidence: {
        source:  'slack',
        eventId: `signoff-${userId}-${Date.now()}`,
        excerpt: match?.event.context[0]?.slice(0, 120),
        url:     match?.event.entity.url,
      },
      autoCompleted: false,
      confidence:    match?.confidence ?? 1,
      timestamp:     new Date().toISOString(),
    })
      .then(holonId => log.success(`Proof holon written to STARNET: ${holonId}`))
      .catch(err    => log.warn(`Holon write failed (will retry): ${err}`))

    // Also attempt the signoff endpoint (for when it's implemented)
    recordSignOff(payload.runId, payload.stepId, userId, 'slack').catch(() => {})
  })

  slackApp.action('star_flag_deviation', async ({ ack, body, action }) => {
    await ack()
    const payload = JSON.parse((action as { value: string }).value) as { runId: string; stepId: string }
    const userId  = (body as { user: { id: string } }).user.id
    await recordDeviation(payload.runId, payload.stepId, userId, 'Flagged via Slack').catch(() => {})
    log.warn(`Deviation flagged on step ${payload.stepId} by ${userId}`)
  })

  slackApp.action('star_snooze', async ({ ack }) => { await ack() })
  slackApp.action('star_open_app', async ({ ack }) => { await ack() })
}

// ─── Send a sign-off request ──────────────────────────────────────────────

export async function sendSignOffRequest(
  channelOrUserId: string,
  match: MatchResult
): Promise<void> {
  const app = await getSlackApp().catch(() => null)
  if (!app) {
    log.warn('Slack delivery not initialised — cannot send sign-off request')
    return
  }

  // Register this match so the button handler can write a proof holon
  registerPendingMatch(match)

  const stepIndex = match.run.steps.findIndex(s => s.id === match.stepId) + 1
  const actionPayload = JSON.stringify({ runId: match.runId, stepId: match.stepId })
  const appUrl = `http://localhost:5176/runner/${match.runId}`

  await app.client.chat.postMessage({
    channel: channelOrUserId,
    text:    `Sign-off needed: ${match.run.sopName} · Step ${stepIndex}`,
    blocks: [
      {
        type: 'section',
        text: {
          type: 'mrkdwn',
          text: `*STAR* · _${match.run.sopName}_`,
        },
      },
      { type: 'divider' },
      {
        type: 'section',
        fields: [
          { type: 'mrkdwn', text: `*Step ${stepIndex} of ${match.run.steps.length}*\n${match.step.name}` },
          { type: 'mrkdwn', text: `*Evidence detected*\n${match.event.source} · ${match.event.action.replace(/_/g, ' ')}` },
          { type: 'mrkdwn', text: `*Confidence*\n${Math.round(match.confidence * 100)}%` },
          { type: 'mrkdwn', text: `*Detected*\nJust now` },
        ],
      },
      {
        type: 'context',
        elements: [{ type: 'mrkdwn', text: `_${match.reasoning}_` }],
      },
      {
        type: 'actions',
        elements: [
          {
            type: 'button',
            text:   { type: 'plain_text', text: 'Sign off', emoji: false },
            style:  'primary',
            action_id: 'star_sign_off',
            value:  actionPayload,
          },
          {
            type: 'button',
            text:  { type: 'plain_text', text: 'Flag deviation', emoji: false },
            style: 'danger',
            action_id: 'star_flag_deviation',
            value: actionPayload,
          },
          {
            type: 'button',
            text:  { type: 'plain_text', text: 'Snooze 1h', emoji: false },
            action_id: 'star_snooze',
            value: actionPayload,
          },
          {
            type: 'button',
            text:  { type: 'plain_text', text: 'Open in app ↗', emoji: false },
            url:   appUrl,
            action_id: 'star_open_app',
            value: actionPayload,
          },
        ],
      },
    ],
  })

  log.info(`Sign-off request sent to ${channelOrUserId} for step ${match.stepId}`)
}

// ─── Silent auto-completion notice ────────────────────────────────────────

export async function sendAutoCompleteNotice(
  channelOrUserId: string,
  match: MatchResult
): Promise<void> {
  const app = await getSlackApp().catch(() => null)
  if (!app) return
  const stepIndex = match.run.steps.findIndex(s => s.id === match.stepId) + 1
  await app.client.chat.postMessage({
    channel: channelOrUserId,
    text: `STAR auto-completed Step ${stepIndex} of ${match.run.sopName}: ${match.step.name}`,
    blocks: [
      {
        type: 'context',
        elements: [{
          type: 'mrkdwn',
          text: `*STAR* auto-completed *Step ${stepIndex}* of _${match.run.sopName}_ · ${match.step.name} · Confidence ${Math.round(match.confidence * 100)}%`,
        }],
      },
    ],
  })
}

// ─── Helpers ─────────────────────────────────────────────────────────────

function confirmedBlocks(payload: { runId: string; stepId: string }, status: string) {
  return [
    {
      type: 'section',
      text: {
        type: 'mrkdwn',
        text: `*STAR* · :white_check_mark: ${status}\nStep \`${payload.stepId}\` recorded. Run continues.`,
      },
    },
  ]
}
