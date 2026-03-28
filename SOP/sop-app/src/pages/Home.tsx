import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Activity, BarChart2, Sparkles, Play, ExternalLink, Lock, CheckCircle2, AlertTriangle, Clock } from 'lucide-react'
import { listMyWorkflows, listPublicWorkflows, authenticate, getToken, type WorkflowDefinition } from '../api/client'
import { Card, StatCard } from '../components/Card'
import { Badge } from '../components/Badge'

const DEMO_STATS = [
  { label: 'Active SOPs', value: 12, sub: '8 yours · 4 forked', color: '#6366f1' },
  { label: 'Runs This Month', value: 347, sub: '94% completion rate', color: '#22c55e' },
  { label: 'Deviations Detected', value: 23, sub: '8 unresolved', color: '#f59e0b' },
  { label: 'AI Improvements Pending', value: 5, sub: 'Awaiting your review', color: '#a855f7' },
]

const RECENT_RUNS = [
  { id: 'run-1', sop: 'Enterprise Onboarding v3', step: 'Go-live sign-off', status: 'running', avatar: 'Kelly A.', elapsed: '2h 14m' },
  { id: 'run-2', sop: 'Container Gate-In & Customs', step: 'EUDR Compliance Check', status: 'completed', avatar: 'System', elapsed: '44m' },
  { id: 'run-3', sop: 'DD Process — Fund Investment', step: 'IC Vote', status: 'deviation', avatar: 'Max G.', elapsed: '3d 1h' },
  { id: 'run-4', sop: 'Monthly ESG Impact Report', step: 'AI Analysis', status: 'completed', avatar: 'System', elapsed: '18m' },
  { id: 'run-5', sop: 'New Employee Onboarding', step: 'IT Provisioning', status: 'running', avatar: 'Jordan T.', elapsed: '6h 03m' },
]

function statusBadge(status: string) {
  if (status === 'running') return <Badge variant="info"><span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}><span style={{ width: 6, height: 6, borderRadius: '50%', background: '#818cf8', display: 'inline-block' }} />Running</span></Badge>
  if (status === 'completed') return <Badge variant="success"><span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}><CheckCircle2 size={10} />Completed</span></Badge>
  if (status === 'deviation') return <Badge variant="warning"><span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}><AlertTriangle size={10} />Deviation</span></Badge>
  return <Badge>{status}</Badge>
}

