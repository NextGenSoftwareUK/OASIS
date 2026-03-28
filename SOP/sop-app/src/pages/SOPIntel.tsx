import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  BarChart2, CheckCircle2, AlertTriangle, Sparkles, ChevronRight,
  ExternalLink, ThumbsUp, ThumbsDown, TrendingUp, Clock, Users, Activity
} from 'lucide-react'
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer,
  Cell, CartesianGrid
} from 'recharts'
import { StatCard } from '../components/Card'
import { Badge } from '../components/Badge'

// ─── Demo data ───────────────────────────────────────────────────────────────

const SOPS = [
  'Enterprise Customer Onboarding v3',
  'Container Gate-In & Customs Clearance',
  'New Employee Onboarding',
  'Investment Due Diligence',
  'Monthly ESG Impact Report',
]

const STATS = [
  { label: 'Completion Rate', value: '87%', sub: '94 of 108 runs completed', color: '#22c55e' },
  { label: 'Avg Duration', value: '42m', sub: 'vs 60m estimated', color: '#6366f1' },
  { label: 'Deviations / Run', value: '1.2', sub: '23 total · 8 unresolved', color: '#f59e0b' },
  { label: 'AI Improvements', value: '5', sub: 'Pending your review', color: '#a855f7' },
]

const STEP_DEVIATIONS = [
  { step: 'Welcome Email', deviations: 2, avgDuration: 5, color: '#22c55e' },
  { step: 'Discovery Call', deviations: 8, avgDuration: 52, color: '#f59e0b' },
  { step: 'Complexity Branch', deviations: 1, avgDuration: 2, color: '#22c55e' },
  { step: 'Setup Guide', deviations: 14, avgDuration: 78, color: '#ef4444' },
  { step: 'Activation Check', deviations: 11, avgDuration: 95, color: '#ef4444' },
  { step: 'Go-Live Sign-Off', deviations: 4, avgDuration: 34, color: '#f59e0b' },
  { step: 'CRM Update', deviations: 0, avgDuration: 1, color: '#22c55e' },
]

const AVATARS = [
  { name: 'Kelly A.', role: 'CustomerSuccessManager', completions: 34, deviations: 3, rate: '91%' },
  { name: 'Max G.', role: 'SolutionsEngineer', completions: 28, deviations: 7, rate: '79%' },
  { name: 'Jordan T.', role: 'CustomerSuccessManager', completions: 22, deviations: 2, rate: '94%' },
  { name: 'Sam R.', role: 'SolutionsEngineer', completions: 18, deviations: 5, rate: '83%' },
  { name: 'System', role: 'Automated', completions: 108, deviations: 1, rate: '99%' },
]

const AI_IMPROVEMENTS = [
  {
    id: 'imp-1',
    step: 'Setup Guide + Zendesk Ticket',
    issue: 'Step 4 has a 34% deviation rate due to timeout. Avg duration is 78 min vs 60 min timeout.',
    suggestion: 'Increase TimeoutMinutes from 60 to 120. Add sub-step checklist to ConnectorConfig to break into smaller verifiable actions.',
    severity: 'High',
    status: 'pending',
  },
  {
    id: 'imp-2',
    step: 'Activation Check-In (Day 7)',
    issue: 'Step 5 has 11 deviations — 8 are MissingEvidence type. Executors are completing without uploading usage data screenshot.',
    suggestion: 'Set RequiresEvidence to true. Update AIPrompt to explicitly request product usage rate as a numeric input before completion.',
    severity: 'High',
    status: 'pending',
  },
  {
    id: 'imp-3',
    step: 'Technical Discovery Call',
    issue: 'Step 2 has 8 deviations — 6 are Timeout type. Calls are running long.',
    suggestion: 'Add a structured discovery template as ConnectorConfig.questionnaireUrl. This reduces call time by giving the SE a pre-filled agenda.',
    severity: 'Medium',
    status: 'pending',
  },
  {
    id: 'imp-4',
    step: 'Go-Live Sign-Off',
    issue: '3 out of 4 deviations are delayed sign-off — customer admin not responding to wallet sign request.',
    suggestion: 'Add a fallback branch: if avatar_wallet sign-off is not completed within 24h, automatically switch to docusign path.',
    severity: 'Medium',
    status: 'pending',
  },
  {
    id: 'imp-5',
    step: 'SOP Overall',
    issue: 'Runs where AI guidance was used have 31% lower deviation rate than runs without.',
    suggestion: 'Add RequiresAIGuidance flag to Steps 2, 4, and 5. Prompt runner UI to open co-pilot before those steps can be started.',
    severity: 'Low',
    status: 'pending',
  },
]

