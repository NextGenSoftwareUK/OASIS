import { useState } from 'react'
import {
  CheckCircle, Warning, Circle, Copy, ArrowSquareOut,
  Terminal, Lightning, Plug, ChartBar
} from '@phosphor-icons/react'

// ─── Types ────────────────────────────────────────────────────────────────

type ConnectorStatus = 'connected' | 'disconnected' | 'error'

// Brandfetch CDN — swap {domain} per connector
// /w/{n}/h/{n} requests the icon at a specific resolution from the CDN
const BRANDFETCH_KEY = '1ida8ggQZDf64bgCqxt'
const logo = (domain: string, px = 80) =>
  `https://cdn.brandfetch.io/domain/${domain}/w/${px}/h/${px}/icon?c=${BRANDFETCH_KEY}`

interface Connector {
  id:          string
  name:        string
  description: string
  status:      ConnectorStatus
  logoDomain:  string           // passed to Brandfetch CDN
  iconFallback: string          // shown if logo fails to load
  iconBg:      string
  iconColor:   string
  eventsToday: number | null
  matchRate:   number | null    // 0–1
  lastEvent:   string | null
  setupCmd:    string
  docsUrl:     string
  phase:       1 | 2 | 3
}

interface EventLogEntry {
  id:         string
  time:       string
  connector:  string
  action:     string
  matched:    boolean
  confidence: number | null
  stepName:   string | null
  runName:    string | null
  outcome:    'auto_complete' | 'escalated' | 'no_match' | 'pending'
}

// ─── Demo data ────────────────────────────────────────────────────────────

const CONNECTORS: Connector[] = [
  {
    id: 'slack',
    name: 'Slack',
    description: 'Watch channels for step completions, sign-off confirmations, and file shares.',
    status: 'connected',
    logoDomain: 'slack.com',
    iconFallback: 'S',
    iconBg: 'rgba(255,255,255,0.04)',
    iconColor: '#E01E5A',
    eventsToday: 47,
    matchRate: 0.31,
    lastEvent: '2 min ago',
    setupCmd: 'star add slack',
    docsUrl: 'https://api.slack.com/apps',
    phase: 1,
  },
  {
    id: 'salesforce',
    name: 'Salesforce',
    description: 'Track Opportunity stage changes, Case status updates, and Task completions.',
    status: 'disconnected',
    logoDomain: 'salesforce.com',
    iconFallback: 'SF',
    iconBg: 'rgba(255,255,255,0.04)',
    iconColor: '#00A1E0',
    eventsToday: null,
    matchRate: null,
    lastEvent: null,
    setupCmd: 'star add salesforce',
    docsUrl: 'https://developer.salesforce.com/docs/atlas.en-us.api_streaming.meta/api_streaming',
    phase: 2,
  },
  {
    id: 'github',
    name: 'GitHub',
    description: 'Watch PR merges, issue closes, and review approvals for engineering SOPs.',
    status: 'disconnected',
    logoDomain: 'github.com',
    iconFallback: 'GH',
    iconBg: 'rgba(255,255,255,0.04)',
    iconColor: '#E0E0E0',
    eventsToday: null,
    matchRate: null,
    lastEvent: null,
    setupCmd: 'star add github',
    docsUrl: 'https://docs.github.com/webhooks',
    phase: 3,
  },
  {
    id: 'email',
    name: 'Email (IMAP)',
    description: 'Monitor inboxes for approval chains, client communications, and compliance triggers.',
    status: 'disconnected',
    logoDomain: 'google.com',
    iconFallback: 'EM',
    iconBg: 'rgba(255,255,255,0.04)',
    iconColor: '#EA4335',
    eventsToday: null,
    matchRate: null,
    lastEvent: null,
    setupCmd: 'star add email',
    docsUrl: '',
    phase: 2,
  },
  {
    id: 'jira',
    name: 'Jira',
    description: 'Watch issue status changes, sprint events, and epic completions.',
    status: 'disconnected',
    logoDomain: 'atlassian.com',
    iconFallback: 'JR',
    iconBg: 'rgba(255,255,255,0.04)',
    iconColor: '#0052CC',
    eventsToday: null,
    matchRate: null,
    lastEvent: null,
    setupCmd: 'star add jira',
    docsUrl: 'https://developer.atlassian.com/cloud/jira/platform/webhooks',
    phase: 3,
  },
  {
    id: 'zapier',
    name: 'Zapier',
    description: 'Connect any of 5,000+ apps via Zapier webhooks — no custom connector needed.',
    status: 'disconnected',
    logoDomain: 'zapier.com',
    iconFallback: 'ZP',
    iconBg: 'rgba(255,255,255,0.04)',
    iconColor: '#FF4A00',
    eventsToday: null,
    matchRate: null,
    lastEvent: null,
    setupCmd: 'star add zapier',
    docsUrl: 'https://zapier.com',
    phase: 3,
  },
  {
    id: 'zendesk',
    name: 'Zendesk',
    description: 'Track ticket status changes and customer support SOP completions.',
    status: 'disconnected',
    logoDomain: 'zendesk.com',
    iconFallback: 'ZD',
    iconBg: 'rgba(255,255,255,0.04)',
    iconColor: '#03363D',
    eventsToday: null,
    matchRate: null,
    lastEvent: null,
    setupCmd: 'star add zendesk',
    docsUrl: 'https://developer.zendesk.com/api-reference/webhooks',
    phase: 2,
  },
  {
    id: 'notion',
    name: 'Notion',
    description: 'Detect page updates, database entries, and checklist completions in Notion.',
    status: 'disconnected',
    logoDomain: 'notion.so',
    iconFallback: 'NO',
    iconBg: 'rgba(255,255,255,0.04)',
    iconColor: '#E0E0E0',
    eventsToday: null,
    matchRate: null,
    lastEvent: null,
    setupCmd: 'star add notion',
    docsUrl: 'https://developers.notion.com',
    phase: 3,
  },
]

