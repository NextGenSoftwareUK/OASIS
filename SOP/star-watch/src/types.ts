// ─── Connector types ───────────────────────────────────────────────────────

export type ConnectorType =
  | 'slack'
  | 'salesforce'
  | 'github'
  | 'email'
  | 'jira'
  | 'notion'
  | 'zendesk'
  | 'zapier'
  | 'sop_step'
  | 'sop_signoff'
  | 'sop_decision'

// ─── Normalised event from any connector ──────────────────────────────────

export interface ObservedEvent {
  id:        string
  source:    ConnectorType
  timestamp: Date
  actor: {
    id:       string         // user/system ID in the source tool
    name:     string
    email?:   string
    avatarId?: string        // OASIS Avatar ID, resolved after linking
  }
  action:    string          // e.g. 'message_sent', 'stage_changed', 'pr_merged'
  entity: {
    type:    string          // e.g. 'opportunity', 'pull_request', 'message'
    id:      string
    name?:   string
    url?:    string
  }
  payload:   Record<string, unknown>
  context:   string[]        // text snippets for BRAID semantic matching
}

// ─── SOP step (from STAR API) ─────────────────────────────────────────────

export interface SOPStep {
  id:                string
  name:              string
  connector:         ConnectorType
  action:            string
  inputs:            Record<string, unknown>
  requiresSignOff?:  boolean
  requiresEvidence?: boolean
  triggerConditions?: TriggerCondition[]
}

export interface TriggerCondition {
  field:    string
  operator: 'equals' | 'changed_to' | 'contains' | 'changed_from_to'
  value:    string
  value2?:  string   // used for 'changed_from_to'
}

// ─── Active SOP run ───────────────────────────────────────────────────────

export interface ActiveRun {
  id:           string
  sopId:        string
  sopName:      string
  sopVersion:   string
  contextId?:   string   // e.g. Salesforce Opportunity ID
  currentStepIndex: number
  steps:        SOPStep[]
  assignees:    Record<string, string>  // stepId → avatarId
  startedAt:    Date
  lastUpdatedAt: Date
}

// ─── Matcher result ───────────────────────────────────────────────────────

export interface MatchResult {
  runId:      string
  stepId:     string
  step:       SOPStep
  run:        ActiveRun
  event:      ObservedEvent
  confidence: number          // 0–1
  reasoning:  string          // BRAID explanation
  action:     MatchAction
}

export type MatchAction =
  | 'auto_complete'    // confidence ≥ 0.92, no sign-off required
  | 'escalate'         // confidence ≥ 0.75, sign-off required or borderline
  | 'soft_notify'      // confidence 0.50–0.75, informational
  | 'log_only'         // confidence < 0.50

// ─── Delivery message ─────────────────────────────────────────────────────

export interface DeliveryMessage {
  type:       'sign_off_request' | 'auto_complete_notice' | 'deviation_alert' | 'digest'
  match?:     MatchResult
  message:    string
  actions?:   ('sign_off' | 'flag_deviation' | 'snooze_1h' | 'open_in_app')[]
  expiresAt?: Date
}

// ─── Config ───────────────────────────────────────────────────────────────

export interface StarConfig {
  avatar:   string
  token:    string
  apiBase:  string
  org:      string
  connectors: Partial<Record<ConnectorType, ConnectorConfig>>
  sops: {
    autoLoad:         boolean
    orgId:            string
    pinnedTemplates?: string[]
  }
  escalation: {
    defaultChannel:         'slack' | 'email' | 'browser'
    requireSignOff:         boolean
    notifyOnAutoComplete:   boolean
    digestSchedule:         string  // cron expression
  }
}

export interface ConnectorConfig {
  [key: string]: unknown
}
