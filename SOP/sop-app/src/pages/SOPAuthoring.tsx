import { useState, useRef, useEffect } from 'react'
import { Sparkles, Send, User, Cpu, Plus, Trash2, GripVertical, ExternalLink, RefreshCw, CheckCircle2 } from 'lucide-react'
import { runBraid } from '../api/client'
import { Badge } from '../components/Badge'

// ─── Types ───────────────────────────────────────────────────────────────────

interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
  timestamp: string
}

interface DraftStep {
  id: string
  name: string
  description: string
  role: string
  connector: string
  requiresSignOff: boolean
  requiresEvidence: boolean
  aiPrompt: string
  timeoutMinutes: number
}

// ─── Prompt engineering ──────────────────────────────────────────────────────

const SYSTEM_PROMPT = `You are an expert at designing Holonic Standard Operating Procedures (SOPs) for OASIS.
When the user describes a business process, extract it as a structured JSON SOP with this exact format:

{
  "sopName": "string",
  "description": "string",
  "category": "CustomerSuccess|Operations|HR|Compliance|Finance|Sales|Technical|Other",
  "estimatedDurationMinutes": number,
  "requiredRoles": ["string"],
  "steps": [
    {
      "id": "s1",
      "name": "string (short action title)",
      "description": "string (full instruction text for the executor)",
      "role": "string (who does this step)",
      "connector": "sop_step|sop_signoff|sop_decision|sop_ai_guide|webhook|email|slack|salesforce|hubspot|zendesk|docusign|google_calendar|jira|zapier|avatar|braid",
      "requiresSignOff": boolean,
      "requiresEvidence": boolean,
      "aiPrompt": "string (BRAID prompt for AI guidance at this step, or empty)",
      "timeoutMinutes": number
    }
  ]
}

Always return valid JSON. If the user provides more context, update the JSON. Start with a brief acknowledgment then the JSON block.`

// ─── Demo steps to pre-fill when API is unavailable ─────────────────────────

const DEMO_STEPS: DraftStep[] = [
  { id: 's1', name: 'Receive and acknowledge request', description: 'Log the incoming request, assign a reference number, and send an automated acknowledgement to the requester within 2 hours.', role: 'OperationsManager', connector: 'email', requiresSignOff: false, requiresEvidence: false, aiPrompt: 'Draft a professional acknowledgement email for this type of request.', timeoutMinutes: 120 },
  { id: 's2', name: 'Initial assessment and triage', description: 'Review the request details, assess complexity and priority, and route to the appropriate team. Flag any immediate risks.', role: 'OperationsManager', connector: 'sop_step', requiresSignOff: false, requiresEvidence: true, aiPrompt: 'Assess this request and suggest a priority level (Low/Medium/High/Critical) with reasoning. Identify any risks or blockers.', timeoutMinutes: 60 },
  { id: 's3', name: 'Complexity routing decision', description: 'Route to Fast Track (simple, <2 days) or Standard Process (complex, up to 2 weeks) based on assessment.', role: 'System', connector: 'sop_decision', requiresSignOff: false, requiresEvidence: false, aiPrompt: '', timeoutMinutes: 5 },
  { id: 's4', name: 'Assign team member and brief', description: 'Assign the request to the most appropriate available team member based on skills and workload. Send briefing pack.', role: 'TeamLead', connector: 'slack', requiresSignOff: true, requiresEvidence: false, aiPrompt: 'Based on the request type and team member profiles, recommend the best assignee and draft a briefing message.', timeoutMinutes: 240 },
  { id: 's5', name: 'Execute and document deliverable', description: 'Complete the requested work. Document all decisions and outputs. Upload final deliverable for review.', role: 'Assignee', connector: 'sop_step', requiresSignOff: false, requiresEvidence: true, aiPrompt: 'Review the uploaded deliverable and check it meets the original request criteria. Flag any gaps.', timeoutMinutes: 1440 },
  { id: 's6', name: 'Quality review and approval', description: 'Review the deliverable against quality standards. Approve, request revisions, or reject with feedback.', role: 'TeamLead', connector: 'sop_signoff', requiresSignOff: true, requiresEvidence: false, aiPrompt: 'Draft a quality review checklist tailored to this type of deliverable.', timeoutMinutes: 480 },
  { id: 's7', name: 'Deliver and close request', description: 'Send the final deliverable to the requester. Update CRM/ticketing system. Log completion and collect feedback.', role: 'OperationsManager', connector: 'zendesk', requiresSignOff: false, requiresEvidence: false, aiPrompt: 'Draft a professional delivery email summarising what was completed and next steps for the requester.', timeoutMinutes: 60 },
]