const EVENT_LOG: EventLogEntry[] = [
  { id: 'e1', time: '14:22', connector: 'Slack',       action: 'message_sent',   matched: true,  confidence: 0.97, stepName: 'Go-live sign-off',       runName: 'Enterprise Onboarding v3', outcome: 'escalated'     },
  { id: 'e2', time: '14:18', connector: 'Slack',       action: 'file_shared',    matched: true,  confidence: 0.94, stepName: 'Upload evidence',         runName: 'DD Process — Fund',        outcome: 'auto_complete' },
  { id: 'e3', time: '14:11', connector: 'Slack',       action: 'message_sent',   matched: false, confidence: 0.22, stepName: null,                      runName: null,                       outcome: 'no_match'      },
  { id: 'e4', time: '14:04', connector: 'Slack',       action: 'reaction_added', matched: true,  confidence: 0.81, stepName: '7-day activation check-in', runName: 'Enterprise Onboarding v3', outcome: 'escalated'     },
  { id: 'e5', time: '13:51', connector: 'Slack',       action: 'message_sent',   matched: false, confidence: 0.18, stepName: null,                      runName: null,                       outcome: 'no_match'      },
  { id: 'e6', time: '13:44', connector: 'Slack',       action: 'file_shared',    matched: true,  confidence: 0.99, stepName: 'Contract upload',         runName: 'DD Process — Fund',        outcome: 'auto_complete' },
]

// ─── Sub-components ───────────────────────────────────────────────────────

// ─── Brandfetch logo — sized exactly to the container ────────────────────
// Pass the container's pixel size so the img uses explicit px dimensions,
// avoiding the flex/percentage sizing ambiguity entirely.

function BrandLogo({
  domain,
  fallback,
  size = 40,
}: {
  domain:   string
  fallback: string
  size?:    number   // must match the wrapping container's width/height
}) {
  const [failed, setFailed] = useState(false)
  // Request 2× for retina sharpness
  const cdnPx = size * 2

  if (failed) {
    return (
      <span style={{
        fontSize: `${Math.max(9, Math.round(size * 0.28))}px`,
        fontWeight: 700, letterSpacing: '0.04em',
        fontFamily: 'var(--font-sans)', color: 'var(--text2)',
      }}>
        {fallback}
      </span>
    )
  }

  return (
    <img
      src={logo(domain, cdnPx)}
      alt={domain}
      onError={() => setFailed(true)}
      style={{
        width:      size,
        height:     size,
        objectFit:  'contain',
        display:    'block',
        flexShrink: 0,
      }}
    />
  )
}

