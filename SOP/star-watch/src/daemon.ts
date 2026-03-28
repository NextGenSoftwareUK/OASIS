import type { ObservedEvent, ActiveRun } from './types'
import { readConfig } from './config'
import { getActiveRuns, completeStep, writeHolon } from './star-api'
import { matchEvent } from './matcher'
import { startSlackWatcher } from './watchers/slack'
import { startSalesforceWatcher } from './watchers/salesforce'
import { initSlackDelivery, sendSignOffRequest, sendAutoCompleteNotice } from './delivery/slack'
import { log } from './logger'

// ─── Daemon: the main loop ────────────────────────────────────────────────
// Starts all configured watchers, maintains the active run cache,
// and routes matched events to the appropriate action (auto-complete / escalate).

const RUN_CACHE_TTL_MS = 30_000  // refresh active runs every 30s

let activeRuns: ActiveRun[] = []
let lastRunFetch = 0

export async function startDaemon(options: { verbose?: boolean } = {}): Promise<void> {
  const config = readConfig()
  log.info(`STAR Watch starting — org: ${config.org}`)

  // Initial run cache
  await refreshRunCache()

  // Refresh run cache on schedule
  setInterval(async () => {
    await refreshRunCache()
  }, RUN_CACHE_TTL_MS)

  // Initialise delivery adapters
  await initSlackDelivery()

  // Build event handler — wrapped so one bad event never crashes the daemon
  const handleEvent = async (event: ObservedEvent) => {
    try {
    if (options.verbose) {
      log.event(event.source, event.action, event.actor.name)
    }

    const match = await matchEvent(event, activeRuns)
    if (!match) return

    log.match(match.step.name, match.confidence, match.action)

    switch (match.action) {

      case 'auto_complete': {
        // Write proof holon first (non-blocking on completeStep API)
        completeStep(match.runId, match.stepId, {
          completedBy:   'star-watch',
          evidence:      { eventId: match.event.id, source: match.event.source, excerpt: match.event.context[0] },
          autoCompleted: true,
          confidence:    match.confidence,
        }).catch(() => {})  // endpoint not yet implemented server-side

        const holonId = await writeHolon({
          type:          'SOPStepCompletionHolon',
          runId:         match.runId,
          sopId:         match.run.sopId,
          stepId:        match.stepId,
          completedBy:   match.event.actor.avatarId ?? 'star-watch',
          evidence:      { source: match.event.source, eventId: match.event.id, excerpt: match.event.context[0], url: match.event.entity.url },
          autoCompleted: true,
          confidence:    match.confidence,
          timestamp:     new Date().toISOString(),
        }).catch(() => 'pending')
        log.success(`Proof holon written to STARNET: ${holonId}`)
        // Notify in the channel where the event happened
        if (config.escalation.notifyOnAutoComplete) {
          const defaultChannel = (config.connectors.slack as { deliverTo?: string } | undefined)?.deliverTo
          const notifyChannel  = resolveAssigneeChannel(match.runId, match.stepId)
            ?? (defaultChannel || undefined)
            ?? (match.event.payload.channel as string | undefined)
          if (notifyChannel) await sendAutoCompleteNotice(notifyChannel, match)
        }
        // Invalidate run cache so next event sees updated step index
        activeRuns = activeRuns.map(r =>
          r.id === match.runId
            ? { ...r, currentStepIndex: r.currentStepIndex + 1 }
            : r
        )
        break
      }

      case 'escalate': {
        const assigneeChannel = resolveAssigneeChannel(match.runId, match.stepId)
        const defaultChannel  = (config.connectors.slack as { deliverTo?: string } | undefined)?.deliverTo
        // Priority: assignee DM → configured deliverTo channel → channel where event was detected
        const targetChannel   = assigneeChannel
          ?? (defaultChannel || undefined)
          ?? (match.event.payload.channel as string | undefined)
        if (targetChannel) await sendSignOffRequest(targetChannel, match)
        break
      }

      case 'soft_notify': {
        if (options.verbose) {
          log.info(`Soft notify: "${match.step.name}" — ${Math.round(match.confidence * 100)}% confidence, no action taken`)
        }
        break
      }

      case 'log_only':
      default:
        break
    }
    } catch (err) {
      log.error(`Event handler error: ${err}`)
    }
  }

  // Start all configured watchers
  const connectors = Object.keys(config.connectors ?? {}) as (keyof typeof config.connectors)[]

  if (connectors.includes('slack'))       await startSlackWatcher(handleEvent)
  if (connectors.includes('salesforce'))  await startSalesforceWatcher(handleEvent)
  // Phase 2+: github, email, jira watchers will slot in here

  log.success(`STAR Watch running — monitoring ${connectors.length} connector(s)`)
  log.info(`Active SOP runs: ${activeRuns.length}`)
  log.info(`Press Ctrl+C to stop.\n`)
}

