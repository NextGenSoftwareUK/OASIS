import type { ActiveRun, MatchResult, ObservedEvent, SOPStep } from './types'
import { readConfig } from './config'

// ─── STAR API client ──────────────────────────────────────────────────────
// Thin wrapper around the OASIS STAR REST API used by star-watch.
// Mirrors the api/client.ts patterns from the SOP app.

async function request<T>(
  method: string,
  path: string,
  body?: unknown
): Promise<T> {
  const config = readConfig()
  const res = await fetch(`${config.apiBase}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${config.token}`,
    },
    body: body ? JSON.stringify(body) : undefined,
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`STAR API ${method} ${path} → ${res.status}: ${text}`)
  }
  return res.json() as Promise<T>
}

// ─── Active runs ─────────────────────────────────────────────────────────

export async function getActiveRuns(orgId: string): Promise<ActiveRun[]> {
  return request<ActiveRun[]>('GET', `/api/workflow/runs?orgId=${orgId}&status=running`)
}

// ─── Advance a run step ───────────────────────────────────────────────────

export async function completeStep(
  runId: string,
  stepId: string,
  data: {
    completedBy: string
    evidence: { eventId: string; source: string; excerpt?: string }
    autoCompleted: boolean
    confidence: number
  }
): Promise<void> {
  await request('POST', `/api/workflow/runs/${runId}/steps/${stepId}/complete`, data)
}

// ─── Record an escalation ────────────────────────────────────────────────

export async function recordEscalation(
  runId: string,
  stepId: string,
  assigneeAvatarId: string
): Promise<void> {
  await request('POST', `/api/workflow/runs/${runId}/steps/${stepId}/escalate`, {
    assigneeAvatarId,
    escalatedAt: new Date().toISOString(),
  })
}

// ─── Record sign-off from delivery channel ───────────────────────────────

export async function recordSignOff(
  runId: string,
  stepId: string,
  avatarId: string,
  channel: string
): Promise<void> {
  await request('POST', `/api/workflow/runs/${runId}/steps/${stepId}/signoff`, {
    avatarId,
    channel,
    signedAt: new Date().toISOString(),
  })
}

// ─── Record a deviation flag ─────────────────────────────────────────────

export async function recordDeviation(
  runId: string,
  stepId: string,
  flaggedBy: string,
  reason?: string
): Promise<void> {
  await request('POST', `/api/workflow/runs/${runId}/steps/${stepId}/deviation`, {
    flaggedBy,
    reason,
    flaggedAt: new Date().toISOString(),
  })
}

// ─── Auto-start a new run ─────────────────────────────────────────────────

export async function startRun(
  sopId: string,
  contextId: string,
  triggeredBy: { source: string; eventId: string }
): Promise<string> {
  const data = await request<{ runId: string }>(
    'POST',
    '/api/workflow/runs/start',
    { sopId, contextId, triggeredBy, startedAt: new Date().toISOString() }
  )
  return data.runId
}

// ─── BRAID pattern matching ───────────────────────────────────────────────
// Calls BRAID to semantically match an observed event to candidate SOP steps.

export interface BraidMatchResponse {
  stepId:     string | null
  confidence: number
  reasoning:  string
}

export async function braidMatch(
  event: ObservedEvent,
  candidates: SOPStep[],
  runContext: { runId: string; sopName: string; completedSteps: string[] }
): Promise<BraidMatchResponse> {
  return request<BraidMatchResponse>('POST', '/api/braid/match', {
    event,
    candidates,
    runContext,
  })
}

// ─── Write a StepCompletionHolon ──────────────────────────────────────────

export interface HolonPayload {
  type:          'SOPStepCompletionHolon'
  runId:         string
  sopId:         string
  stepId:        string
  completedBy:   string
  evidence:      { source: string; eventId: string; excerpt?: string; url?: string }
  autoCompleted: boolean
  confidence:    number
  timestamp:     string
}

export async function writeHolon(payload: HolonPayload): Promise<string> {
  const data = await request<{
    result?: {
      starnetdna?: { id?: string }
    }
    message?: string
  }>('POST', '/api/Holons/create', {
    name:        'SOPStepCompletionHolon',
    description: `STAR Watch — ${payload.stepId} completed. ${payload.sopId} · ${payload.completedBy}`,
    holonSubType: 0,
    createOptions: {
      starnetHolon: {},
      starnetdna: {
        name:              'SOPStepCompletionHolon',
        description:       'Immutable proof of SOP step completion recorded by STAR Watch',
        starnetHolonType:  'SOPStepCompletion',
      },
      checkIfSourcePathExists: false,
      customCreateParams: {
        runId:          payload.runId,
        sopId:          payload.sopId,
        stepId:         payload.stepId,
        completedBy:    payload.completedBy,
        autoCompleted:  payload.autoCompleted,
        confidence:     payload.confidence,
        timestamp:      payload.timestamp,
        evidence:       payload.evidence,
      },
    },
  })
  return data?.result?.starnetdna?.id ?? 'unknown'
}