const RUNS = [
  { id: 'r-001', date: '2026-03-28', status: 'completed', duration: '47m', avatars: 3, deviations: 0, proof: 'ph-a1b2' },
  { id: 'r-002', date: '2026-03-27', status: 'completed', duration: '1h 12m', avatars: 2, deviations: 2, proof: 'ph-c3d4' },
  { id: 'r-003', date: '2026-03-26', status: 'running', duration: '2h 14m', avatars: 3, deviations: 1, proof: null },
  { id: 'r-004', date: '2026-03-25', status: 'completed', duration: '38m', avatars: 2, deviations: 0, proof: 'ph-e5f6' },
  { id: 'r-005', date: '2026-03-24', status: 'failed', duration: '4h 02m', avatars: 4, deviations: 5, proof: null },
  { id: 'r-006', date: '2026-03-23', status: 'completed', duration: '52m', avatars: 2, deviations: 1, proof: 'ph-g7h8' },
]

// ─── Custom tooltip ──────────────────────────────────────────────────────────

function CustomTooltip({ active, payload, label }: { active?: boolean; payload?: Array<{ value: number; name: string }>; label?: string }) {
  if (!active || !payload?.length) return null
  return (
    <div style={{ background: '#18181f', border: '1px solid #2a2a38', borderRadius: 6, padding: '10px 14px', fontSize: '0.78rem' }}>
      <p style={{ color: '#e2e2f0', fontWeight: 600, marginBottom: 4 }}>{label}</p>
      {payload.map(p => (
        <p key={p.name} style={{ color: '#9090a8' }}>{p.name}: <span style={{ color: '#e2e2f0' }}>{p.value}</span></p>
      ))}
    </div>
  )
}

// ─── Component ───────────────────────────────────────────────────────────────