// ─── Active run cache ─────────────────────────────────────────────────────

async function refreshRunCache(): Promise<void> {
  const now = Date.now()
  if (now - lastRunFetch < RUN_CACHE_TTL_MS) return
  try {
    const config = readConfig()
    const runs = await getActiveRuns(config.sops.orgId)
    lastRunFetch = now
    // If the API returns runs, use them; otherwise keep existing cache
    if (runs.length > 0) {
      activeRuns = runs
      log.info(`Run cache refreshed — ${activeRuns.length} active run(s)`)
    } else if (activeRuns.length === 0) {
      // Seed a demo run so the matcher has something to work with while
      // the workflow runs API is being stood up
      activeRuns = [DEMO_RUN]
      log.info(`No active runs in API — seeded 1 demo run for matching`)
    }
  } catch {
    // API endpoint not yet implemented — seed demo run silently
    if (activeRuns.length === 0) {
      activeRuns = [DEMO_RUN]
      log.info(`Runs API not available — seeded 1 demo run for matching`)
    }
    lastRunFetch = now
  }
}

// ─── Demo run (used when /api/workflow/runs is not yet implemented) ────────

import type { SOPStep } from './types'

const DEMO_SOP_STEPS: SOPStep[] = [
  { id: 's1', name: 'Send welcome email + kickoff link', connector: 'email', action: 'send', inputs: { role: 'CustomerSuccessManager', requiresSignOff: false }, triggerConditions: [{ field: 'text', operator: 'contains', value: 'welcome email' }] },
  { id: 's2', name: 'Technical discovery call',          connector: 'sop_signoff', action: 'sign', inputs: { role: 'SolutionsEngineer', requiresSignOff: true }, triggerConditions: [{ field: 'text', operator: 'contains', value: 'discovery call' }] },
  { id: 's3', name: '7-day activation check-in',         connector: 'sop_step', action: 'complete', inputs: { role: 'CustomerSuccessManager', requiresSignOff: false }, triggerConditions: [{ field: 'text', operator: 'contains', value: 'activation' }] },
  { id: 's4', name: 'Go-live sign-off (customer)',        connector: 'sop_signoff', action: 'sign', inputs: { role: 'CustomerAdmin', requiresSignOff: true }, triggerConditions: [{ field: 'text', operator: 'contains', value: 'go-live' }] },
  { id: 's5', name: 'Update Salesforce + notify Slack',  connector: 'salesforce', action: 'update_opportunity', inputs: { role: 'System', requiresSignOff: false }, triggerConditions: [{ field: 'text', operator: 'contains', value: 'salesforce' }] },
]

const DEMO_RUN: import('./types').ActiveRun = {
  id: 'run-live-demo',
  sopId: 'sop-enterprise-onboarding',
  sopName: 'Enterprise Customer Onboarding v3',
  currentStepIndex: 0,
  steps: DEMO_SOP_STEPS,
  assignees: {},
  startedAt: new Date().toISOString(),
}

// ─── Assignee resolution ──────────────────────────────────────────────────

function resolveAssigneeChannel(runId: string, stepId: string): string | undefined {
  const run = activeRuns.find(r => r.id === runId)
  if (!run) return undefined
  // run.assignees maps stepId → avatarId
  // In Phase 2 this will look up the avatar's preferred delivery channel
  // For now, return the Slack user ID if the avatarId looks like one (U...)
  const avatarId = run.assignees[stepId]
  if (avatarId?.startsWith('U')) return avatarId  // Slack user ID
  return undefined
}