function StatusDot({ status }: { status: ConnectorStatus }) {
  const color = status === 'connected' ? '#2DD4BF' : status === 'error' ? '#EF4444' : '#383838'
  return (
    <span style={{
      display: 'inline-block', width: 7, height: 7, borderRadius: '50%',
      background: color,
      boxShadow: status === 'connected' ? `0 0 6px ${color}66` : 'none',
      flexShrink: 0,
    }} />
  )
}

function ConnectorCard({ c, onSetup }: { c: Connector; onSetup: (c: Connector) => void }) {
  const isConnected = c.status === 'connected'

  return (
    <div className="panel-card" style={{ padding: '22px 24px', display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{
            width: 40, height: 40, borderRadius: 10,
            background: c.iconBg,
            border: '0.5px solid rgba(255,255,255,0.08)',
            flexShrink: 0,
            overflow: 'hidden',
          }}>
            <BrandLogo domain={c.logoDomain} fallback={c.iconFallback} size={40} />
          </div>
          <div>
            <div style={{ fontFamily: 'var(--font-sans)', fontWeight: 600, fontSize: '0.9rem', color: '#DCDCDC', marginBottom: 2 }}>{c.name}</div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <StatusDot status={c.status} />
              <span style={{ fontSize: '0.72rem', color: isConnected ? '#2DD4BF' : '#484848', fontWeight: 500 }}>
                {isConnected ? 'Connected' : c.phase === 1 ? 'Ready to connect' : `Phase ${c.phase}`}
              </span>
            </div>
          </div>
        </div>
        {isConnected ? (
          <button className="btn-ghost" style={{ fontSize: '0.78rem' }}>Manage</button>
        ) : (
          <button
            onClick={() => onSetup(c)}
            className="btn-primary"
            style={{ fontSize: '0.78rem' }}
          >
            Connect
          </button>
        )}
      </div>

      {/* Description */}
      <p style={{ fontSize: '0.85rem', color: '#525252', lineHeight: 1.65, margin: 0 }}>{c.description}</p>

      {/* Stats (connected only) */}
      {isConnected && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 1, background: 'rgba(255,255,255,0.05)', borderRadius: 6, overflow: 'hidden' }}>
          {[
            { label: 'Events today', value: c.eventsToday ?? '—' },
            { label: 'Match rate',   value: c.matchRate != null ? `${Math.round(c.matchRate * 100)}%` : '—' },
            { label: 'Last event',   value: c.lastEvent ?? '—' },
          ].map(s => (
            <div key={s.label} style={{ background: '#0A0A0A', padding: '10px 14px' }}>
              <div style={{ fontSize: '0.62rem', color: '#484848', textTransform: 'uppercase', letterSpacing: '0.09em', fontWeight: 600, marginBottom: 4 }}>{s.label}</div>
              <div style={{ fontSize: '0.95rem', fontFamily: 'var(--font-mono)', color: '#C0C0C0', fontWeight: 500 }}>{s.value}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function OutcomeChip({ outcome }: { outcome: EventLogEntry['outcome'] }) {
  const map = {
    auto_complete: { label: 'Auto-completed', color: '#2DD4BF', bg: 'rgba(45,212,191,0.07)' },
    escalated:     { label: 'Escalated',      color: '#60A5FA', bg: 'rgba(96,165,250,0.07)' },
    no_match:      { label: 'No match',       color: '#383838', bg: 'rgba(255,255,255,0.03)' },
    pending:       { label: 'Pending',        color: '#F59E0B', bg: 'rgba(245,158,11,0.07)' },
  }
  const s = map[outcome]
  return (
    <span style={{
      fontSize: '0.68rem', fontWeight: 600, color: s.color,
      background: s.bg, padding: '2px 7px', borderRadius: 4,
      border: `0.5px solid ${s.color}30`,
      boxShadow: 'inset 0 0.5px 0 rgba(255,255,255,0.06)',
      whiteSpace: 'nowrap',
    }}>
      {s.label}
    </span>
  )
}

// ─── Setup panel (slides up when "Connect" clicked) ───────────────────────

function SetupPanel({ connector, onClose }: { connector: Connector; onClose: () => void }) {
  const [copied, setCopied] = useState(false)

  function copyCmd() {
    navigator.clipboard.writeText(connector.setupCmd)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const steps = getSetupSteps(connector.id)

  return (
    <div style={{
      position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)',
      display: 'flex', alignItems: 'flex-end', justifyContent: 'center',
      zIndex: 100, backdropFilter: 'blur(4px)',
    }}
      onClick={e => { if (e.target === e.currentTarget) onClose() }}
    >
      <div style={{
        width: '100%', maxWidth: 680,
        background: '#0F0F0F',
        border: '0.5px solid rgba(255,255,255,0.1)',
        borderBottom: 'none',
        borderRadius: '12px 12px 0 0',
        padding: '32px 36px 48px',
        position: 'relative',
      }}>
        {/* Hairline top accent */}
        <div style={{ position: 'absolute', top: 0, left: 0, right: 0, height: 1, background: 'linear-gradient(90deg, transparent, rgba(45,212,191,0.35), transparent)', borderRadius: '12px 12px 0 0' }} />

        <button onClick={onClose} style={{ position: 'absolute', top: 18, right: 20, background: 'transparent', color: '#484848', fontSize: '1.2rem', lineHeight: 1 }}>×</button>

        {/* Connector identity */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 28 }}>
          <div style={{ width: 44, height: 44, borderRadius: 10, background: connector.iconBg, border: '0.5px solid rgba(255,255,255,0.08)', overflow: 'hidden', flexShrink: 0 }}>
            <BrandLogo domain={connector.logoDomain} fallback={connector.iconFallback} size={44} />
          </div>
          <div>
            <div style={{ fontFamily: 'var(--font-serif)', fontSize: '1.4rem', fontWeight: 400, color: '#DCDCDC', letterSpacing: '-0.01em' }}>Connect {connector.name}</div>
            <div style={{ fontSize: '0.82rem', color: '#525252', marginTop: 3 }}>{connector.description}</div>
          </div>
        </div>

        {/* Steps */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20, marginBottom: 28 }}>
          {steps.map((step, i) => (
            <div key={i} style={{ display: 'flex', gap: 14 }}>
              <div style={{ width: 24, height: 24, borderRadius: '50%', background: 'rgba(45,212,191,0.08)', border: '0.5px solid rgba(45,212,191,0.2)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, fontSize: '0.7rem', fontWeight: 700, color: '#2DD4BF' }}>
                {i + 1}
              </div>
              <div style={{ paddingTop: 3 }}>
                <div style={{ fontSize: '0.9rem', fontWeight: 500, color: '#DCDCDC', marginBottom: 4 }}>{step.title}</div>
                <div style={{ fontSize: '0.83rem', color: '#525252', lineHeight: 1.6 }}>{step.detail}</div>
                {step.link && (
                  <a href={step.link} target="_blank" rel="noopener noreferrer"
                    style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: '0.8rem', color: '#2DD4BF', marginTop: 6, textDecoration: 'none', opacity: 0.8 }}>
                    Open {connector.name} <ArrowSquareOut size={11} weight="bold" />
                  </a>
                )}
              </div>
            </div>
          ))}
        </div>

        {/* Terminal command */}
        <div style={{ marginBottom: 8 }}>
          <div style={{ fontSize: '0.72rem', color: '#484848', textTransform: 'uppercase', letterSpacing: '0.09em', fontWeight: 600, marginBottom: 10, display: 'flex', alignItems: 'center', gap: 6 }}>
            <Terminal size={11} weight="bold" /> Run in your terminal
          </div>
          <div style={{
            background: '#080808', border: '0.5px solid rgba(255,255,255,0.08)',
            borderRadius: 8, padding: '14px 16px',
            display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          }}>
            <code style={{ fontFamily: 'var(--font-mono)', fontSize: '0.9rem', color: '#2DD4BF', letterSpacing: '0.01em' }}>
              {connector.setupCmd}
            </code>
            <button
              onClick={copyCmd}
              style={{ background: 'transparent', color: copied ? '#2DD4BF' : '#484848', padding: '4px', display: 'flex', alignItems: 'center', gap: 5, fontSize: '0.75rem', transition: 'color 0.1s' }}
            >
              <Copy size={13} weight="bold" />
              {copied ? 'Copied' : 'Copy'}
            </button>
          </div>
        </div>

        <p style={{ fontSize: '0.8rem', color: '#383838', lineHeight: 1.6 }}>
          The CLI will walk you through authentication interactively. Restart <code style={{ fontFamily: 'var(--font-mono)', fontSize: '0.78rem', color: '#484848' }}>star watch</code> after connecting.
        </p>
      </div>
    </div>
  )
}

function getSetupSteps(connectorId: string): { title: string; detail: string; link?: string }[] {
  switch (connectorId) {
    case 'slack':
      return [
        { title: 'Create a Slack app', detail: 'Go to api.slack.com/apps → Create New App → From scratch. Name it "STAR Watch".', link: 'https://api.slack.com/apps' },
        { title: 'Enable Socket Mode', detail: 'Under Settings → Socket Mode, enable it. Generate an App-Level Token (xapp-…) with connections:write scope.' },
        { title: 'Add bot scopes', detail: 'Under OAuth & Permissions → Bot Token Scopes, add: channels:history, channels:read, reactions:read, files:read, chat:write, im:write.' },
        { title: 'Subscribe to events', detail: 'Under Event Subscriptions → enable and subscribe to: message.channels, reaction_added, file_shared.' },
        { title: 'Install and run', detail: 'Install the app to your workspace. Copy the Bot Token (xoxb-…) and App Token. Then run the command below.' },
      ]
    case 'salesforce':
      return [
        { title: 'Create a Connected App', detail: 'In Salesforce Setup → App Manager → New Connected App. Enable OAuth with scopes: api, refresh_token.', link: 'https://login.salesforce.com' },
        { title: 'Create PushTopics', detail: 'In the Developer Console, create PushTopics for Opportunity and Case objects. star-watch can do this automatically with admin permissions.' },
        { title: 'Get your credentials', detail: 'Copy your Consumer Key, Consumer Secret, and your org\'s instance URL (e.g. https://acme.salesforce.com).' },
        { title: 'Run the command', detail: 'Run the command below — the CLI will guide you through authentication and verify the connection.' },
      ]
    case 'github':
      return [
        { title: 'Create a personal access token', detail: 'Go to GitHub Settings → Developer Settings → Personal Access Tokens → Fine-grained token. Grant: repository read, pull_requests read.', link: 'https://github.com/settings/tokens' },
        { title: 'Select repositories', detail: 'Choose which repositories to watch for SOP-relevant events (PR merges, issue closes, review approvals).' },
        { title: 'Run the command', detail: 'Run the command below — the CLI will prompt for your token and repos.' },
      ]
    default:
      return [
        { title: 'Run the setup command', detail: 'The CLI will guide you through authentication interactively.' },
      ]
  }
}

// ─── Main page ────────────────────────────────────────────────────────────

export function Connections() {
  const [activeSetup, setActiveSetup] = useState<Connector | null>(null)
  const [activeTab,   setActiveTab]   = useState<'integrations' | 'events'>('integrations')

  const connected    = CONNECTORS.filter(c => c.status === 'connected')
  const disconnected = CONNECTORS.filter(c => c.status !== 'connected')
  const eventsToday  = EVENT_LOG.length
  const autoToday    = EVENT_LOG.filter(e => e.outcome === 'auto_complete').length
  const escalToday   = EVENT_LOG.filter(e => e.outcome === 'escalated').length

  return (
    <div style={{ padding: '48px 40px 64px', position: 'relative', zIndex: 1 }}>

      {/* ── Header ── */}
      <div className="fade-up fade-up-1" style={{ marginBottom: '40px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 }}>
          <div style={{ width: 32, height: 32, borderRadius: 8, background: 'rgba(45,212,191,0.08)', border: '1px solid rgba(45,212,191,0.18)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Plug size={16} color="#2DD4BF" weight="bold" />
          </div>
          <h1 style={{ fontFamily: 'var(--font-serif)', fontSize: '2.4rem', fontWeight: 400, color: '#DCDCDC', letterSpacing: '-0.01em', lineHeight: 1.15 }}>
            Connections
          </h1>
        </div>
        <p style={{ fontSize: '1rem', color: '#636363', lineHeight: 1.65, maxWidth: 520 }}>
          Connect your existing tools. STAR Watch observes activity in the background and surfaces SOP intelligence without changing how your team works.
        </p>
      </div>

      {/* ── Summary stats ── */}
      <div className="fade-up fade-up-2 panel-card" style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '1px', background: 'rgba(255,255,255,0.06)', borderRadius: 10, overflow: 'hidden', marginBottom: '36px' }}>
        {[
          { label: 'Connected',      value: connected.length,    sub: `of ${CONNECTORS.length} integrations`, color: '#2DD4BF' },
          { label: 'Events today',   value: eventsToday,         sub: 'across all connectors',                color: '#E0E0E0' },
          { label: 'Auto-completed', value: autoToday,           sub: 'no human action needed',              color: '#2DD4BF' },
          { label: 'Escalated',      value: escalToday,          sub: 'sent to assigned avatar',             color: '#60A5FA' },
        ].map(s => (
          <div key={s.label} style={{ background: '#0A0A0A', padding: '20px 24px' }}>
            <div style={{ fontSize: '0.68rem', color: '#484848', textTransform: 'uppercase', letterSpacing: '0.1em', fontWeight: 600, marginBottom: 10 }}>{s.label}</div>
            <div style={{ fontFamily: 'var(--font-serif)', fontSize: '2.2rem', fontWeight: 400, color: s.color, letterSpacing: '-0.02em', lineHeight: 1, marginBottom: 6 }}>{s.value}</div>
            <div style={{ fontSize: '0.78rem', color: '#484848' }}>{s.sub}</div>
          </div>
        ))}
      </div>

      {/* ── Tabs ── */}
      <div className="fade-up fade-up-3" style={{ display: 'flex', gap: 2, marginBottom: '28px', borderBottom: '0.5px solid rgba(255,255,255,0.07)', paddingBottom: 0 }}>
        {(['integrations', 'events'] as const).map(tab => (
          <button key={tab} onClick={() => setActiveTab(tab)} style={{
            background: 'transparent',
            color: activeTab === tab ? '#DCDCDC' : '#525252',
            fontFamily: 'var(--font-sans)', fontWeight: 500, fontSize: '0.875rem',
            padding: '8px 16px',
            borderBottom: `2px solid ${activeTab === tab ? '#2DD4BF' : 'transparent'}`,
            borderRadius: 0, marginBottom: -1,
            transition: 'color 0.1s, border-color 0.1s',
            textTransform: 'capitalize',
          }}>
            {tab === 'integrations' ? 'Integrations' : 'Event log'}
          </button>
        ))}
      </div>

      {activeTab === 'integrations' && (
        <div className="fade-up fade-up-4">
          {/* Connected */}
          {connected.length > 0 && (
            <div style={{ marginBottom: '32px' }}>
              <h2 style={{ fontFamily: 'var(--font-serif)', fontSize: '1.2rem', fontWeight: 400, color: '#DCDCDC', marginBottom: 16 }}>Active</h2>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 12 }}>
                {connected.map(c => <ConnectorCard key={c.id} c={c} onSetup={setActiveSetup} />)}
              </div>
            </div>
          )}

          {/* Available to connect */}
          <div>
            <h2 style={{ fontFamily: 'var(--font-serif)', fontSize: '1.2rem', fontWeight: 400, color: '#DCDCDC', marginBottom: 4 }}>Available</h2>
            <p style={{ fontSize: '0.875rem', color: '#484848', marginBottom: 16 }}>Phase 1 connectors are ready now. Phase 2–3 ship on the roadmap.</p>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 12 }}>
              {disconnected.map(c => <ConnectorCard key={c.id} c={c} onSetup={setActiveSetup} />)}
            </div>
          </div>

          {/* star-watch daemon status */}
          <div className="panel-card" style={{ marginTop: 32, padding: '20px 24px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 20 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <div style={{ width: 32, height: 32, borderRadius: 8, background: 'rgba(45,212,191,0.06)', border: '0.5px solid rgba(45,212,191,0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Terminal size={15} color="#2DD4BF" weight="bold" />
              </div>
              <div>
                <div style={{ fontFamily: 'var(--font-sans)', fontWeight: 600, fontSize: '0.9rem', color: '#DCDCDC', marginBottom: 3 }}>STAR Watch daemon</div>
                <div style={{ fontSize: '0.82rem', color: '#484848' }}>Run this once on your machine or server to activate background observation.</div>
              </div>
            </div>
            <div style={{ background: '#080808', border: '0.5px solid rgba(255,255,255,0.08)', borderRadius: 8, padding: '10px 16px', display: 'flex', alignItems: 'center', gap: 10, flexShrink: 0 }}>
              <Lightning size={12} color="#2DD4BF" weight="fill" />
              <code style={{ fontFamily: 'var(--font-mono)', fontSize: '0.85rem', color: '#2DD4BF' }}>star watch</code>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'events' && (
        <div className="fade-up fade-up-4">
          <div className="panel-card" style={{ overflow: 'hidden' }}>
            {/* Table header */}
            <div style={{ display: 'grid', gridTemplateColumns: '60px 100px 1fr 200px 130px', padding: '10px 20px', borderBottom: '0.5px solid rgba(255,255,255,0.06)', background: 'rgba(0,0,0,0.2)' }}>
              {['Time', 'Source', 'Event', 'Matched step', 'Outcome'].map(h => (
                <span key={h} style={{ fontSize: '0.68rem', color: '#484848', textTransform: 'uppercase', letterSpacing: '0.09em', fontWeight: 600 }}>{h}</span>
              ))}
            </div>

            {EVENT_LOG.map((e, i) => (
              <div key={e.id} style={{
                display: 'grid', gridTemplateColumns: '60px 100px 1fr 200px 130px',
                padding: '0 20px', alignItems: 'center', height: 52,
                borderBottom: i < EVENT_LOG.length - 1 ? '0.5px solid rgba(255,255,255,0.04)' : 'none',
                transition: 'background 0.1s',
              }}
                onMouseEnter={el => (el.currentTarget as HTMLDivElement).style.background = 'rgba(255,255,255,0.02)'}
                onMouseLeave={el => (el.currentTarget as HTMLDivElement).style.background = 'transparent'}
              >
                <span style={{ fontSize: '0.8rem', color: '#484848', fontFamily: 'var(--font-mono)' }}>{e.time}</span>
                <span style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: '0.82rem', color: '#686868' }}>
                  <span style={{ width: 16, height: 16, borderRadius: 4, background: 'rgba(255,255,255,0.04)', border: '0.5px solid rgba(255,255,255,0.07)', overflow: 'hidden', flexShrink: 0, display: 'inline-block' }}>
                    <BrandLogo domain={`${e.connector.toLowerCase()}.com`} fallback={e.connector[0]} size={16} />
                  </span>
                  {e.connector}
                </span>
                <span style={{ fontSize: '0.85rem', color: '#888', fontFamily: 'var(--font-mono)', fontSize: '0.78rem' as never }}>{e.action.replace(/_/g, ' ')}</span>
                <span style={{ fontSize: '0.85rem', color: e.matched ? '#C0C0C0' : '#383838', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', paddingRight: 12 }}>
                  {e.matched && e.stepName ? (
                    <span style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
                      <CheckCircle size={11} color="#2DD4BF" weight="fill" />
                      {e.stepName}
                    </span>
                  ) : (
                    <span style={{ display: 'flex', alignItems: 'center', gap: 5, color: '#383838' }}>
                      <Circle size={11} color="#2E2E2E" />
                      No match
                    </span>
                  )}
                </span>
                <OutcomeChip outcome={e.outcome} />
              </div>
            ))}

            <div style={{ padding: '14px 20px', borderTop: '0.5px solid rgba(255,255,255,0.04)', display: 'flex', alignItems: 'center', gap: 6 }}>
              <ChartBar size={12} color="#484848" />
              <span style={{ fontSize: '0.78rem', color: '#484848' }}>
                {autoToday} auto-completed · {escalToday} escalated · {EVENT_LOG.filter(e => e.outcome === 'no_match').length} no match · Match rate {Math.round((autoToday + escalToday) / eventsToday * 100)}%
              </span>
            </div>
          </div>
        </div>
      )}

      {/* ── Setup panel (modal) ── */}
      {activeSetup && (
        <SetupPanel connector={activeSetup} onClose={() => setActiveSetup(null)} />
      )}
    </div>
  )
}
