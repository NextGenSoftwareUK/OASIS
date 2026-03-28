import type { ActiveRun, ObservedEvent, SOPStep } from './types'
import { log } from './logger'

// ─── Dev / Demo Mode ─────────────────────────────────────────────────────
// Runs star-watch entirely in-memory:
//   - No credentials, no Slack tokens, no STAR API
//   - Mock active runs built from a demo SOP
//   - Synthetic events drip in every few seconds
//   - Rule-based + keyword matching (no BRAID call)
//   - Logs exactly what the real daemon would do
//
// Run with:  star watch --dev

const TEAL   = '\x1b[38;2;45;212;191m'
const GREEN  = '\x1b[38;2;34;197;94m'
const AMBER  = '\x1b[38;2;245;158;11m'
const PURPLE = '\x1b[38;2;139;92;246m'
const DIM    = '\x1b[2m'
const BOLD   = '\x1b[1m'
const RESET  = '\x1b[0m'

// ── Demo SOP ─────────────────────────────────────────────────────────────

const DEMO_STEPS: SOPStep[] = [
  {
    id: 's1', name: 'Send welcome email + kickoff link',
    connector: 'email', action: 'send',
    inputs: { role: 'CustomerSuccessManager', requiresSignOff: false, requiresEvidence: false, description: 'Sends personalised welcome email with calendar links.' },
    triggerConditions: [{ field: 'text', operator: 'contains', value: 'welcome email' }],
  },
  {
    id: 's2', name: 'Technical discovery call',
    connector: 'sop_signoff', action: 'sign',
    inputs: { role: 'SolutionsEngineer', requiresSignOff: true, requiresEvidence: false, description: 'Discovery call notes signed off by SE.' },
    triggerConditions: [{ field: 'text', operator: 'contains', value: 'discovery call' }],
  },
  {
    id: 's3', name: '7-day activation check-in',
    connector: 'sop_step', action: 'complete',
    inputs: { role: 'CustomerSuccessManager', requiresSignOff: false, requiresEvidence: true, description: 'Target ≥40% feature activation by Day 7.' },
    triggerConditions: [{ field: 'text', operator: 'contains', value: 'activation' }],
  },
  {
    id: 's4', name: 'Go-live sign-off (customer)',
    connector: 'sop_signoff', action: 'sign',
    inputs: { role: 'CustomerAdmin', requiresSignOff: true, requiresEvidence: false, description: 'Customer Avatar wallet sign-off.' },
    triggerConditions: [{ field: 'text', operator: 'contains', value: 'go-live' }],
  },
  {
    id: 's5', name: 'Update Salesforce + notify Slack',
    connector: 'salesforce', action: 'update_opportunity',
    inputs: { role: 'System', requiresSignOff: false, requiresEvidence: false, description: 'Automated CRM update.' },
    triggerConditions: [{ field: 'text', operator: 'contains', value: 'salesforce update' }],
  },
]

const MOCK_RUN: ActiveRun = {
  id: 'run-dev-001',
  sopId: 'sop-enterprise-onboarding',
  sopName: 'Enterprise Customer Onboarding v3',
  currentStepIndex: 0,
  steps: DEMO_STEPS,
  assignees: { s2: 'Udev-engineer', s4: 'Udev-admin' },
  startedAt: new Date().toISOString(),
}

// ── Synthetic event stream ───────────────────────────────────────────────
// Each entry is [delayMs, ObservedEvent]

type SyntheticEntry = [number, Omit<ObservedEvent, 'id' | 'timestamp' | 'actor'>]

const SYNTHETIC_EVENTS: SyntheticEntry[] = [
  [3000, {
    source: 'slack', action: 'message_sent',
    entity: { type: 'message', id: 'msg-001', url: 'https://slack.com/archives/C01/pmsg001' },
    payload: { text: 'Just sent the welcome email and kickoff link to the Acme team!', channel: 'C01' },
    context:  ['Just sent the welcome email and kickoff link to the Acme team!'],
  }],
  [5000, {
    source: 'slack', action: 'message_sent',
    entity: { type: 'message', id: 'msg-002' },
    payload: { text: 'Scheduling the technical discovery call for Thursday 2pm.' },
    context: ['Scheduling the technical discovery call for Thursday 2pm.'],
  }],
  [4000, {
    source: 'slack', action: 'message_sent',
    entity: { type: 'message', id: 'msg-003' },
    payload: { text: 'Discovery call complete — great session. Complexity score 3 (Enterprise path).' },
    context: ['Discovery call complete — great session. Complexity score 3 (Enterprise path).'],
  }],
  [6000, {
    source: 'slack', action: 'message_sent',
    entity: { type: 'message', id: 'msg-004' },
    payload: { text: 'Day 7 check — Acme activation at 62%. Ahead of target. Moving to go-live phase.' },
    context: ['Day 7 check — Acme activation at 62%. Ahead of target.'],
  }],
  [5000, {
    source: 'slack', action: 'message_sent',
    entity: { type: 'message', id: 'msg-005' },
    payload: { text: 'Customer confirmed go-live. Getting sign-off from their admin now.' },
    context: ['Customer confirmed go-live. Getting sign-off from their admin now.'],
  }],
  [4000, {
    source: 'salesforce', action: 'opportunity_updated',
    entity: { type: 'opportunity', id: 'opp-001' },
    payload: { text: 'salesforce update: stage changed to Customer', stage: 'Customer' },
    context: ['Opportunity stage updated to Customer in Salesforce'],
  }],
]

// ── Keyword matching (no BRAID needed in dev mode) ───────────────────────

interface DevMatch {
  step: SOPStep
  confidence: number
  reason: string
}