export function SOPIntel() {
  const navigate = useNavigate()
  const [selectedSOP, setSelectedSOP] = useState(0)
  const [improvements, setImprovements] = useState(AI_IMPROVEMENTS)
  const [activeTab, setActiveTab] = useState<'deviations' | 'duration'>('deviations')

  function handleImprovement(id: string, action: 'approve' | 'reject') {
    setImprovements(prev => prev.map(i => i.id === id ? { ...i, status: action === 'approve' ? 'approved' : 'rejected' } : i))
  }

  const pendingCount = improvements.filter(i => i.status === 'pending').length

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '32px 24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', marginBottom: '28px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '6px' }}>
            <BarChart2 size={18} color="#22c55e" />
            <h1 style={{ fontSize: '1.3rem', fontWeight: 700, color: '#e2e2f0' }}>SOPIntel</h1>
          </div>
          <p style={{ color: '#9090a8', fontSize: '0.85rem' }}>Analytics, deviation heatmaps, and AI-proposed improvements</p>
        </div>
        <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
          <select
            value={selectedSOP}
            onChange={e => setSelectedSOP(Number(e.target.value))}
            style={{ width: 'auto', minWidth: 260 }}
          >
            {SOPS.map((s, i) => <option key={i} value={i}>{s}</option>)}
          </select>
          <button
            onClick={() => navigate('/runner')}
            style={{ background: '#6366f1', color: '#fff', padding: '8px 16px', borderRadius: '6px', fontWeight: 600, fontSize: '0.82rem', display: 'flex', alignItems: 'center', gap: '6px', whiteSpace: 'nowrap' }}
          >
            <Activity size={13} /> Run SOP
          </button>
        </div>
      </div>

      {/* KPI cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '12px', marginBottom: '28px' }}>
        {STATS.map(s => <StatCard key={s.label} {...s} />)}
      </div>

      {/* Heatmap + Avatar table row */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 380px', gap: '16px', marginBottom: '20px' }}>

        {/* Heatmap */}
        <div style={{ background: '#111118', border: '1px solid #1e1e2a', borderRadius: '10px', padding: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
            <div>
              <h2 style={{ fontSize: '0.9rem', fontWeight: 600, color: '#e2e2f0', marginBottom: '2px' }}>Step Analysis</h2>
              <p style={{ fontSize: '0.75rem', color: '#9090a8' }}>Deviations and avg duration per step</p>
            </div>
            <div style={{ display: 'flex', gap: '6px' }}>
              {(['deviations', 'duration'] as const).map(tab => (
                <button
                  key={tab}
                  onClick={() => setActiveTab(tab)}
                  style={{
                    padding: '5px 12px', borderRadius: '5px', fontSize: '0.75rem', fontWeight: 600,
                    background: activeTab === tab ? '#1e1e2a' : 'transparent',
                    color: activeTab === tab ? '#e2e2f0' : '#5a5a72',
                    border: activeTab === tab ? '1px solid #2a2a38' : '1px solid transparent',
                  }}
                >
                  {tab === 'deviations' ? 'Deviations' : 'Avg Duration'}
                </button>
              ))}
            </div>
          </div>

          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={STEP_DEVIATIONS} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#1e1e2a" />
              <XAxis dataKey="step" tick={{ fill: '#5a5a72', fontSize: 11 }} tickLine={false} axisLine={false} />
              <YAxis tick={{ fill: '#5a5a72', fontSize: 11 }} tickLine={false} axisLine={false} />
              <Tooltip content={<CustomTooltip />} />
              <Bar
                dataKey={activeTab === 'deviations' ? 'deviations' : 'avgDuration'}
                name={activeTab === 'deviations' ? 'Deviations' : 'Avg Duration (min)'}
                radius={[4, 4, 0, 0]}
              >
                {STEP_DEVIATIONS.map((entry, index) => (
                  <Cell key={index} fill={entry.color} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>

          {/* Legend */}
          <div style={{ display: 'flex', gap: '16px', marginTop: '12px', justifyContent: 'center' }}>
            {[['#22c55e', 'Low (0–3)'], ['#f59e0b', 'Medium (4–10)'], ['#ef4444', 'High (11+)']].map(([color, label]) => (
              <div key={label} style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.72rem', color: '#9090a8' }}>
                <div style={{ width: 10, height: 10, borderRadius: 2, background: color }} />
                {label}
              </div>
            ))}
          </div>
        </div>

        {/* Avatar performance */}
        <div style={{ background: '#111118', border: '1px solid #1e1e2a', borderRadius: '10px', padding: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '16px' }}>
            <Users size={14} color="#6366f1" />
            <h2 style={{ fontSize: '0.9rem', fontWeight: 600, color: '#e2e2f0' }}>Avatar Performance</h2>
          </div>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid #1e1e2a' }}>
                {['Name', 'Steps', 'Dev', 'Rate'].map(h => (
                  <th key={h} style={{ padding: '6px 8px', textAlign: 'left', fontSize: '0.65rem', color: '#5a5a72', textTransform: 'uppercase', letterSpacing: '0.07em', fontWeight: 600 }}>
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {AVATARS.map(a => (
                <tr key={a.name} style={{ borderBottom: '1px solid #1e1e2a' }}>
                  <td style={{ padding: '10px 8px' }}>
                    <div style={{ fontSize: '0.82rem', color: '#e2e2f0', fontWeight: 500 }}>{a.name}</div>
                    <div style={{ fontSize: '0.68rem', color: '#5a5a72' }}>{a.role}</div>
                  </td>
                  <td style={{ padding: '10px 8px', fontSize: '0.8rem', color: '#9090a8', fontFamily: 'JetBrains Mono, monospace' }}>{a.completions}</td>
                  <td style={{ padding: '10px 8px' }}>
                    <Badge variant={a.deviations === 0 ? 'success' : a.deviations <= 3 ? 'warning' : 'error'}>
                      {a.deviations}
                    </Badge>
                  </td>
                  <td style={{ padding: '10px 8px' }}>
                    <span style={{ fontSize: '0.82rem', color: parseInt(a.rate) >= 90 ? '#22c55e' : parseInt(a.rate) >= 80 ? '#f59e0b' : '#ef4444', fontWeight: 600 }}>
                      {a.rate}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* AI Improvement Queue + Run History row */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>

        {/* AI improvements */}
        <div style={{ background: '#111118', border: '1px solid #1e1e2a', borderRadius: '10px', padding: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '7px' }}>
              <Sparkles size={14} color="#a855f7" />
              <h2 style={{ fontSize: '0.9rem', fontWeight: 600, color: '#e2e2f0' }}>AI Improvement Queue</h2>
            </div>
            {pendingCount > 0 && <Badge variant="purple">{pendingCount} pending</Badge>}
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', maxHeight: 460, overflowY: 'auto' }}>
            {improvements.map(imp => (
              <div
                key={imp.id}
                style={{
                  background: '#18181f', border: `1px solid ${imp.status === 'approved' ? '#22c55e30' : imp.status === 'rejected' ? '#2a2a38' : '#a855f720'}`,
                  borderRadius: '8px', padding: '14px',
                  opacity: imp.status !== 'pending' ? 0.5 : 1,
                  transition: 'opacity 0.2s',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '6px' }}>
                  <span style={{ fontSize: '0.78rem', fontWeight: 600, color: '#e2e2f0' }}>{imp.step}</span>
                  <Badge variant={imp.severity === 'High' ? 'error' : imp.severity === 'Medium' ? 'warning' : 'default'}>
                    {imp.severity}
                  </Badge>
                </div>
                <p style={{ fontSize: '0.76rem', color: '#9090a8', marginBottom: '6px', lineHeight: 1.5 }}>
                  <strong style={{ color: '#f59e0b' }}>Issue:</strong> {imp.issue}
                </p>
                <p style={{ fontSize: '0.76rem', color: '#c0d0c0', marginBottom: '10px', lineHeight: 1.5 }}>
                  <strong style={{ color: '#a855f7' }}>Suggestion:</strong> {imp.suggestion}
                </p>
                {imp.status === 'pending' ? (
                  <div style={{ display: 'flex', gap: '8px' }}>
                    <button
                      onClick={() => handleImprovement(imp.id, 'approve')}
                      style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '5px', background: '#22c55e15', border: '1px solid #22c55e30', color: '#22c55e', padding: '6px', borderRadius: '5px', fontSize: '0.75rem', fontWeight: 600 }}
                    >
                      <ThumbsUp size={11} /> Approve
                    </button>
                    <button
                      onClick={() => handleImprovement(imp.id, 'reject')}
                      style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '5px', background: '#18181f', border: '1px solid #2a2a38', color: '#9090a8', padding: '6px', borderRadius: '5px', fontSize: '0.75rem' }}
                    >
                      <ThumbsDown size={11} /> Reject
                    </button>
                  </div>
                ) : (
                  <Badge variant={imp.status === 'approved' ? 'success' : 'default'}>
                    {imp.status === 'approved' ? <><CheckCircle2 size={9} style={{ marginRight: 3 }} />Approved — SOPVersionHolon created</> : 'Rejected'}
                  </Badge>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Run history */}
        <div style={{ background: '#111118', border: '1px solid #1e1e2a', borderRadius: '10px', padding: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '16px' }}>
            <TrendingUp size={14} color="#6366f1" />
            <h2 style={{ fontSize: '0.9rem', fontWeight: 600, color: '#e2e2f0' }}>Run History</h2>
          </div>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid #1e1e2a' }}>
                {['Date', 'Status', 'Duration', 'Participants', 'Deviations', 'Proof'].map(h => (
                  <th key={h} style={{ padding: '6px 8px', textAlign: 'left', fontSize: '0.65rem', color: '#5a5a72', textTransform: 'uppercase', letterSpacing: '0.07em', fontWeight: 600 }}>
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {RUNS.map(run => (
                <tr
                  key={run.id}
                  style={{ borderBottom: '1px solid #1e1e2a', cursor: 'pointer', transition: 'background 0.1s' }}
                  onMouseEnter={e => e.currentTarget.style.background = '#18181f'}
                  onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
                  onClick={() => navigate(`/runner/${run.id}`)}
                >
                  <td style={{ padding: '10px 8px', fontSize: '0.78rem', color: '#9090a8', fontFamily: 'JetBrains Mono, monospace' }}>{run.date}</td>
                  <td style={{ padding: '10px 8px' }}>
                    {run.status === 'completed' && <Badge variant="success"><CheckCircle2 size={9} style={{ marginRight: 3 }} />Done</Badge>}
                    {run.status === 'running' && <Badge variant="info">Running</Badge>}
                    {run.status === 'failed' && <Badge variant="error">Failed</Badge>}
                  </td>
                  <td style={{ padding: '10px 8px' }}>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '0.75rem', color: '#9090a8', fontFamily: 'JetBrains Mono, monospace' }}>
                      <Clock size={10} />{run.duration}
                    </span>
                  </td>
                  <td style={{ padding: '10px 8px', fontSize: '0.78rem', color: '#9090a8', textAlign: 'center' }}>{run.avatars}</td>
                  <td style={{ padding: '10px 8px', textAlign: 'center' }}>
                    {run.deviations > 0
                      ? <Badge variant="warning"><AlertTriangle size={9} style={{ marginRight: 3 }} />{run.deviations}</Badge>
                      : <Badge variant="success">0</Badge>
                    }
                  </td>
                  <td style={{ padding: '10px 8px' }}>
                    {run.proof
                      ? (
                        <button
                          onClick={e => { e.stopPropagation(); window.open(`http://localhost:5001/api/workflow/verify/${run.proof}`, '_blank') }}
                          style={{ background: '#22c55e15', border: '1px solid #22c55e30', color: '#22c55e', padding: '3px 8px', borderRadius: '4px', fontSize: '0.7rem', display: 'flex', alignItems: 'center', gap: '4px' }}
                        >
                          <ExternalLink size={9} /> Verify
                        </button>
                      )
                      : <span style={{ fontSize: '0.72rem', color: '#5a5a72' }}>—</span>
                    }
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div style={{ marginTop: '12px', padding: '10px 12px', background: '#18181f', borderRadius: '6px', fontSize: '0.75rem', color: '#9090a8', display: 'flex', alignItems: 'center', gap: '6px' }}>
            <CheckCircle2 size={12} color="#22c55e" />
            Proof holons are verifiable at <code style={{ fontFamily: 'JetBrains Mono, monospace', color: '#6366f1', marginLeft: 4 }}>GET /api/workflow/verify/{'{holonId}'}</code>
          </div>

          {/* Trend mini-chart */}
          <div style={{ marginTop: '16px' }}>
            <p style={{ fontSize: '0.72rem', color: '#5a5a72', textTransform: 'uppercase', letterSpacing: '0.07em', fontWeight: 600, marginBottom: '8px' }}>
              Completion Rate — Last 30 Days
            </p>
            <ResponsiveContainer width="100%" height={80}>
              <BarChart data={[
                { day: 'W1', rate: 82 }, { day: 'W2', rate: 88 }, { day: 'W3', rate: 85 }, { day: 'W4', rate: 94 },
              ]} margin={{ top: 0, right: 0, left: -30, bottom: 0 }}>
                <XAxis dataKey="day" tick={{ fill: '#5a5a72', fontSize: 10 }} tickLine={false} axisLine={false} />
                <YAxis domain={[70, 100]} tick={{ fill: '#5a5a72', fontSize: 10 }} tickLine={false} axisLine={false} />
                <Tooltip content={<CustomTooltip />} />
                <Bar dataKey="rate" name="Completion %" fill="#6366f1" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      {/* STARNET CTA */}
      <div style={{
        marginTop: '20px', background: '#6366f110', border: '1px solid #6366f130',
        borderRadius: '10px', padding: '20px 24px',
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      }}>
        <div>
          <h3 style={{ fontSize: '0.9rem', fontWeight: 700, color: '#e2e2f0', marginBottom: '4px' }}>
            Publish this SOP to STARNET
          </h3>
          <p style={{ fontSize: '0.8rem', color: '#9090a8' }}>
            Make this SOP template discoverable and forkable by other organisations. Choose MIT (free) or Commercial (treasury-gated).
          </p>
        </div>
        <button
          style={{ background: '#6366f1', color: '#fff', padding: '10px 20px', borderRadius: '8px', fontWeight: 700, fontSize: '0.85rem', display: 'flex', alignItems: 'center', gap: '8px', whiteSpace: 'nowrap' }}
        >
          Publish to STARNET <ChevronRight size={14} />
        </button>
      </div>
    </div>
  )
}
