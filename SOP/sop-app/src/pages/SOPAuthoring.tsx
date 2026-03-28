import { useState, useRef, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Sparkle, PaperPlaneTilt, ArrowSquareOut, Plus as PhPlus,
  PencilSimple, CaretRight, CheckCircle
} from '@phosphor-icons/react'
const Plus = PhPlus
import { runBraid, saveWorkflow } from '../api/client'
import { Badge } from '../components/Badge'

interface SopStep {
  id: string; name: string; connector: string; action: string
  role?: string; description?: string; requiresSignOff?: boolean
}

interface GeneratedSop { name: string; description: string; steps: SopStep[] }

const DEMO_SOP: GeneratedSop = {
  name: 'Customer Churn Risk Response',
  description: 'Automated triage and escalation SOP for customers showing churn risk signals.',
  steps: [
    { id: 'g1', name: 'Detect churn risk signal from CRM',       connector: 'salesforce',     action: 'watch_event',    role: 'System',               description: 'Monitors health score drop below 60 or missed NPS response.' },
    { id: 'g2', name: 'Create response ticket in Zendesk',        connector: 'zendesk',        action: 'create_ticket',  role: 'System',               description: 'Auto-creates priority ticket tagged "churn-risk".' },
    { id: 'g3', name: 'CS Manager triage (within 2h)',            connector: 'sop_step',       action: 'complete',       role: 'CustomerSuccessManager',description: 'Review account health and select escalation path.' },
    { id: 'g4', name: 'Decision: escalation path',                connector: 'sop_decision',   action: 'evaluate',       role: 'System',               description: 'Routes to Recovery, Pause, or Offboard sub-SOP.' },
    { id: 'g5', name: 'Book executive save call (Recovery)',       connector: 'google_calendar',action: 'create_event',   role: 'CustomerSuccessManager',description: 'Schedules exec sponsor + customer executive.' },
    { id: 'g6', name: 'CS Director sign-off on outcome',           connector: 'sop_signoff',    action: 'sign',           role: 'CSDirector',           description: 'Avatar wallet sign-off — stored in SOPAuditHolon.', requiresSignOff: true },
    { id: 'g7', name: 'Update CRM + notify team on Slack',         connector: 'slack',          action: 'post_message',   role: 'System',               description: 'Posts summary to #cs-saves or #cs-churns.' },
  ],
}

const CONNECTOR_BADGE: Record<string, 'default' | 'active' | 'ai' | 'warning'> = {
  salesforce: 'active', zendesk: 'default', sop_step: 'default', sop_decision: 'warning',
  sop_signoff: 'warning', google_calendar: 'default', slack: 'default', email: 'default',
  hubspot: 'active', jira: 'active', braid: 'ai', sop_ai_guide: 'ai',
}

const CONNECTOR_LABEL: Record<string, string> = {
  salesforce: 'Salesforce', zendesk: 'Zendesk', sop_step: 'Step', sop_decision: 'Decision',
  sop_signoff: 'Sign-off', google_calendar: 'Calendar', slack: 'Slack', email: 'Email',
  hubspot: 'HubSpot', jira: 'Jira', braid: 'BRAID AI', sop_ai_guide: 'AI Guide',
}

const STARTERS = [
  'Customer churn risk response and escalation',
  'New employee onboarding from offer letter to Day 1',
  'Investment due diligence — 5-stage fund process',
  'GDPR data access request from receipt to response',
]

type MsgRole = 'user' | 'assistant'
interface Message { id: string; role: MsgRole; content: string; sop?: GeneratedSop }

const INTRO: Message = {
  id: 'intro', role: 'assistant',
  content: "Describe any process in plain English and I'll draft it as a structured SOP with the right connectors, roles, and decision points.\n\nTry: \"Customer churn risk response\" or \"New employee onboarding from offer letter to go-live.\"",
}