const CONNECTOR_COLORS: Record<string, string> = {
  email: '#f59e0b', slack: '#36c037', salesforce: '#1e40af', hubspot: '#f97316',
  zendesk: '#7c3aed', docusign: '#16a34a', google_calendar: '#db2777', jira: '#2563eb',
  zapier: '#ca8a04', sop_step: '#ea580c', sop_decision: '#dc2626', sop_signoff: '#059669',
  sop_ai_guide: '#9333ea', webhook: '#475569', avatar: '#0e7490', braid: '#9333ea',
}

// ─── Component ───────────────────────────────────────────────────────────────

export function SOPAuthoring() {
  const [messages, setMessages] = useState<ChatMessage[]>([{
    role: 'assistant',
    content: "Hi! I'm BRAID — your AI SOP author. Describe your business process in plain English and I'll design the full Holonic SOP structure for you.\n\nFor example: \"We need an SOP for handling customer refund requests — from initial receipt through approval, processing, and confirmation.\"",
    timestamp: new Date().toLocaleTimeString(),
  }])
  const [input, setInput] = useState('')
  const [sending, setSending] = useState(false)
  const [steps, setSteps] = useState<DraftStep[]>([])
  const [sopName, setSopName] = useState('')
  const [sopCategory, setSopCategory] = useState('')
  const [sopDuration, setSopDuration] = useState(0)
  const [editingStep, setEditingStep] = useState<string | null>(null)
  const [exported, setExported] = useState(false)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLTextAreaElement>(null)

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  async function sendMessage() {
    if (!input.trim() || sending) return
    const userMsg: ChatMessage = { role: 'user', content: input.trim(), timestamp: new Date().toLocaleTimeString() }
    setMessages(prev => [...prev, userMsg])
    const userInput = input.trim()
    setInput('')
    setSending(true)

    try {
      const res = await runBraid('sop-authoring', {
        systemPrompt: SYSTEM_PROMPT,
        userMessage: userInput,
        conversationHistory: messages.slice(-6).map(m => ({ role: m.role, content: m.content })),
      })

      const assistantMsg: ChatMessage = {
        role: 'assistant',
        content: res.output,
        timestamp: new Date().toLocaleTimeString(),
      }
      setMessages(prev => [...prev, assistantMsg])

      // Try to extract JSON from the response
      const jsonMatch = res.output.match(/\{[\s\S]*"sopName"[\s\S]*\}/)
      if (jsonMatch) {
        try {
          const parsed = JSON.parse(jsonMatch[0])
          setSopName(parsed.sopName ?? '')
          setSopCategory(parsed.category ?? '')
          setSopDuration(parsed.estimatedDurationMinutes ?? 0)
          if (parsed.steps?.length) {
            setSteps(parsed.steps.map((s: DraftStep, i: number) => ({
              ...s,
              id: s.id ?? `s${i + 1}`,
            })))
          }
        } catch { /* JSON parse failed — show raw output only */ }
      }
    } catch {
      // API unavailable — show demo
      const demoMsg: ChatMessage = {
        role: 'assistant',
        content: `I've designed a 7-step Holonic SOP for your process. Here's the structure (BRAID demo mode — STAR API not connected):\n\n**${userInput || 'Operations Request Handling SOP'}**\n\nI've generated the step preview on the right. You can edit any step inline, reorder them, and export to the Workflow Builder when ready.`,
        timestamp: new Date().toLocaleTimeString(),
      }
      setMessages(prev => [...prev, demoMsg])
      setSopName(userInput.length > 60 ? userInput.slice(0, 57) + '...' : userInput || 'Operations Request Handling')
      setSopCategory('Operations')
      setSopDuration(480)
      setSteps(DEMO_STEPS)
    } finally {
      setSending(false)
    }
  }

  function updateStep(id: string, field: keyof DraftStep, value: string | boolean | number) {
    setSteps(prev => prev.map(s => s.id === id ? { ...s, [field]: value } : s))
  }

  function deleteStep(id: string) {
    setSteps(prev => prev.filter(s => s.id !== id))
  }

  function addStep() {
    const newId = `s${Date.now()}`
    setSteps(prev => [...prev, {
      id: newId, name: 'New step', description: '',
      role: '', connector: 'sop_step',
      requiresSignOff: false, requiresEvidence: false,
      aiPrompt: '', timeoutMinutes: 60,
    }])
    setEditingStep(newId)
  }

  function exportToBuilder() {
    const workflow = {
      name: sopName,
      description: `AI-authored SOP — ${sopCategory} — Est. ${sopDuration} min`,
      version: '1.0.0',
      isPublic: false,
      steps: steps.map(s => ({
        id: s.id, name: s.name, connector: s.connector, action: 'complete',
        inputs: { role: s.role, requiresSignOff: s.requiresSignOff, requiresEvidence: s.requiresEvidence, aiPrompt: s.aiPrompt, timeoutMinutes: s.timeoutMinutes },
        onFailure: 'abort',
      })),
      inputSchema: {},
    }
    localStorage.setItem('oasis_import_workflow', JSON.stringify(workflow))
    setExported(true)
    setTimeout(() => window.open('http://localhost:5174', '_blank'), 300)
  }

  return (
    <div style={{ height: 'calc(100vh - 52px)', display: 'flex' }}>

      {/* Left — chat */}
      <div style={{ width: '420px', flexShrink: 0, display: 'flex', flexDirection: 'column', borderRight: '1px solid #1e1e2a' }}>
        {/* Header */}
        <div style={{ flexShrink: 0, padding: '16px 20px', borderBottom: '1px solid #1e1e2a', background: '#0d0d14' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
            <Sparkles size={16} color="#a855f7" />
            <h1 style={{ fontSize: '1rem', fontWeight: 700, color: '#e2e2f0' }}>AI SOP Authoring</h1>
          </div>
          <p style={{ fontSize: '0.78rem', color: '#9090a8' }}>
            Describe your process → BRAID designs the full SOP structure
          </p>
        </div>

        {/* Messages */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '16px', display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {messages.map((msg, i) => (
            <div key={i} style={{ display: 'flex', gap: '10px', alignItems: 'flex-start' }}>
              <div style={{
                width: 28, height: 28, borderRadius: '50%', flexShrink: 0,
                background: msg.role === 'user' ? '#6366f130' : '#a855f720',
                border: `1px solid ${msg.role === 'user' ? '#6366f140' : '#a855f730'}`,
                display: 'flex', alignItems: 'center', justifyContent: 'center',
              }}>
                {msg.role === 'user' ? <User size={13} color="#818cf8" /> : <Cpu size={13} color="#c084fc" />}
              </div>
              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'baseline', gap: '8px', marginBottom: '4px' }}>
                  <span style={{ fontSize: '0.72rem', fontWeight: 600, color: msg.role === 'user' ? '#818cf8' : '#c084fc' }}>
                    {msg.role === 'user' ? 'You' : 'BRAID'}
                  </span>
                  <span style={{ fontSize: '0.65rem', color: '#5a5a72', fontFamily: 'JetBrains Mono, monospace' }}>{msg.timestamp}</span>
                </div>
                <div style={{
                  background: msg.role === 'user' ? '#18181f' : '#1a0033',
                  border: `1px solid ${msg.role === 'user' ? '#2a2a38' : '#a855f720'}`,
                  borderRadius: '8px', padding: '12px 14px',
                  fontSize: '0.83rem', color: msg.role === 'user' ? '#e2e2f0' : '#d0c0e8',
                  lineHeight: 1.65, whiteSpace: 'pre-wrap',
                }}>
                  {msg.content.replace(/\{[\s\S]*\}/, '[JSON structure generated — see step preview →]')}
                </div>
              </div>
            </div>
          ))}
          {sending && (
            <div style={{ display: 'flex', gap: '10px', alignItems: 'flex-start' }}>
              <div style={{ width: 28, height: 28, borderRadius: '50%', background: '#a855f720', border: '1px solid #a855f730', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Cpu size={13} color="#c084fc" />
              </div>
              <div style={{ background: '#1a0033', border: '1px solid #a855f720', borderRadius: '8px', padding: '12px 14px' }}>
                <div style={{ display: 'flex', gap: '4px' }}>
                  {[0, 1, 2].map(i => (
                    <div key={i} style={{ width: 6, height: 6, borderRadius: '50%', background: '#a855f7', animation: 'bounce 1s ease-in-out infinite', animationDelay: `${i * 0.2}s` }} />
                  ))}
                  <style>{`@keyframes bounce { 0%,100% { transform: translateY(0); opacity:0.3 } 50% { transform: translateY(-4px); opacity:1 } }`}</style>
                </div>
              </div>
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>

        {/* Starter prompts */}
        {messages.length === 1 && (
          <div style={{ flexShrink: 0, padding: '0 16px 12px', display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <p style={{ fontSize: '0.68rem', color: '#5a5a72', textTransform: 'uppercase', letterSpacing: '0.07em', fontWeight: 600, marginBottom: '2px' }}>
              Try one of these
            </p>
            {[
              'Create an SOP for enterprise customer onboarding — from deal closed to go-live in 48 hours',
              'Design a due diligence SOP for evaluating startup investments with IC vote and term sheet',
              'Build a monthly ESG impact reporting SOP with AI analysis and stakeholder sign-off',
            ].map(prompt => (
              <button
                key={prompt}
                onClick={() => { setInput(prompt); inputRef.current?.focus() }}
                style={{
                  textAlign: 'left', padding: '9px 12px', background: '#18181f',
                  border: '1px solid #2a2a38', borderRadius: '6px',
                  fontSize: '0.78rem', color: '#9090a8', cursor: 'pointer',
                  transition: 'border-color 0.15s', lineHeight: 1.4,
                }}
                onMouseEnter={e => e.currentTarget.style.borderColor = '#a855f740'}
                onMouseLeave={e => e.currentTarget.style.borderColor = '#2a2a38'}
              >
                {prompt}
              </button>
            ))}
          </div>
        )}

        {/* Input */}
        <div style={{ flexShrink: 0, padding: '12px 16px', borderTop: '1px solid #1e1e2a', background: '#0d0d14' }}>
          <div style={{ display: 'flex', gap: '8px', alignItems: 'flex-end' }}>
            <textarea
              ref={inputRef}
              value={input}
              onChange={e => setInput(e.target.value)}
              onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage() } }}
              rows={3}
              placeholder="Describe your process... (Enter to send, Shift+Enter for new line)"
              style={{ flex: 1, resize: 'none', fontSize: '0.85rem' }}
            />
            <button
              onClick={sendMessage}
              disabled={!input.trim() || sending}
              style={{ background: '#a855f7', color: '#fff', padding: '10px 12px', borderRadius: '6px', flexShrink: 0, display: 'flex', alignItems: 'center' }}
            >
              <Send size={14} />
            </button>
          </div>
        </div>
      </div>

      {/* Right — step preview */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        {/* Preview header */}
        <div style={{ flexShrink: 0, padding: '14px 24px', borderBottom: '1px solid #1e1e2a', background: '#0d0d14', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div>
            {sopName ? (
              <>
                <h2 style={{ fontSize: '1rem', fontWeight: 700, color: '#e2e2f0', marginBottom: '2px' }}>{sopName}</h2>
                <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                  {sopCategory && <Badge variant="info">{sopCategory}</Badge>}
                  {sopDuration > 0 && <Badge variant="default"><span style={{ fontFamily: 'JetBrains Mono, monospace' }}>~{sopDuration >= 60 ? `${Math.round(sopDuration / 60)}h` : `${sopDuration}m`}</span></Badge>}
                  <Badge variant="default">{steps.length} steps</Badge>
                </div>
              </>
            ) : (
              <p style={{ color: '#5a5a72', fontSize: '0.85rem' }}>SOP preview will appear here as you describe your process</p>
            )}
          </div>
          {steps.length > 0 && (
            <div style={{ display: 'flex', gap: '8px' }}>
              <button
                onClick={addStep}
                style={{ display: 'flex', alignItems: 'center', gap: '6px', background: '#18181f', border: '1px solid #2a2a38', color: '#9090a8', padding: '7px 14px', borderRadius: '6px', fontSize: '0.8rem' }}
              >
                <Plus size={13} /> Add Step
              </button>
              <button
                onClick={exportToBuilder}
                style={{ display: 'flex', alignItems: 'center', gap: '6px', background: exported ? '#22c55e' : '#6366f1', color: '#fff', padding: '7px 16px', borderRadius: '6px', fontSize: '0.8rem', fontWeight: 600, transition: 'background 0.2s' }}
              >
                {exported ? <><CheckCircle2 size={13} /> Exported!</> : <><ExternalLink size={13} /> Open in Builder</>}
              </button>
            </div>
          )}
        </div>

        {/* Step list */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '20px 24px' }}>
          {steps.length === 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', gap: '16px', color: '#5a5a72' }}>
              <Sparkles size={40} color="#2a2a38" />
              <p style={{ fontSize: '0.9rem', textAlign: 'center', maxWidth: 320 }}>
                Chat with BRAID to generate your SOP. Steps will appear here and can be edited inline.
              </p>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              {steps.map((step, idx) => (
                <div
                  key={step.id}
                  style={{
                    background: '#111118',
                    border: `1px solid ${editingStep === step.id ? '#6366f160' : '#1e1e2a'}`,
                    borderRadius: '8px', overflow: 'hidden',
                    transition: 'border-color 0.15s',
                  }}
                >
                  {/* Step header row */}
                  <div
                    style={{
                      padding: '12px 14px', cursor: 'pointer',
                      display: 'flex', alignItems: 'center', gap: '10px',
                      background: editingStep === step.id ? '#18181f' : 'transparent',
                    }}
                    onClick={() => setEditingStep(editingStep === step.id ? null : step.id)}
                  >
                    <GripVertical size={14} color="#5a5a72" style={{ flexShrink: 0 }} />
                    <span style={{ fontSize: '0.68rem', fontFamily: 'JetBrains Mono, monospace', color: '#5a5a72', flexShrink: 0 }}>
                      {String(idx + 1).padStart(2, '0')}
                    </span>
                    <div style={{ width: 8, height: 8, borderRadius: '50%', background: CONNECTOR_COLORS[step.connector] ?? '#5a5a72', flexShrink: 0 }} />
                    <span style={{ flex: 1, fontSize: '0.87rem', fontWeight: 600, color: '#e2e2f0' }}>{step.name}</span>
                    <div style={{ display: 'flex', gap: '6px', flexShrink: 0 }}>
                      {step.requiresSignOff && <Badge variant="warning">Sign-off</Badge>}
                      {step.requiresEvidence && <Badge variant="info">Evidence</Badge>}
                      {step.aiPrompt && <Badge variant="purple">AI</Badge>}
                      <span style={{ fontSize: '0.7rem', color: '#5a5a72', fontFamily: 'JetBrains Mono, monospace' }}>{step.connector}</span>
                    </div>
                    <button
                      onClick={e => { e.stopPropagation(); deleteStep(step.id) }}
                      style={{ background: 'transparent', color: '#5a5a72', padding: '2px 4px', flexShrink: 0 }}
                    >
                      <Trash2 size={13} />
                    </button>
                  </div>

                  {/* Expanded edit form */}
                  {editingStep === step.id && (
                    <div style={{ padding: '16px', borderTop: '1px solid #1e1e2a', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                      <div style={{ gridColumn: '1 / -1' }}>
                        <label style={{ display: 'block', fontSize: '0.72rem', color: '#9090a8', marginBottom: '4px', fontWeight: 600 }}>Step Name</label>
                        <input value={step.name} onChange={e => updateStep(step.id, 'name', e.target.value)} />
                      </div>
                      <div style={{ gridColumn: '1 / -1' }}>
                        <label style={{ display: 'block', fontSize: '0.72rem', color: '#9090a8', marginBottom: '4px', fontWeight: 600 }}>Instruction</label>
                        <textarea rows={2} value={step.description} onChange={e => updateStep(step.id, 'description', e.target.value)} style={{ resize: 'none' }} />
                      </div>
                      <div>
                        <label style={{ display: 'block', fontSize: '0.72rem', color: '#9090a8', marginBottom: '4px', fontWeight: 600 }}>Assigned Role</label>
                        <input value={step.role} onChange={e => updateStep(step.id, 'role', e.target.value)} placeholder="CustomerSuccessManager" />
                      </div>
                      <div>
                        <label style={{ display: 'block', fontSize: '0.72rem', color: '#9090a8', marginBottom: '4px', fontWeight: 600 }}>Connector Type</label>
                        <select value={step.connector} onChange={e => updateStep(step.id, 'connector', e.target.value)}>
                          {['sop_step', 'sop_decision', 'sop_signoff', 'sop_ai_guide', 'email', 'slack', 'salesforce', 'hubspot', 'zendesk', 'docusign', 'google_calendar', 'jira', 'zapier', 'webhook', 'avatar', 'braid'].map(c => (
                            <option key={c} value={c}>{c}</option>
                          ))}
                        </select>
                      </div>
                      <div>
                        <label style={{ display: 'block', fontSize: '0.72rem', color: '#9090a8', marginBottom: '4px', fontWeight: 600 }}>Timeout (minutes)</label>
                        <input type="number" value={step.timeoutMinutes} onChange={e => updateStep(step.id, 'timeoutMinutes', Number(e.target.value))} />
                      </div>
                      <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
                        <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.8rem', color: '#9090a8', cursor: 'pointer' }}>
                          <input type="checkbox" checked={step.requiresSignOff} onChange={e => updateStep(step.id, 'requiresSignOff', e.target.checked)} style={{ width: 'auto' }} />
                          Sign-off
                        </label>
                        <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.8rem', color: '#9090a8', cursor: 'pointer' }}>
                          <input type="checkbox" checked={step.requiresEvidence} onChange={e => updateStep(step.id, 'requiresEvidence', e.target.checked)} style={{ width: 'auto' }} />
                          Evidence
                        </label>
                      </div>
                      <div style={{ gridColumn: '1 / -1' }}>
                        <label style={{ fontSize: '0.72rem', color: '#9090a8', marginBottom: '4px', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '5px' }}>
                          <Sparkles size={11} color="#a855f7" />AI Guidance Prompt (BRAID)
                        </label>
                        <textarea rows={2} value={step.aiPrompt} onChange={e => updateStep(step.id, 'aiPrompt', e.target.value)} placeholder="Instructions for BRAID at this step..." style={{ resize: 'none' }} />
                      </div>
                    </div>
                  )}
                </div>
              ))}

              {/* Export footer */}
              <div style={{
                marginTop: '8px', padding: '16px', background: '#6366f110',
                border: '1px solid #6366f130', borderRadius: '8px',
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
              }}>
                <div>
                  <p style={{ fontSize: '0.85rem', fontWeight: 600, color: '#e2e2f0', marginBottom: '2px' }}>
                    Ready to build this SOP?
                  </p>
                  <p style={{ fontSize: '0.78rem', color: '#9090a8' }}>
                    Export to the Workflow Builder to wire up integrations, conditions, and publish to STARNET.
                  </p>
                </div>
                <div style={{ display: 'flex', gap: '8px' }}>
                  <button
                    onClick={() => { setMessages(prev => [...prev, { role: 'user', content: 'Can you improve this SOP? Look for steps that could benefit from AI automation or better integration choices.', timestamp: new Date().toLocaleTimeString() }]); sendMessage() }}
                    style={{ display: 'flex', alignItems: 'center', gap: '6px', background: '#18181f', border: '1px solid #2a2a38', color: '#a855f7', padding: '8px 14px', borderRadius: '6px', fontSize: '0.8rem' }}
                  >
                    <RefreshCw size={12} /> Improve with AI
                  </button>
                  <button
                    onClick={exportToBuilder}
                    style={{ display: 'flex', alignItems: 'center', gap: '6px', background: exported ? '#22c55e' : '#6366f1', color: '#fff', padding: '8px 18px', borderRadius: '6px', fontWeight: 700, fontSize: '0.85rem' }}
                  >
                    {exported ? <><CheckCircle2 size={13} /> Exported to Builder</> : <><ExternalLink size={13} /> Open in Builder</>}
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