function devMatch(event: ObservedEvent, run: ActiveRun): DevMatch | null {
  const windowStart = Math.max(0, run.currentStepIndex)
  const windowEnd   = Math.min(run.steps.length, run.currentStepIndex + 3)
  const candidates  = run.steps.slice(windowStart, windowEnd)

  const text = String(event.payload.text ?? event.context[0] ?? '').toLowerCase()

  for (const step of candidates) {
    for (const cond of step.triggerConditions ?? []) {
      if (cond.operator === 'contains' && text.includes(cond.value.toLowerCase())) {
        return { step, confidence: 0.94, reason: `Message contains "${cond.value}"` }
      }
    }
  }
  return null
}

// ── Action simulation ─────────────────────────────────────────────────────

function simulateAction(match: DevMatch, run: ActiveRun, event: ObservedEvent) {
  const stepIdx = run.steps.findIndex(s => s.id === match.step.id) + 1
  const needsSignOff = match.step.inputs?.requiresSignOff === true

  if (needsSignOff || match.confidence < 0.92) {
    // Escalate — would send Slack Block Kit message
    console.log(`${AMBER}  ↑ ESCALATE${RESET}  ${DIM}Sign-off required for step ${stepIdx}${RESET}`)
    console.log(`    ${DIM}Would send to Slack: "${BOLD}Sign-off needed: ${run.sopName} · Step ${stepIdx}${RESET}${DIM}"${RESET}`)
    console.log(`    ${DIM}Buttons: [Sign off] [Flag deviation] [Snooze 1h] [Open in app]${RESET}`)
  } else {
    // Auto-complete — would write proof holon
    console.log(`${GREEN}  ✓ AUTO-COMPLETE${RESET}  ${DIM}Confidence ${Math.round(match.confidence * 100)}% — no human required${RESET}`)
    console.log(`    ${DIM}Would call: POST /api/workflow/runs/${run.id}/steps/${match.step.id}/complete${RESET}`)
    console.log(`    ${DIM}Would write: SOPStepCompletionHolon { runId, stepId, evidence: { source: '${event.source}', excerpt: '${event.context[0].slice(0, 60)}…' } }${RESET}`)
  }

  console.log()
  // Advance the mock run
  run.currentStepIndex = Math.min(run.steps.length - 1, run.currentStepIndex + 1)
}

// ── Main dev daemon ───────────────────────────────────────────────────────

export async function startDevMode(): Promise<void> {
  console.log(`${BOLD}${TEAL}★ STAR Watch${RESET}  ${DIM}— dev mode (no credentials required)${RESET}`)
  console.log()
  console.log(`${DIM}  Loaded 1 mock active run:${RESET}`)
  console.log(`  ${TEAL}${BOLD}${MOCK_RUN.sopName}${RESET}  ${DIM}run-id: ${MOCK_RUN.id}${RESET}`)
  console.log(`  ${DIM}${MOCK_RUN.steps.length} steps · starting at Step 1${RESET}`)
  console.log()
  console.log(`${DIM}  Streaming ${SYNTHETIC_EVENTS.length} synthetic Slack events…${RESET}`)
  console.log(`${DIM}  (In production, these arrive from real Slack/Salesforce/GitHub/etc.)${RESET}`)
  console.log()
  console.log(`  ${'─'.repeat(62)}`)
  console.log()

  const run = { ...MOCK_RUN }  // mutable copy
  let eventIdx = 0

  for (const [delay, partial] of SYNTHETIC_EVENTS) {
    await sleep(delay)

    const event: ObservedEvent = {
      ...partial,
      id: `dev-${Date.now()}-${eventIdx++}`,
      timestamp: new Date(),
      actor: { id: 'Udev-kelly', name: 'Kelly', avatarId: 'avatar-kelly-001' },
    }

    // Header: event received
    const stepLabel = run.steps[run.currentStepIndex]?.name ?? 'unknown'
    console.log(`${TEAL}● EVENT${RESET}  ${DIM}${event.source} · ${event.action}${RESET}`)
    console.log(`  ${DIM}Actor:${RESET}  ${event.actor.name}`)
    console.log(`  ${DIM}Text:${RESET}   "${String(event.payload.text ?? '').slice(0, 72)}"`)
    console.log()

    // Current step context
    console.log(`  ${DIM}Active step:${RESET} ${BOLD}${stepLabel}${RESET}  ${DIM}(Step ${run.currentStepIndex + 1}/${run.steps.length})${RESET}`)
    console.log()

    // Match
    const match = devMatch(event, run)
    if (match) {
      console.log(`${PURPLE}  ≈ MATCH${RESET}   ${DIM}${match.reason} — confidence ${Math.round(match.confidence * 100)}%${RESET}`)
      console.log(`  ${DIM}Matched step:${RESET} "${match.step.name}"`)
      console.log()
      simulateAction(match, run, event)
    } else {
      console.log(`  ${DIM}No match — event logged, no action taken${RESET}`)
      console.log()
    }

    console.log(`  ${'─'.repeat(62)}`)
    console.log()
  }

  console.log(`${GREEN}${BOLD}★ Dev run complete.${RESET}  ${DIM}All ${SYNTHETIC_EVENTS.length} events processed.${RESET}`)
  console.log()
  console.log(`  ${DIM}To connect to real Slack and OASIS:${RESET}`)
  console.log(`  ${TEAL}star connect${RESET}   ${DIM}→ authenticate with OASIS${RESET}`)
  console.log(`  ${TEAL}star add slack${RESET} ${DIM}→ add your Slack workspace${RESET}`)
  console.log(`  ${TEAL}star watch${RESET}     ${DIM}→ start the live daemon${RESET}`)
  console.log()
}

function sleep(ms: number): Promise<void> {
  return new Promise(r => setTimeout(r, ms))
}