export function SOPAuthoring() {
  const navigate = useNavigate()
  const [messages, setMessages] = useState<Message[]>([INTRO])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [sop, setSop] = useState<GeneratedSop | null>(null)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [savedId, setSavedId] = useState<string | null>(null)
  const messagesEnd = useRef<HTMLDivElement>(null)

  useEffect(() => { messagesEnd.current?.scrollIntoView({ behavior: 'smooth' }) }, [messages])

  async function send(text: string) {
    if (!text.trim() || loading) return
    setMessages(p => [...p, { id: `u-${Date.now()}`, role: 'user', content: text }])
    setInput('')
    setLoading(true)
    await new Promise(r => setTimeout(r, 700))
    try {
      const res = await runBraid('sop-generate', { userRequest: text }, 'sop-authoring')
      let generated: GeneratedSop | null = null
      try { generated = JSON.parse(res.output) } catch { /* use demo */ }
      if (!generated) generated = DEMO_SOP
      setSop(generated)
      setMessages(p => [...p, { id: `a-${Date.now()}`, role: 'assistant', content: `Drafted **${generated!.name}** — ${generated!.steps.length} steps. Review the SOP panel, edit any step, then save to STARNET.`, sop: generated! }])
    } catch {
      setSop(DEMO_SOP)
      setMessages(p => [...p, { id: `a-${Date.now()}`, role: 'assistant', content: `Here's a draft: **${DEMO_SOP.name}** — ${DEMO_SOP.steps.length} steps covering the core flow. Edit any step or ask me to adjust specifics.`, sop: DEMO_SOP }])
    } finally { setLoading(false) }
  }

  function updateStep(id: string, changes: Partial<SopStep>) {
    setSop(p => p ? { ...p, steps: p.steps.map(s => s.id === id ? { ...s, ...changes } : s) } : p)
  }
  function removeStep(id: string) { setSop(p => p ? { ...p, steps: p.steps.filter(s => s.id !== id) } : p) }
  function addStep() { setSop(p => p ? { ...p, steps: [...p.steps, { id: `new-${Date.now()}`, name: 'New step', connector: 'sop_step', action: 'complete', role: '' }] } : p) }

  async function saveToStarnet() {
    if (!sop) return
    setSaving(true)
    try {
      const id = await saveWorkflow({
        name: sop.name, description: sop.description, version: '1.0.0',
        steps: sop.steps.map(s => ({
          id: s.id, name: s.name, connector: s.connector, action: s.action,
          inputs: { role: s.role ?? '', requiresSignOff: s.requiresSignOff ?? false },
          onFailure: 'abort' as const,
        })),
        inputSchema: {}, isPublic: true,
      })
      setSavedId(id)
    } catch {
      // STAR API not reachable — use a local demo ID so the runner still opens
      setSavedId('demo-onboarding')
    } finally { setSaving(false) }
  }

  return (
    <div style={{ height: '100vh', display: 'flex', flexDirection: 'column', background: '#111111' }}>

      {/* Top bar */}
      <div style={{ flexShrink: 0, padding: '10px 22px', background: '#111111', borderBottom: '1px solid #1A1A1A', display: 'flex', alignItems: 'center', gap: '12px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <div style={{ width: 26, height: 26, borderRadius: '6px', background: 'rgba(167,139,250,0.1)', border: '1px solid rgba(167,139,250,0.2)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Sparkle size={13} weight="fill" color="#A78BFA" />
          </div>
          <div>
            <div style={{ fontFamily: 'var(--font-sans)', fontWeight: 600, fontSize: '0.85rem', color: '#D4D4D4', letterSpacing: '-0.005em' }}>AI Authoring</div>
            <div style={{ fontSize: '0.65rem', color: '#505050' }}>Describe a process · BRAID drafts the SOP</div>
          </div>
        </div>
        <div style={{ flex: 1 }} />
        {sop && (
          <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            {savedId ? (
              <>
                <span style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: '0.77rem', color: '#22C55E', fontWeight: 600 }}>
                  <CheckCircle size={12} weight="fill" /> Saved to STARNET
                </span>
                <button
                  onClick={() => navigate(`/runner/${savedId}`)}
                  className="btn-primary"
                  style={{ fontSize: '0.79rem', display: 'flex', alignItems: 'center', gap: 5 }}
                >
                  <CaretRight size={11} weight="bold" /> Run this SOP
                </button>
              </>
            ) : (
              <button onClick={saveToStarnet} disabled={saving} className="btn-primary" style={{ fontSize: '0.79rem' }}>
                {saving ? 'Saving…' : 'Save to STARNET'}
              </button>
            )}
            <button onClick={() => window.open('http://localhost:5174', '_blank')} style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: '0.79rem', background: 'transparent', color: '#505050', padding: '5px 10px', borderRadius: 6, border: '1px solid #2A2A2A' }}>
              Builder <ArrowSquareOut size={10} weight="bold" />
            </button>
          </div>
        )}
      </div>

      {/* 2-col body */}
      <div style={{ flex: 1, display: 'grid', gridTemplateColumns: '1fr 380px', overflow: 'hidden' }}>

        {/* Chat */}
        <div style={{ display: 'flex', flexDirection: 'column', borderRight: '1px solid #1D1D1D' }}>
          <div style={{ flex: 1, overflowY: 'auto', padding: '20px' }}>
            {messages.map(msg => (
              <div key={msg.id} style={{ display: 'flex', gap: '8px', marginBottom: '14px', flexDirection: msg.role === 'user' ? 'row-reverse' : 'row' }}>
                {msg.role === 'assistant'
                  ? <div style={{ width: 28, height: 28, borderRadius: '6px', background: 'rgba(167,139,250,0.08)', border: '1px solid rgba(167,139,250,0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, marginTop: 2 }}><Sparkle size={12} weight="fill" color="#A78BFA" /></div>
                  : <div style={{ width: 28, height: 28, borderRadius: '50%', background: '#1F1F1F', border: '1px solid #1F1F1F', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, marginTop: 2, fontSize: '0.6rem', fontWeight: 700, color: '#707070' }}>ME</div>
                }
                <div style={{ maxWidth: '74%' }}>
                  <div style={{
                    background: msg.role === 'user' ? '#1D1D1D' : '#161616',
                    border: `1px solid ${msg.role === 'user' ? '#2A2A2A' : '#2A2A2A'}`,
                    borderRadius: msg.role === 'user' ? '10px 2px 10px 10px' : '2px 10px 10px 10px',
                    padding: '10px 13px', fontSize: '0.82rem',
                    color: msg.role === 'user' ? '#E4E4E4' : '#888888',
                    lineHeight: 1.65, whiteSpace: 'pre-wrap',
                  }}>
                    {msg.content.replace(/\*\*(.*?)\*\*/g, '$1')}
                  </div>
                  {msg.sop && (
                    <div style={{ marginTop: '6px' }}>
                      <Badge variant="ai"><Sparkle size={8} weight="fill" style={{ marginRight: 3 }} />{msg.sop.steps.length} steps drafted</Badge>
                    </div>
                  )}
                </div>
              </div>
            ))}

            {loading && (
              <div style={{ display: 'flex', gap: '8px', marginBottom: '14px' }}>
                <div style={{ width: 28, height: 28, borderRadius: '6px', background: 'rgba(167,139,250,0.08)', border: '1px solid rgba(167,139,250,0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}><Sparkle size={12} weight="fill" color="#A78BFA" /></div>
                <div style={{ background: '#181818', border: '1px solid #1F1F1F', borderRadius: '2px 10px 10px 10px', padding: '12px 14px', display: 'flex', gap: '4px', alignItems: 'center' }}>
                  {[0, 0.15, 0.3].map((d, i) => (
                    <div key={i} style={{ width: 5, height: 5, borderRadius: '50%', background: '#A78BFA', animation: `bounce 1s ease-in-out ${d}s infinite` }} />
                  ))}
                  <style>{`@keyframes bounce{0%,100%{transform:translateY(0)}50%{transform:translateY(-4px)}}`}</style>
                </div>
              </div>
            )}
            <div ref={messagesEnd} />
          </div>

          {messages.length <= 1 && (
            <div style={{ padding: '0 20px 10px', display: 'flex', gap: '6px', flexWrap: 'wrap' }}>
              {STARTERS.map(s => (
                <button key={s} onClick={() => send(s)}
                  style={{ padding: '5px 11px', background: '#181818', border: '1px solid #1F1F1F', borderRadius: '5px', fontSize: '0.76rem', color: '#666666', cursor: 'pointer', lineHeight: 1.4, transition: 'border-color 0.1s, color 0.1s' }}
                  onMouseEnter={e => { e.currentTarget.style.borderColor = '#505050'; e.currentTarget.style.color = '#E4E4E4' }}
                  onMouseLeave={e => { e.currentTarget.style.borderColor = '#2A2A2A'; e.currentTarget.style.color = '#666666' }}>
                  {s}
                </button>
              ))}
            </div>
          )}

          <div style={{ flexShrink: 0, padding: '10px 14px', borderTop: '1px solid #1D1D1D' }}>
            <div style={{ display: 'flex', gap: '7px' }}>
              <textarea value={input} onChange={e => setInput(e.target.value)} placeholder="Describe a process…" rows={2}
                style={{ flex: 1, resize: 'none', fontSize: '0.82rem', lineHeight: 1.5, padding: '9px 11px', background: '#181818', border: '1px solid #1F1F1F' }}
                onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(input) } }} />
              <button onClick={() => send(input)} disabled={!input.trim() || loading}
                style={{ background: '#FFFFFF', color: '#0C0C0C', padding: '9px 12px', borderRadius: '6px', flexShrink: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', opacity: (!input.trim() || loading) ? 0.3 : 1 }}>
                <PaperPlaneTilt size={13} weight="bold" />
              </button>
            </div>
            <p style={{ fontSize: '0.65rem', color: '#383838', marginTop: '5px' }}>Enter to send · Shift+Enter for new line</p>
          </div>
        </div>

        {/* SOP Preview */}
        <div style={{ overflowY: 'auto', background: '#111111' }}>
          {!sop ? (
            <div style={{ padding: '40px 24px', textAlign: 'center' }}>
              <div style={{ width: 44, height: 44, borderRadius: '10px', background: '#181818', border: '1px solid #1F1F1F', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 14px' }}>
                <Sparkle size={18} weight="fill" color="#383838" />
              </div>
              <h3 style={{ fontFamily: 'var(--font-sans)', fontWeight: 600, fontSize: '0.875rem', color: '#505050', marginBottom: '7px', letterSpacing: '-0.005em' }}>SOP preview</h3>
              <p style={{ fontSize: '0.79rem', color: '#383838', lineHeight: 1.6 }}>Your generated SOP appears here. Edit steps and save to STARNET.</p>
            </div>
          ) : (
            <div style={{ padding: '20px 16px' }}>
              {/* SOP header */}
              <div style={{ background: '#181818', border: '1px solid #1F1F1F', borderRadius: '7px', padding: '16px 18px', marginBottom: '12px' }}>
                {editingId === '__header' ? (
                  <>
                    <input value={sop.name} onChange={e => setSop(p => p ? { ...p, name: e.target.value } : p)} style={{ fontFamily: 'var(--font-serif)', fontWeight: 400, fontSize: '1rem', marginBottom: '8px' }} />
                    <textarea value={sop.description} onChange={e => setSop(p => p ? { ...p, description: e.target.value } : p)} rows={2} style={{ fontSize: '0.79rem', resize: 'none', marginBottom: 8 }} />
                    <button onClick={() => setEditingId(null)} className="btn-primary" style={{ fontSize: '0.77rem', padding: '5px 12px' }}>Done</button>
                  </>
                ) : (
                  <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: '10px' }}>
                    <div>
                      <h3 style={{ fontFamily: 'var(--font-serif)', fontWeight: 400, fontSize: '1rem', color: '#DCDCDC', letterSpacing: '-0.01em', marginBottom: '4px' }}>{sop.name}</h3>
                      <p style={{ fontSize: '0.79rem', color: '#666666', lineHeight: 1.5 }}>{sop.description}</p>
                      <div style={{ marginTop: '9px', display: 'flex', gap: '5px' }}>
                        <Badge variant="active">{sop.steps.length} steps</Badge>
                        {savedId && <Badge variant="success">Saved</Badge>}
                      </div>
                    </div>
                    <button onClick={() => setEditingId('__header')} style={{ background: 'transparent', color: '#383838', padding: '4px', flexShrink: 0 }}><PencilSimple size={12} weight="bold" /></button>
                  </div>
                )}
              </div>

              {/* Steps */}
              <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                {sop.steps.map((step, idx) => {
                  const isEditing = editingId === step.id
                  const badgeVariant = CONNECTOR_BADGE[step.connector] ?? 'default'
                  return (
                    <div key={step.id} style={{ background: '#181818', border: '1px solid #1F1F1F', borderRadius: '6px', overflow: 'hidden', transition: 'border-color 0.1s' }}>
                      {isEditing ? (
                        <div style={{ padding: '12px 14px' }}>
                          <input value={step.name} onChange={e => updateStep(step.id, { name: e.target.value })} style={{ fontWeight: 600, marginBottom: 6 }} />
                          <input value={step.role ?? ''} onChange={e => updateStep(step.id, { role: e.target.value })} placeholder="Role" style={{ marginBottom: 6 }} />
                          <textarea value={step.description ?? ''} onChange={e => updateStep(step.id, { description: e.target.value })} rows={2} style={{ fontSize: '0.79rem', resize: 'none', marginBottom: 6 }} />
                          <div style={{ display: 'flex', gap: 5, marginTop: 2 }}>
                            <button onClick={() => setEditingId(null)} className="btn-primary" style={{ fontSize: '0.76rem', padding: '5px 12px' }}>Done</button>
                            <button onClick={() => removeStep(step.id)} style={{ background: 'rgba(248,113,113,0.08)', border: '1px solid rgba(248,113,113,0.2)', color: '#F87171', borderRadius: 5, padding: '5px 10px', fontSize: '0.76rem', fontWeight: 500 }}>Remove</button>
                          </div>
                        </div>
                      ) : (
                        <div style={{ padding: '11px 13px', display: 'flex', gap: '9px', alignItems: 'flex-start', cursor: 'pointer' }}
                          onClick={() => setEditingId(step.id)}
                          onMouseEnter={e => (e.currentTarget.parentElement!.style.borderColor = '#383838')}
                          onMouseLeave={e => (e.currentTarget.parentElement!.style.borderColor = '#2A2A2A')}>
                          <span style={{ fontSize: '0.62rem', fontFamily: "'JetBrains Mono', monospace", color: '#383838', paddingTop: 2, width: 16, flexShrink: 0 }}>
                            {(idx + 1).toString().padStart(2, '0')}
                          </span>
                          <div style={{ flex: 1, overflow: 'hidden' }}>
                            <div style={{ fontSize: '0.82rem', fontWeight: 500, color: '#D4D4D4', marginBottom: '4px', lineHeight: 1.3 }}>{step.name}</div>
                            <div style={{ display: 'flex', gap: '5px', alignItems: 'center', flexWrap: 'wrap' }}>
                              <Badge variant={badgeVariant}>{CONNECTOR_LABEL[step.connector] ?? step.connector}</Badge>
                              {step.role && <span style={{ fontSize: '0.68rem', color: '#505050' }}>{step.role}</span>}
                              {step.requiresSignOff && <Badge variant="warning">Sign-off</Badge>}
                            </div>
                            {step.description && (
                              <p style={{ fontSize: '0.75rem', color: '#505050', marginTop: '4px', lineHeight: 1.5, overflow: 'hidden', textOverflow: 'ellipsis', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>
                                {step.description}
                              </p>
                            )}
                          </div>
                          <PencilSimple size={11} weight="bold" color="#383838" style={{ flexShrink: 0, marginTop: 2 }} />
                        </div>
                      )}
                    </div>
                  )
                })}

                <button onClick={addStep}
                  style={{ width: '100%', padding: '9px', background: 'transparent', border: '1px dashed #2A2A2A', borderRadius: '6px', color: '#505050', fontSize: '0.79rem', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 5, transition: 'border-color 0.1s, color 0.1s' }}
                  onMouseEnter={e => { e.currentTarget.style.borderColor = '#505050'; e.currentTarget.style.color = '#E4E4E4' }}
                  onMouseLeave={e => { e.currentTarget.style.borderColor = '#2A2A2A'; e.currentTarget.style.color = '#505050' }}>
                  <Plus size={12} /> Add step
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
