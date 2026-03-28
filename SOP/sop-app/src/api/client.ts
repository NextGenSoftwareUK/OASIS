const BASE = '/api'

let _token: string | null = localStorage.getItem('oasis_token')

export function setToken(t: string) {
  _token = t
  localStorage.setItem('oasis_token', t)
}

export function getToken(): string | null {
  return _token
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' }
  if (_token) headers['Authorization'] = `Bearer ${_token}`
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`${method} ${path} → ${res.status}: ${text.slice(0, 200)}`)
  }
  return res.json()
}

// ── Auth ────────────────────────────────────────────────────────────────────

export async function authenticate(username: string, password: string): Promise<string> {
  const data = await request<{ result?: { result?: { jwtToken?: string } } }>(
    'POST', '/avatar/authenticate', { username, password }
  )
  const token = data?.result?.result?.jwtToken
  if (!token) throw new Error('Authentication failed — no token returned')
  setToken(token)
  return token
}

// ── Workflows (SOPs) ────────────────────────────────────────────────────────

export interface WorkflowDefinition {
  id?: string
  name: string
  description: string
  version: string
  isPublic: boolean
  steps: WorkflowStep[]
  inputSchema: Record<string, { type: string; required: boolean }>
}

export interface WorkflowStep {
  id: string
  name: string
  connector: string
  action: string
  inputs: Record<string, string | number | boolean>
  onFailure: 'abort' | 'skip' | 'retry'
  condition?: string
}

export interface WorkflowExecution {
  executionId: string
  workflowId: string
  status: 'running' | 'completed' | 'failed'
  steps: ExecutionStep[]
  startedAt: string
  completedAt?: string
  proofHolonId?: string
}

export interface ExecutionStep {
  stepId: string
  name: string
  status: 'pending' | 'running' | 'success' | 'error' | 'skipped'
  output?: string
  error?: string
  durationMs?: number
}

export async function listMyWorkflows(): Promise<WorkflowDefinition[]> {
  const data = await request<{ result?: WorkflowDefinition[] }>('GET', '/workflow/my')
  return data?.result ?? []
}

export async function listPublicWorkflows(): Promise<WorkflowDefinition[]> {
  const data = await request<{ result?: WorkflowDefinition[] }>('GET', '/workflow/public')
  return data?.result ?? []
}

export async function getWorkflow(id: string): Promise<WorkflowDefinition> {
  const data = await request<{ result?: WorkflowDefinition }>('GET', `/workflow/${id}`)
  if (!data?.result) throw new Error(`Workflow ${id} not found`)
  return data.result
}

export async function executeWorkflow(
  workflowId: string,
  inputs: Record<string, unknown>
): Promise<WorkflowExecution> {
  const data = await request<{ result?: WorkflowExecution }>('POST', '/workflow/execute', {
    workflowId,
    inputs,
  })
  if (!data?.result) throw new Error('Failed to start workflow execution')
  return data.result
}

export async function getExecution(executionId: string): Promise<WorkflowExecution> {
  const data = await request<{ result?: WorkflowExecution }>(
    'GET', `/workflow/execution/${executionId}`
  )
  if (!data?.result) throw new Error(`Execution ${executionId} not found`)
  return data.result
}

export async function saveWorkflow(wf: Omit<WorkflowDefinition, 'id'>): Promise<string> {
  const data = await request<{ result?: { id?: string } }>('POST', '/workflow/save', wf)
  return data?.result?.id ?? `local-${Date.now()}`
}

// ── Execution step completion ────────────────────────────────────────────────

export async function completeStepApi(
  executionId: string,
  stepId: string,
  data: { completedBy: string; note?: string; evidence?: string[] }
): Promise<void> {
  await request('POST', `/workflow/execution/${executionId}/steps/${stepId}/complete`, data)
}

export async function verifyProof(holonId: string): Promise<{ verified: boolean; details: string }> {
  const data = await request<{ result?: { verified: boolean; details: string } }>(
    'GET', `/workflow/verify/${holonId}`
  )
  return data?.result ?? { verified: false, details: 'No result' }
}

// ── BRAID ───────────────────────────────────────────────────────────────────

export interface BraidResult {
  output: string
  tokensUsed: number
  graphHolonId?: string
  durationMs: number
}

export async function runBraid(
  taskType: string,
  inputs: Record<string, unknown>,
  holonId?: string
): Promise<BraidResult> {
  const data = await request<{ result?: BraidResult }>('POST', '/braid/run', {
    taskType,
    inputs,
    holonId,
    model: 'anthropic',
  })
  return data?.result ?? { output: '', tokensUsed: 0, durationMs: 0 }
}

// ── OAPPs (SOP templates on STARNET) ────────────────────────────────────────

export interface OAPPSummary {
  id: string
  name: string
  description: string
  isActive: boolean
  createdDate: string
}

export async function listOAPPs(): Promise<OAPPSummary[]> {
  const data = await request<{ result?: OAPPSummary[] }>('GET', '/OAPPs')
  return data?.result ?? []
}

// ── Holons (SOP run data) ───────────────────────────────────────────────────

export interface HolonData {
  id: string
  name: string
  metaData: Record<string, unknown>
  createdDate: string
}

export async function getHolon(id: string): Promise<HolonData> {
  const data = await request<{ result?: HolonData }>('GET', `/Holons/${id}`)
  if (!data?.result) throw new Error(`Holon ${id} not found`)
  return data.result
}
