import { useState } from 'react'
import { CheckCircle, Warning, Sparkle, ThumbsUp, ThumbsDown, ArrowSquareOut } from '@phosphor-icons/react'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell, CartesianGrid, LineChart, Line } from 'recharts'
import { StatCard, Card, CardHeader } from '../components/Card'
import { Badge } from '../components/Badge'

const SOPS = ['Enterprise Customer Onboarding v3','Container Gate-In & Customs Clearance','New Employee Onboarding','Investment Due Diligence','Monthly ESG Impact Report']

const STATS = [
  { label: 'Completion Rate', value: '87%', sub: '94 of 108 runs',     trend: { direction: 'up'   as const, label: '+4% vs last month' } },
  { label: 'Avg Duration',    value: '42m', sub: 'vs 60 min estimated', trend: { direction: 'up'   as const, label: '30% faster' } },
  { label: 'Deviations / Run',value: '1.2', sub: '23 total · 8 open',  trend: { direction: 'down' as const, label: '−0.3 vs avg' } },
  { label: 'AI Improvements', value: '5',   sub: 'Pending review' },
]

const STEPS = [
  { step: 'Welcome Email',    deviations: 2,  avgDuration: 5  },
  { step: 'Discovery Call',   deviations: 8,  avgDuration: 52 },
  { step: 'Branch',           deviations: 1,  avgDuration: 2  },
  { step: 'Setup Guide',      deviations: 14, avgDuration: 78 },
  { step: 'Activation Check', deviations: 11, avgDuration: 95 },
  { step: 'Go-Live Sign-Off', deviations: 4,  avgDuration: 34 },
  { step: 'CRM Update',       deviations: 0,  avgDuration: 1  },
]

const AVATARS = [
  { name: 'Kelly A.',  runs: 34, completion: '94%', avgDuration: '39m', deviations: 2, role: 'CS Manager'     },
  { name: 'Max G.',    runs: 21, completion: '71%', avgDuration: '58m', deviations: 7, role: 'Solutions Eng.' },
  { name: 'Jordan T.', runs: 18, completion: '89%', avgDuration: '43m', deviations: 3, role: 'Onboarding'     },
  { name: 'System',    runs: 47, completion: '100%',avgDuration: '6m',  deviations: 0, role: 'Automation'     },
]

const AI_QUEUE = [
  { id: 1, step: 'Setup Guide',     suggestion: 'This step has a 58% deviation rate for Enterprise customers. BRAID recommends splitting into two steps: "Send guide" and "Confirm receipt (24h check-in)".', impact: 'High',   accepted: null },
  { id: 2, step: 'Activation Check',suggestion: 'Average duration is 95 min — 60% over estimate. BRAID suggests automated Zendesk check-in at 48h to flag at-risk accounts before the 7-day window.',              impact: 'High',   accepted: null },
  { id: 3, step: 'Discovery Call',  suggestion: 'BRAID detected 3 runs where the complexity branch was manually overridden. Recommend adding a scoring rubric (1–5) to reduce subjectivity.',                       impact: 'Medium', accepted: null },
]

const RUNS = [
  { id: 'r1', avatar: 'Kelly A.',  status: 'completed', duration: '38m',   deviations: 0, started: 'Mar 24', proof: '0x1a2b…3c4d' },
  { id: 'r2', avatar: 'Max G.',    status: 'deviation',  duration: '3d 1h', deviations: 2, started: 'Mar 22', proof: '0x5e6f…7a8b' },
  { id: 'r3', avatar: 'System',    status: 'completed', duration: '44m',   deviations: 0, started: 'Mar 21', proof: '0x9c0d…1e2f' },
  { id: 'r4', avatar: 'Jordan T.', status: 'completed', duration: '52m',   deviations: 1, started: 'Mar 20', proof: '0x3a4b…5c6d' },
]