export function Home() {
  const navigate = useNavigate()
  const [workflows, setWorkflows] = useState<WorkflowDefinition[]>([])
  const [loading, setLoading] = useState(false)
  const [authed, setAuthed] = useState(!!getToken())
  const [loginForm, setLoginForm] = useState({ username: '', password: '' })
  const [loginError, setLoginError] = useState('')

  useEffect(() => {
    if (authed) {
      setLoading(true)
      Promise.all([listMyWorkflows(), listPublicWorkflows()])
        .then(([mine, pub]) => {
          const seen = new Set(mine.map(w => w.id))
          setWorkflows([...mine, ...pub.filter(w => !seen.has(w.id))])
        })
        .catch(() => {})
        .finally(() => setLoading(false))
    }
  }, [authed])

  async function handleLogin(e: React.FormEvent) {
    e.preventDefault()
    setLoginError('')
    try {
      await authenticate(loginForm.username, loginForm.password)
      setAuthed(true)
    } catch {
      setLoginError('Invalid credentials. Try OASIS_ADMIN / Uppermall1!')
    }
  }

  return (
    <div style={{ maxWidth: 1100, margin: '0 auto', padding: '40px 24px' }}>
      {/* Hero */}
      <div style={{ marginBottom: '40px' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 700, color: '#e2e2f0', marginBottom: '8px' }}>
          OASIS SOP Platform
        </h1>
        <p style={{ color: '#9090a8', fontSize: '0.95rem', maxWidth: 560 }}>
          Living standard operating procedures — executable, auditable, AI-guided, and published on STARNET.
        </p>
      </div>

      {/* Quick nav cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '16px', marginBottom: '40px' }}>
        {[
          { icon: <Activity size={20} />, title: 'SOPRunner', desc: 'Execute a procedure step by step with AI co-pilot', path: '/runner', color: '#6366f1' },
          { icon: <BarChart2 size={20} />, title: 'SOPIntel', desc: 'Analytics, deviation heatmaps, and AI improvements', path: '/intel', color: '#22c55e' },
          { icon: <Sparkles size={20} />, title: 'AI Authoring', desc: 'Describe your process in plain English — AI drafts the SOP', path: '/authoring', color: '#a855f7' },
        ].map(item => (
          <button
            key={item.path}
            onClick={() => navigate(item.path)}
            style={{
              background: '#111118', border: '1px solid #1e1e2a', borderRadius: '10px',
              padding: '20px', textAlign: 'left', cursor: 'pointer',
              transition: 'border-color 0.15s, box-shadow 0.15s',
            }}
            onMouseEnter={e => {
              e.currentTarget.style.borderColor = item.color + '60'
              e.currentTarget.style.boxShadow = `0 0 20px ${item.color}10`
            }}
            onMouseLeave={e => {
              e.currentTarget.style.borderColor = '#1e1e2a'
              e.currentTarget.style.boxShadow = 'none'
            }}
          >
            <div style={{ color: item.color, marginBottom: '10px' }}>{item.icon}</div>
            <div style={{ fontWeight: 600, color: '#e2e2f0', marginBottom: '6px' }}>{item.title}</div>
            <div style={{ fontSize: '0.8rem', color: '#9090a8' }}>{item.desc}</div>
          </button>
        ))}
      </div>

      {/* Stats row */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '12px', marginBottom: '40px' }}>
        {DEMO_STATS.map(s => <StatCard key={s.label} {...s} />)}
      </div>

      {/* Recent runs */}
      <Card style={{ marginBottom: '32px' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
          <h2 style={{ fontSize: '0.9rem', fontWeight: 600, color: '#e2e2f0' }}>Recent Runs</h2>
          <button
            onClick={() => navigate('/runner')}
            style={{ display: 'flex', alignItems: 'center', gap: '6px', background: '#6366f1', color: '#fff', padding: '6px 14px', borderRadius: '6px', fontSize: '0.8rem', fontWeight: 600 }}
          >
            <Play size={12} /> Start New Run
          </button>
        </div>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid #1e1e2a' }}>
              {['SOP', 'Current Step', 'Assigned To', 'Elapsed', 'Status', ''].map(h => (
                <th key={h} style={{ padding: '8px 12px', textAlign: 'left', fontSize: '0.7rem', color: '#5a5a72', textTransform: 'uppercase', letterSpacing: '0.07em', fontWeight: 600 }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {RECENT_RUNS.map(run => (
              <tr
                key={run.id}
                style={{ borderBottom: '1px solid #1e1e2a', cursor: 'pointer', transition: 'background 0.1s' }}
                onMouseEnter={e => e.currentTarget.style.background = '#18181f'}
                onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
                onClick={() => navigate(`/runner/${run.id}`)}
              >
                <td style={{ padding: '12px', fontSize: '0.85rem', color: '#e2e2f0', fontWeight: 500 }}>{run.sop}</td>
                <td style={{ padding: '12px', fontSize: '0.8rem', color: '#9090a8' }}>{run.step}</td>
                <td style={{ padding: '12px', fontSize: '0.8rem', color: '#9090a8' }}>{run.avatar}</td>
                <td style={{ padding: '12px' }}>
                  <span style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '0.75rem', color: '#5a5a72', fontFamily: 'JetBrains Mono, monospace' }}>
                    <Clock size={11} />{run.elapsed}
                  </span>
                </td>
                <td style={{ padding: '12px' }}>{statusBadge(run.status)}</td>
                <td style={{ padding: '12px' }}>
                  <ExternalLink size={13} color="#5a5a72" />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      {/* STARNET SOPs */}
      <Card>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
          <h2 style={{ fontSize: '0.9rem', fontWeight: 600, color: '#e2e2f0' }}>STARNET SOP Library</h2>
          {!authed && (
            <span style={{ fontSize: '0.75rem', color: '#f59e0b', display: 'flex', alignItems: 'center', gap: '4px' }}>
              <Lock size={11} /> Sign in to load live SOPs
            </span>
          )}
        </div>

        {!authed ? (
          <form onSubmit={handleLogin} style={{ display: 'flex', gap: '10px', alignItems: 'flex-end', flexWrap: 'wrap' }}>
            <div style={{ flex: 1, minWidth: 200 }}>
              <label style={{ display: 'block', fontSize: '0.75rem', color: '#9090a8', marginBottom: '4px' }}>Username</label>
              <input value={loginForm.username} onChange={e => setLoginForm(f => ({ ...f, username: e.target.value }))} placeholder="OASIS_ADMIN" />
            </div>
            <div style={{ flex: 1, minWidth: 200 }}>
              <label style={{ display: 'block', fontSize: '0.75rem', color: '#9090a8', marginBottom: '4px' }}>Password</label>
              <input type="password" value={loginForm.password} onChange={e => setLoginForm(f => ({ ...f, password: e.target.value }))} placeholder="••••••••" />
            </div>
            <button type="submit" style={{ background: '#6366f1', color: '#fff', padding: '9px 20px', borderRadius: '6px', fontWeight: 600, whiteSpace: 'nowrap' }}>
              Connect to STARNET
            </button>
            {loginError && <p style={{ width: '100%', fontSize: '0.8rem', color: '#ef4444' }}>{loginError}</p>}
          </form>
        ) : loading ? (
          <p style={{ color: '#5a5a72', fontSize: '0.85rem' }}>Loading workflows from STARNET...</p>
        ) : workflows.length === 0 ? (
          <p style={{ color: '#5a5a72', fontSize: '0.85rem' }}>No workflows found. Create one in the AI Authoring tab or Workflow Builder.</p>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '12px' }}>
            {workflows.slice(0, 9).map(wf => (
              <button
                key={wf.id}
                onClick={() => navigate(`/runner/${wf.id}`)}
                style={{
                  background: '#18181f', border: '1px solid #2a2a38', borderRadius: '8px',
                  padding: '14px', textAlign: 'left', cursor: 'pointer',
                  transition: 'border-color 0.15s',
                }}
                onMouseEnter={e => e.currentTarget.style.borderColor = '#6366f160'}
                onMouseLeave={e => e.currentTarget.style.borderColor = '#2a2a38'}
              >
                <div style={{ fontWeight: 600, fontSize: '0.85rem', color: '#e2e2f0', marginBottom: '4px' }}>{wf.name}</div>
                <div style={{ fontSize: '0.75rem', color: '#9090a8', marginBottom: '8px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {wf.description}
                </div>
                <Badge variant="info">Run SOP</Badge>
              </button>
            ))}
          </div>
        )}
      </Card>
    </div>
  )
}
