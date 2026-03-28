export type ConnectorType =
  | 'trigger'
  | 'holon'
  | 'avatar'
  | 'agent'
  | 'braid'
  | 'nft'
  | 'wallet'
  | 'escrow'
  | 'treasury'
  | 'quest'
  | 'mission'
  | 'geonfft'
  | 'egg'
  | 'leaderboard'
  | 'x402'
  | 'world'
  | 'karma'
  | 'bridge'
  | 'webhook'
  | 'condition'
  | 'delay'
  // Named integrations
  | 'slack'
  | 'email'
  | 'salesforce'
  | 'hubspot'
  | 'zendesk'
  | 'docusign'
  | 'google_calendar'
  | 'jira'
  | 'zapier'
  // SOP-specific nodes
  | 'sop_step'
  | 'sop_decision'
  | 'sop_signoff'
  | 'sop_ai_guide'

export interface ConnectorConfig {
  type: ConnectorType
  label: string
  description: string
  color: string
  bgColor: string
  borderColor: string
  icon: string
  inputs: ConfigField[]
  category: 'trigger' | 'data' | 'ai' | 'financial' | 'identity' | 'logic' | 'game' | 'integration' | 'sop'
}

export interface ConfigField {
  key: string
  label: string
  type: 'text' | 'select' | 'number' | 'textarea' | 'boolean'
  placeholder?: string
  options?: { label: string; value: string }[]
  required?: boolean
  default?: string | number | boolean
}

export interface WorkflowNodeData {
  connectorType: ConnectorType
  label: string
  config: Record<string, string | number | boolean>
  onFailure: 'abort' | 'skip' | 'retry'
  condition?: string
  executionStatus?: 'idle' | 'running' | 'success' | 'error' | 'skipped'
  executionOutput?: string
  executionDuration?: number
}

export interface WorkflowStep {
  id: string
  name: string
  connector: ConnectorType
  action: string
  inputs: Record<string, string | number | boolean>
  onFailure: 'abort' | 'skip' | 'retry'
  condition?: string
}

export interface WorkflowDefinition {
  id?: string
  name: string
  description: string
  version: string
  author?: string
  isPublic: boolean
  steps: WorkflowStep[]
  inputSchema: Record<string, { type: string; required: boolean }>
}

export interface ExecutionStep {
  stepId: string
  name: string
  status: 'pending' | 'running' | 'success' | 'error' | 'skipped'
  output?: string
  error?: string
  durationMs?: number
  tokensCost?: number
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

export interface WorkflowTemplate {
  id: string
  name: string
  description: string
  category: string
  author: string
  tags: string[]
  nodes: any[]
  edges: any[]
}