const TREND = [{ week: 'W9', rate: 78 }, { week: 'W10', rate: 81 }, { week: 'W11', rate: 80 }, { week: 'W12', rate: 85 }, { week: 'W13', rate: 87 }]

const TT = { contentStyle: { background: '#181818', border: '1px solid #2C2C2C', borderRadius: 5, fontSize: 12, color: '#D4D4D4' }, labelStyle: { color: '#707070' }, cursor: { fill: 'rgba(255,255,255,0.03)' } }
const TH = { padding: '8px 18px', textAlign: 'left' as const, fontSize: '0.63rem', color: '#454545', textTransform: 'uppercase' as const, letterSpacing: '0.08em', fontWeight: 600 }
const TD = { padding: '11px 18px', fontSize: '0.82rem' }

export function SOPIntel() {
  const [sop, setSop] = useState(SOPS[0])
  const [queue, setQueue] = useState(AI_QUEUE)
  const maxDev = Math.max(...STEPS.map(s => s.deviations))

  return (
    <div style={{ padding: '32px 36px 48px' }}>

      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', marginBottom: '32px' }}>
        <div>
                      <h1 style={{ fontFamily: "'Instrument Serif', Georgia, serif", fontSize: '2.4rem', fontWeight: 400, color: '#DCDCDC', letterSpacing: '-0.01em', lineHeight: 1.15, marginBottom: 8 }}>SOPIntel</h1>
          <p style={{ fontSize: '0.95rem', color: '#636363' }}>Deviation heatmaps, team performance, and AI improvement suggestions.</p>
        </div>
        <select value={sop} onChange={e => setSop(e.target.value)}
          style={{ width: 'auto', padding: '6px 10px', fontSize: '0.8rem', cursor: 'pointer', background: '#181818', border: '1px solid #2C2C2C', color: '#C4C4C4', borderRadius: 5 }}>
          {SOPS.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '28px', padding: '20px 22px', background: '#111111', border: '1px solid #1F1F1F', borderRadius: '7px', marginBottom: '14px' }}>
        {STATS.map(s => <StatCard key={s.label} {...s} />)}
      </div>

      {/* Trend + Heatmap */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px', marginBottom: '14px' }}>
        <Card>
          <CardHeader title="Completion Trend" sub="Weekly rate" />
          <div style={{ padding: '16px 18px' }}>
            <ResponsiveContainer width="100%" height={140}>
              <LineChart data={TREND}>
                <CartesianGrid strokeDasharray="3 3" stroke="#1A1A1A" />
                <XAxis dataKey="week" tick={{ fill: '#454545', fontSize: 11 }} axisLine={false} tickLine={false} />
                <YAxis domain={[70, 100]} tick={{ fill: '#454545', fontSize: 11 }} axisLine={false} tickLine={false} />
                <Tooltip {...TT} />
                <Line type="monotone" dataKey="rate" stroke="#22C55E" strokeWidth={1.5} dot={{ fill: '#22C55E', r: 2.5 }} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </Card>

        <Card>
          <CardHeader title="Step Deviations" sub="Darker = more deviations" />
          <div style={{ padding: '16px 18px', display: 'flex', flexDirection: 'column', gap: 5 }}>
            {STEPS.map(s => {
              const t = maxDev > 0 ? s.deviations / maxDev : 0
              return (
                <div key={s.step} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <span style={{ fontSize: '0.72rem', color: '#606060', width: 110, flexShrink: 0, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{s.step}</span>
                  <div style={{ flex: 1, height: 16, background: '#181818', borderRadius: 3, overflow: 'hidden', position: 'relative' }}>
                    <div style={{ position: 'absolute', inset: 0, width: `${t * 100}%`, background: t > 0.6 ? '#EF4444' : t > 0.3 ? '#F59E0B' : '#2C2C2C', borderRadius: 3, transition: 'width 0.4s' }} />
                  </div>
                  <span style={{ fontSize: '0.68rem', fontFamily: "'JetBrains Mono', monospace", color: '#505050', width: 14, textAlign: 'right' }}>{s.deviations}</span>
                </div>
              )
            })}
          </div>
        </Card>
      </div>

      {/* Duration chart */}
      <Card style={{ marginBottom: '14px' }}>
        <CardHeader title="Step Duration" sub="Average minutes per step" right={<span style={{ fontSize: '0.7rem', color: '#454545' }}>Estimate: 60 min</span>} />
        <div style={{ padding: '16px 18px' }}>
          <ResponsiveContainer width="100%" height={155}>
            <BarChart data={STEPS} barSize={16}>
              <CartesianGrid strokeDasharray="3 3" stroke="#1A1A1A" vertical={false} />
              <XAxis dataKey="step" tick={{ fill: '#454545', fontSize: 10 }} axisLine={false} tickLine={false} interval={0} angle={-12} textAnchor="end" height={32} />
              <YAxis tick={{ fill: '#454545', fontSize: 11 }} axisLine={false} tickLine={false} />
              <Tooltip {...TT} formatter={(v: number) => [`${v} min`, 'Duration']} />
              <Bar dataKey="avgDuration" radius={[3, 3, 0, 0]}>
                {STEPS.map((s, i) => <Cell key={i} fill={s.avgDuration > 60 ? '#F59E0B' : '#2C2C2C'} />)}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      </Card>

      {/* Avatar performance */}
      <Card style={{ marginBottom: '14px' }}>
        <CardHeader title="Avatar Performance" sub="Individual and automated run statistics" />
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr style={{ borderBottom: '1px solid #1A1A1A' }}>
            {['Avatar', 'Role', 'Runs', 'Completion', 'Avg Duration', 'Deviations'].map(h => <th key={h} style={TH}>{h}</th>)}
          </tr></thead>
          <tbody>
            {AVATARS.map(av => (
              <tr key={av.name} style={{ borderBottom: '1px solid #181818', transition: 'background 0.1s' }}
                onMouseEnter={e => e.currentTarget.style.background = '#161616'}
                onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>
                <td style={TD}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <div style={{ width: 22, height: 22, borderRadius: '50%', background: '#1A1A1A', border: '1px solid #2C2C2C', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '0.55rem', fontWeight: 700, color: '#666', flexShrink: 0 }}>
                      {av.name.split(' ').map(p => p[0]).join('')}
                    </div>
                    <span style={{ color: '#C4C4C4', fontWeight: 500 }}>{av.name}</span>
                  </div>
                </td>
                <td style={{ ...TD, color: '#606060' }}>{av.role}</td>
                <td style={{ ...TD, color: '#808080', fontFamily: "'JetBrains Mono', monospace" }}>{av.runs}</td>
                <td style={TD}><span style={{ fontWeight: 600, color: parseFloat(av.completion) >= 90 ? '#22C55E' : parseFloat(av.completion) >= 75 ? '#808080' : '#EF4444', fontFamily: "'JetBrains Mono', monospace" }}>{av.completion}</span></td>
                <td style={{ ...TD, color: '#606060', fontFamily: "'JetBrains Mono', monospace" }}>{av.avgDuration}</td>
                <td style={TD}><span style={{ fontWeight: 600, color: av.deviations === 0 ? '#22C55E' : av.deviations > 5 ? '#EF4444' : '#F59E0B', fontFamily: "'JetBrains Mono', monospace" }}>{av.deviations}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      {/* AI Improvement Queue */}
      <Card style={{ marginBottom: '14px' }}>
        <CardHeader title="AI Improvement Queue" sub={`${queue.filter(i => i.accepted === null).length} pending — generated by BRAID deviation analysis`} />
        <div style={{ padding: '0' }}>
          {queue.map((item, i) => (
            <div key={item.id} style={{ display: 'flex', gap: 14, padding: '14px 18px', borderBottom: i < queue.length - 1 ? '1px solid #181818' : 'none', background: item.accepted !== null ? 'transparent' : '#111111', opacity: item.accepted !== null ? 0.45 : 1, transition: 'opacity 0.2s' }}>
              <div style={{ width: 26, height: 26, borderRadius: 5, background: 'rgba(139,92,246,0.08)', border: '1px solid rgba(139,92,246,0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, marginTop: 1 }}>
                <Sparkle size={12} weight="fill" color="#8B5CF6" />
              </div>
              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 7, marginBottom: 4 }}>
                  <span style={{ fontSize: '0.82rem', fontWeight: 600, color: '#C4C4C4' }}>{item.step}</span>
                  <Badge variant={item.impact === 'High' ? 'warning' : 'default'}>{item.impact} impact</Badge>
                  {item.accepted === true  && <Badge variant="success">Accepted</Badge>}
                  {item.accepted === false && <Badge>Dismissed</Badge>}
                </div>
                <p style={{ fontSize: '0.8rem', color: '#666666', lineHeight: 1.65 }}>{item.suggestion}</p>
              </div>
              {item.accepted === null && (
                <div style={{ display: 'flex', gap: 6, flexShrink: 0, alignItems: 'flex-start', paddingTop: 2 }}>
                  <button onClick={() => setQueue(q => q.map(x => x.id === item.id ? { ...x, accepted: true } : x))} className="btn-ghost" style={{ display: 'flex', alignItems: 'center', gap: 4, color: '#22C55E', borderColor: 'rgba(34,197,94,0.2)', background: 'rgba(34,197,94,0.06)' }}>
                    <ThumbsUp size={10} /> Accept
                  </button>
                  <button onClick={() => setQueue(q => q.map(x => x.id === item.id ? { ...x, accepted: false } : x))} className="btn-ghost" style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                    <ThumbsDown size={10} /> Dismiss
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      </Card>

      {/* Run History */}
      <Card>
        <CardHeader title="Run History" sub="All executions with immutable proof holon references" />
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead><tr style={{ borderBottom: '1px solid #1A1A1A' }}>
            {['Started', 'Executed by', 'Status', 'Duration', 'Deviations', 'Proof Holon'].map(h => <th key={h} style={TH}>{h}</th>)}
          </tr></thead>
          <tbody>
            {RUNS.map(run => (
              <tr key={run.id} style={{ borderBottom: '1px solid #181818', cursor: 'pointer', transition: 'background 0.1s' }}
                onMouseEnter={e => e.currentTarget.style.background = '#161616'}
                onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>
                <td style={{ ...TD, color: '#606060' }}>{run.started}</td>
                <td style={{ ...TD, color: '#C4C4C4', fontWeight: 500 }}>{run.avatar}</td>
                <td style={TD}>
                  {run.status === 'completed'
                    ? <span style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: '0.77rem', color: '#22C55E', fontWeight: 500 }}><CheckCircle size={11} weight="fill" />Completed</span>
                    : <span style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: '0.77rem', color: '#F59E0B', fontWeight: 500 }}><Warning size={11} weight="fill" />Deviation</span>
                  }
                </td>
                <td style={{ ...TD, color: '#606060', fontFamily: "'JetBrains Mono', monospace" }}>{run.duration}</td>
                <td style={TD}><span style={{ fontWeight: 600, color: run.deviations === 0 ? '#22C55E' : '#F59E0B', fontFamily: "'JetBrains Mono', monospace" }}>{run.deviations}</span></td>
                <td style={TD}>
                  <a href={`#proof/${run.proof}`} style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: '0.72rem', fontFamily: "'JetBrains Mono', monospace", color: '#454545', textDecoration: 'none' }}
                    onMouseEnter={e => (e.currentTarget.style.color = '#707070')}
                    onMouseLeave={e => (e.currentTarget.style.color = '#454545')}>
                    {run.proof}<ArrowSquareOut size={9} weight="bold" />
                  </a>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  )
}
