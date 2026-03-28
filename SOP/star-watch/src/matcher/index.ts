import type { ObservedEvent, ActiveRun, SOPStep, MatchResult, MatchAction } from '../types'
import { braidMatch } from '../star-api'
import { resolveAvatar } from '../config'

// ─── Confidence thresholds ────────────────────────────────────────────────

const THRESHOLD_AUTO_COMPLETE = 0.92
const THRESHOLD_ESCALATE      = 0.75
const THRESHOLD_SOFT_NOTIFY   = 0.50

// ─── Main matching pipeline ───────────────────────────────────────────────

export async function matchEvent(
  event: ObservedEvent,
  activeRuns: ActiveRun[]
): Promise<MatchResult | null> {

  // Step 1: Pre-filter — which runs have steps that could match this event?
  const candidates = findCandidateSteps(event, activeRuns)
  if (candidates.length === 0) return null

  // Step 2: Rule-based trigger check (zero latency, no BRAID call)
  const ruleMatch = checkTriggerConditions(event, candidates)
  if (ruleMatch) {
    return buildResult(ruleMatch.run, ruleMatch.step, event, 0.99, 'Exact rule-based trigger match')
  }

  // Step 3: BRAID semantic match (falls back gracefully if API not available)
  for (const { run, steps } of groupByRun(candidates)) {
    const completedSteps = run.steps
      .slice(0, run.currentStepIndex)
      .map(s => s.id)

    try {
      const response = await braidMatch(event, steps, {
        runId: run.id,
        sopName: run.sopName,
        completedSteps,
      })

      if (response.stepId && response.confidence >= THRESHOLD_SOFT_NOTIFY) {
        const step = steps.find(s => s.id === response.stepId)!
        return buildResult(run, step, event, response.confidence, response.reasoning)
      }
    } catch {
      // BRAID API not available — fall back to keyword matching on trigger conditions
      const keywordMatch = keywordFallback(event, steps)
      if (keywordMatch) {
        return buildResult(run, keywordMatch, event, 0.85, 'Keyword match (BRAID unavailable)')
      }
    }
  }

  return null
}

// ─── Rule-based trigger matching ─────────────────────────────────────────

interface Candidate { run: ActiveRun; step: SOPStep }

function checkTriggerConditions(
  event: ObservedEvent,
  candidates: Candidate[]
): Candidate | null {
  for (const { run, step } of candidates) {
    if (!step.triggerConditions?.length) continue
    for (const cond of step.triggerConditions) {
      if (evaluateTrigger(event, cond)) return { run, step }
    }
  }
  return null
}

function evaluateTrigger(event: ObservedEvent, cond: { field: string; operator: string; value: string; value2?: string }): boolean {
  const raw = getNestedValue(event.payload, cond.field)
  if (raw === undefined) return false
  const val = String(raw)
  switch (cond.operator) {
    case 'equals':           return val === cond.value
    case 'changed_to':       return val === cond.value
    case 'contains':         return val.includes(cond.value)
    case 'changed_from_to':  return val === cond.value2 &&
                                    String(getNestedValue(event.payload, `previous_${cond.field}`)) === cond.value
    default:                 return false
  }
}

function getNestedValue(obj: Record<string, unknown>, path: string): unknown {
  return path.split('.').reduce((acc: unknown, key) => {
    if (acc && typeof acc === 'object') return (acc as Record<string, unknown>)[key]
    return undefined
  }, obj)
}

// ─── Keyword fallback (used when BRAID API is unavailable) ───────────────

function keywordFallback(event: ObservedEvent, steps: SOPStep[]): SOPStep | null {
  const text = String(event.payload.text ?? event.context[0] ?? '').toLowerCase()
  for (const step of steps) {
    for (const cond of step.triggerConditions ?? []) {
      if (cond.operator === 'contains' && text.includes(cond.value.toLowerCase())) {
        return step
      }
    }
  }
  return null
}

// ─── Pre-filter: connector + action match ─────────────────────────────────

function findCandidateSteps(event: ObservedEvent, runs: ActiveRun[]): Candidate[] {
  const candidates: Candidate[] = []
  for (const run of runs) {
    // Look at the current step and the next two (allowing slight out-of-order)
    const windowStart = Math.max(0, run.currentStepIndex)
    const windowEnd   = Math.min(run.steps.length, run.currentStepIndex + 3)
    const window      = run.steps.slice(windowStart, windowEnd)

    for (const step of window) {
      const connectorMatch = step.connector === event.source || step.connector === 'sop_step'
      // Also include steps with explicit trigger conditions — a Slack message can
      // serve as evidence for any step regardless of its connector type
      const hasTriggers = (step.triggerConditions?.length ?? 0) > 0
      if (connectorMatch || hasTriggers) {
        candidates.push({ run, step })
      }
    }
  }
  return candidates
}

function groupByRun(candidates: Candidate[]): { run: ActiveRun; steps: SOPStep[] }[] {
  const map = new Map<string, { run: ActiveRun; steps: SOPStep[] }>()
  for (const { run, step } of candidates) {
    if (!map.has(run.id)) map.set(run.id, { run, steps: [] })
    map.get(run.id)!.steps.push(step)
  }
  return Array.from(map.values())
}

// ─── Build a MatchResult ──────────────────────────────────────────────────

function buildResult(
  run: ActiveRun,
  step: SOPStep,
  event: ObservedEvent,
  confidence: number,
  reasoning: string
): MatchResult {
  let action: MatchAction

  const requiresSignOff = step.requiresSignOff === true || step.inputs?.requiresSignOff === true
  if (confidence >= THRESHOLD_AUTO_COMPLETE && !requiresSignOff) {
    action = 'auto_complete'
  } else if (confidence >= THRESHOLD_ESCALATE) {
    action = 'escalate'
  } else if (confidence >= THRESHOLD_SOFT_NOTIFY) {
    action = 'soft_notify'
  } else {
    action = 'log_only'
  }

  // Resolve actor's OASIS Avatar ID if linked
  if (!event.actor.avatarId) {
    event.actor.avatarId = resolveAvatar(event.source, event.actor.id)
  }

  return { runId: run.id, stepId: step.id, step, run, event, confidence, reasoning, action }
}
