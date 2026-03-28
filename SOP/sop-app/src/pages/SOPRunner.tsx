import { useState, useEffect, useRef, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  CheckCircle2, Circle, ChevronRight, Upload, Pen,
  AlertTriangle, Sparkles, ChevronDown, X, RefreshCw,
  Clock, User, ArrowRight, Shield
} from 'lucide-react'
import { getWorkflow, executeWorkflow, runBraid, type WorkflowDefinition, type WorkflowExecution, type ExecutionStep } from '../api/client'
import { Badge } from '../components/Badge'

// ─── Demo data for UI preview when not connected ────────────────────────────

const DEMO_SOP: WorkflowDefinition = {
  id: 'demo-onboarding',
  name: 'Enterprise Customer Onboarding v3',
  description: 'Full onboarding flow from Closed Won through to Go-Live sign-off.',
  version: '3.0.0',
  steps: [
    { id: 's1', name: 'Send welcome email + kickoff link', connector: 'email', action: 'send', inputs: { role: 'CustomerSuccessManager', requiresSignOff: false, requiresEvidence: false }, onFailure: 'abort' },
    { id: 's2', name: 'Technical discovery call', connector: 'sop_signoff', action: 'sign', inputs: { role: 'SolutionsEngineer', requiresSignOff: true, requiresEvidence: false }, onFailure: 'abort' },
    { id: 's3', name: 'Complexity score branch', connector: 'sop_decision', action: 'evaluate', inputs: { conditionField: 'complexityScore', requiresSignOff: false, requiresEvidence: false }, onFailure: 'abort' },
    { id: 's4', name: 'Setup guide + Zendesk ticket', connector: 'zendesk', action: 'create_ticket', inputs: { role: 'CustomerSuccessManager', requiresSignOff: false, requiresEvidence: false }, onFailure: 'skip' },
    { id: 's5', name: '7-day activation check-in', connector: 'sop_step', action: 'complete', inputs: { role: 'CustomerSuccessManager', requiresSignOff: false, requiresEvidence: true }, onFailure: 'abort' },
    { id: 's6', name: 'Go-live sign-off (customer)', connector: 'sop_signoff', action: 'sign', inputs: { role: 'CustomerAdmin', requiresSignOff: true, requiresEvidence: false }, onFailure: 'abort' },
    { id: 's7', name: 'Update Salesforce + notify #cs-wins', connector: 'salesforce', action: 'update_opportunity', inputs: { role: 'System', requiresSignOff: false, requiresEvidence: false }, onFailure: 'skip' },
  ],
  inputSchema: {},
  isPublic: true,
}

const DEMO_STEPS: ExecutionStep[] = DEMO_SOP.steps.map((s, i) => ({
  stepId: s.id,
  name: s.name,
  status: i === 0 ? 'success' : i === 1 ? 'success' : i === 2 ? 'running' : 'pending',
}))

const CONNECTOR_COLORS: Record<string, string> = {
  email: '#f59e0b', slack: '#36c037', salesforce: '#1e40af', hubspot: '#f97316',
  zendesk: '#7c3aed', docusign: '#16a34a', google_calendar: '#db2777', jira: '#2563eb',
  zapier: '#ca8a04', sop_step: '#ea580c', sop_decision: '#dc2626', sop_signoff: '#059669',
  sop_ai_guide: '#9333ea', webhook: '#475569', avatar: '#0e7490', braid: '#9333ea',
}

const AI_DEMOS: Record<string, string> = {
  's1': 'Based on the customer profile, I suggest the following subject line: "Welcome to OASIS — your personalised onboarding starts now." Key points to include in the welcome email: (1) their specific use case with OASIS, (2) the 3 kickoff slots for next week, (3) a brief overview of what the first 48 hours will look like. The calendar link should pre-fill with their timezone.',
  's2': 'For the discovery call with this customer, focus on: API integration complexity, number of existing data sources to migrate, team size and technical maturity. Key question: "Do you have a dedicated engineering resource for the integration?" This will determine whether we route to the Simple (score 1–2) or Enterprise (score 3–5) path.',
  's3': 'Evaluating the complexity score from your discovery call notes. Score of 3 or higher routes to the Enterprise path with 8 steps. Score of 1–2 routes to the Simple path with 4 steps. I will automatically determine the branch based on the IntegrationComplexityScore field from Step 2.',
  's4': 'Creating a Zendesk onboarding ticket with priority "High" and tagging it "onboarding-q1-2026". The ticket will auto-resolve when the go-live step completes. I\'ll also attach the setup guide specific to their tech stack.',
  's5': 'Checking activation data: target is ≥40% of core features activated by Day 7. If below threshold, I will flag a SOPDeviationHolon and suggest escalating to the Enterprise path. Current benchmark: customers who hit 40% activation by Day 7 have 94% retention at 6 months.',
  's6': 'The go-live sign-off requires an OASIS Avatar wallet signature from the customer\'s admin. I\'ve prepared the confirmation text: "By signing, you confirm your team is ready to go live and that all discovery action items have been completed." Once signed, a StepCompletionHolon is created with the signature hash.',
  's7': 'Updating Salesforce Opportunity stage to "Customer" and posting to #cs-wins Slack channel. I\'ll also create an SOPAuditHolon with the full run summary, proof holon ID, and all evidence hashes for regulatory reference.',
}

// ─── Component ──────────────────────────────────────────────────────────────

export function SOPRunner() {
  const { runId } = useParams()
  const navigate = useNavigate()
  const [sop, setSop] = useState<WorkflowDefinition>(DEMO_SOP)
  const [execution, setExecution] = useState<WorkflowExecution | null>(null)
  const [steps, setSteps] = useState<ExecutionStep[]>(DEMO_STEPS)
  const [activeIdx, setActiveIdx] = useState(2)
  const [aiOutput, setAiOutput] = useState(AI_DEMOS['s3'] ?? '')
  const [aiLoading, setAiLoading] = useState(false)
  const [evidenceFiles, setEvidenceFiles] = useState<string[]>([])
  const [signedOff, setSignedOff] = useState(false)
  const [note, setNote] = useState('')
  const [showDeviation, setShowDeviation] = useState(false)
  const [dragging, setDragging] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  const activeStep = sop.steps[activeIdx]

  useEffect(() => {
    if (runId && runId !== 'demo-onboarding') {
      getWorkflow(runId).then(setSop).catch(() => {})
    }
  }, [runId])

  const loadAI = useCallback(async (stepId: string) => {
    if (AI_DEMOS[stepId]) { setAiOutput(AI_DEMOS[stepId]); return }
    setAiLoading(true)
    setAiOutput('')
    try {
      const res = await runBraid('sop-step-guidance', { stepId, sopName: sop.name }, sop.id)
      setAiOutput(res.output)
    } catch {
      setAiOutput('Unable to reach BRAID — STAR API may not be running. In production, AI guidance would appear here in real time.')
    } finally {
      setAiLoading(false)
    }
  }, [sop])

  useEffect(() => {
    if (activeStep) loadAI(activeStep.id)
  }, [activeIdx, activeStep, loadAI])

  function handleSelectStep(idx: number) {
    setActiveIdx(idx)
    setSignedOff(false)
    setEvidenceFiles([])
    setNote('')
  }

  function handleComplete() {
    setSteps(prev => prev.map((s, i) => i === activeIdx ? { ...s, status: 'success' } : s))
    if (activeIdx < sop.steps.length - 1) {
      const next = activeIdx + 1
      setActiveIdx(next)
      setSteps(prev => prev.map((s, i) => i === next ? { ...s, status: 'running' } : s))
    }
    setEvidenceFiles([])
    setSignedOff(false)
    setNote('')
    if (activeIdx === sop.steps.length - 2) setShowDeviation(false)
  }

  async function handleStart() {
    try {
      const exec = await executeWorkflow(sop.id ?? 'demo', {})
      setExecution(exec)
    } catch {
      // demo mode — just advance
    }
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setDragging(false)
    const files = Array.from(e.dataTransfer.files).map(f => f.name)
    setEvidenceFiles(prev => [...prev, ...files])
  }

  const completedCount = steps.filter(s => s.status === 'success').length
  const progress = Math.round((completedCount / steps.length) * 100)

  const needsSignOff = activeStep?.inputs?.requiresSignOff === true ||
    activeStep?.connector === 'sop_signoff' || activeStep?.connector === 'docusign'
  const needsEvidence = activeStep?.connector === 'sop_step' || activeStep?.connector === 'zendesk'

  function connectorColor(connector?: string) {
    return CONNECTOR_COLORS[connector ?? ''] ?? '#9090a8'
  }

  return (
    <div style={{ height: 'calc(100vh - 52px)', display: 'flex', flexDirection: 'column' }}>
      {/* Top bar */}
      <div style={{
        flexShrink: 0, padding: '14px 24px',
        background: '#0d0d14', borderBottom: '1px solid #1e1e2a',
        display: 'flex', alignItems: 'center', gap: '16px',
      }}>
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '6px' }}>
            <h1 style={{ fontSize: '1rem', fontWeight: 700, color: '#e2e2f0' }}>{sop.name}</h1>
            <Badge variant="info">v{sop.version ?? '1.0.0'}</Badge>
            {execution && <Badge variant="success">Run active</Badge>}
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <span style={{ fontSize: '0.75rem', color: '#9090a8' }}>
              Step {activeIdx + 1} of {sop.steps.length}
            </span>
            {/* Progress bar */}
            <div style={{ flex: 1, maxWidth: 300, height: '4px', background: '#1e1e2a', borderRadius: '2px', overflow: 'hidden' }}>
              <div style={{ width: `${progress}%`, height: '100%', background: '#6366f1', borderRadius: '2px', transition: 'width 0.4s ease' }} />
            </div>
            <span style={{ fontSize: '0.75rem', fontFamily: 'JetBrains Mono, monospace', color: '#6366f1' }}>{progress}%</span>
          </div>
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          {!execution && (
            <button
              onClick={handleStart}
              style={{ background: '#6366f1', color: '#fff', padding: '7px 16px', borderRadius: '6px', fontSize: '0.8rem', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <ArrowRight size={13} /> Start Live Run
            </button>
          )}
          <button
            onClick={() => navigate('/intel')}
            style={{ background: '#18181f', border: '1px solid #2a2a38', color: '#9090a8', padding: '7px 14px', borderRadius: '6px', fontSize: '0.8rem' }}
          >
            View Analytics
          </button>
        </div>
      </div>

      {/* Main 3-column layout */}
      <div style={{ flex: 1, display: 'grid', gridTemplateColumns: '220px 1fr 320px', overflow: 'hidden' }}>

        {/* Left — Step list */}
        <div style={{ borderRight: '1px solid #1e1e2a', overflowY: 'auto', background: '#0d0d14' }}>
          <div style={{ padding: '12px 10px 6px', fontSize: '0.65rem', textTransform: 'uppercase', letterSpacing: '0.08em', color: '#5a5a72', fontWeight: 600 }}>
            Steps
          </div>
          {sop.steps.map((step, idx) => {
            const execStep = steps[idx]
            const isActive = idx === activeIdx
            const isDone = execStep?.status === 'success'
            const isRunning = execStep?.status === 'running'
            return (
              <button
                key={step.id}
                onClick={() => handleSelectStep(idx)}
                style={{
                  width: '100%', textAlign: 'left', padding: '10px 12px',
                  display: 'flex', alignItems: 'flex-start', gap: '8px',
                  background: isActive ? '#1e1e2a' : 'transparent',
                  borderLeft: isActive ? '2px solid #6366f1' : '2px solid transparent',
                  cursor: 'pointer', border: 'none', transition: 'background 0.1s',
                }}
              >
                <div style={{ flexShrink: 0, marginTop: '2px' }}>
                  {isDone
                    ? <CheckCircle2 size={14} color="#22c55e" />
                    : isRunning
                      ? <div style={{ width: 14, height: 14, borderRadius: '50%', border: '2px solid #6366f1', borderTopColor: 'transparent', animation: 'spin 0.8s linear infinite' }} />
                      : <Circle size={14} color="#2a2a38" />
                  }
                </div>
                <div>
                  <div style={{ fontSize: '0.78rem', color: isActive ? '#e2e2f0' : '#9090a8', fontWeight: isActive ? 600 : 400, lineHeight: 1.3 }}>
                    {step.name}
                  </div>
                  <div style={{ fontSize: '0.65rem', marginTop: '2px', display: 'flex', alignItems: 'center', gap: '4px' }}>
                    <span style={{ width: 6, height: 6, borderRadius: '50%', background: connectorColor(step.connector), display: 'inline-block', flexShrink: 0 }} />
                    <span style={{ color: '#5a5a72' }}>{step.connector}</span>
                  </div>
                </div>
              </button>
            )
          })}
          <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
        </div>

        {/* Center — Current step */}
        <div style={{ overflowY: 'auto', padding: '28px 32px' }}>
          {showDeviation && (
            <div style={{
              background: '#f59e0b15', border: '1px solid #f59e0b40', borderRadius: '8px',
              padding: '12px 16px', marginBottom: '20px',
              display: 'flex', alignItems: 'flex-start', gap: '10px',
            }}>
              <AlertTriangle size={16} color="#f59e0b" style={{ flexShrink: 0, marginTop: 2 }} />
              <div>
                <div style={{ fontSize: '0.82rem', fontWeight: 600, color: '#f59e0b', marginBottom: '2px' }}>Deviation Detected</div>
                <div style={{ fontSize: '0.8rem', color: '#9090a8' }}>
                  This step has taken longer than the 60-minute timeout. A SOPDeviationHolon has been created. Review in SOPIntel or resolve below.
                </div>
              </div>
              <button onClick={() => setShowDeviation(false)} style={{ background: 'transparent', color: '#5a5a72', marginLeft: 'auto', padding: '2px' }}>
                <X size={14} />
              </button>
            </div>
          )}

          {activeStep && (
            <>
              {/* Step header */}
              <div style={{ marginBottom: '24px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '8px' }}>
                  <span style={{ fontSize: '0.7rem', color: '#5a5a72', fontFamily: 'JetBrains Mono, monospace' }}>
                    STEP {activeIdx + 1}
                  </span>
                  <div style={{ width: 6, height: 6, borderRadius: '50%', background: connectorColor(activeStep.connector) }} />
                  <span style={{ fontSize: '0.75rem', color: connectorColor(activeStep.connector) }}>{activeStep.connector}</span>
                </div>
                <h2 style={{ fontSize: '1.3rem', fontWeight: 700, color: '#e2e2f0', marginBottom: '10px' }}>
                  {activeStep.name}
                </h2>
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap' }}>
                  {activeStep.inputs?.role && (
                    <span style={{ display: 'flex', alignItems: 'center', gap: '5px', fontSize: '0.78rem', color: '#9090a8' }}>
                      <User size={12} />
                      {String(activeStep.inputs.role)}
                    </span>
                  )}
                  <span style={{ display: 'flex', alignItems: 'center', gap: '5px', fontSize: '0.78rem', color: '#9090a8' }}>
                    <Clock size={12} />
                    Timeout: 60 min
                  </span>
                  {needsSignOff && <Badge variant="warning"><Pen size={9} style={{ marginRight: 3 }} />Sign-off required</Badge>}
                  {needsEvidence && <Badge variant="info"><Upload size={9} style={{ marginRight: 3 }} />Evidence required</Badge>}
                </div>
              </div>

              {/* Step description */}
              <div style={{
                background: '#111118', border: '1px solid #1e1e2a', borderRadius: '8px',
                padding: '16px', marginBottom: '20px',
              }}>
                <p style={{ fontSize: '0.88rem', color: '#c0c0d8', lineHeight: 1.65 }}>
                  {activeStep.connector === 'sop_decision'
                    ? 'This is a decision point. The SOP will automatically route to the correct branch based on the complexity score from the previous step. Review the AI analysis in the co-pilot panel.'
                    : activeStep.connector === 'salesforce'
                      ? 'Automatically update the Salesforce opportunity stage to "Customer" and trigger the #cs-wins Slack notification. This step runs automatically — no manual action required.'
                      : 'Follow the instructions for this step. Use the AI co-pilot for guidance, upload any required evidence, and sign off when complete.'
                  }
                </p>
              </div>

              {/* Evidence upload */}
              {needsEvidence && (
                <div style={{ marginBottom: '20px' }}>
                  <label style={{ display: 'block', fontSize: '0.78rem', fontWeight: 600, color: '#9090a8', marginBottom: '8px' }}>
                    Evidence Upload
                  </label>
                  <div
                    onDragOver={e => { e.preventDefault(); setDragging(true) }}
                    onDragLeave={() => setDragging(false)}
                    onDrop={handleDrop}
                    onClick={() => fileRef.current?.click()}
                    style={{
                      border: `2px dashed ${dragging ? '#6366f1' : '#2a2a38'}`,
                      borderRadius: '8px', padding: '24px',
                      textAlign: 'center', cursor: 'pointer',
                      background: dragging ? '#6366f110' : '#18181f',
                      transition: 'all 0.15s',
                    }}
                  >
                    <Upload size={20} color="#5a5a72" style={{ margin: '0 auto 8px' }} />
                    <p style={{ fontSize: '0.82rem', color: '#9090a8' }}>
                      Drag files here or <span style={{ color: '#6366f1' }}>click to upload</span>
                    </p>
                    <p style={{ fontSize: '0.72rem', color: '#5a5a72', marginTop: '4px' }}>
                      SHA-256 hash stored in StepCompletionHolon
                    </p>
                    <input ref={fileRef} type="file" multiple style={{ display: 'none' }} onChange={e => {
                      const files = Array.from(e.target.files ?? []).map(f => f.name)
                      setEvidenceFiles(prev => [...prev, ...files])
                    }} />
                  </div>
                  {evidenceFiles.length > 0 && (
                    <div style={{ marginTop: '8px', display: 'flex', flexWrap: 'wrap', gap: '6px' }}>
                      {evidenceFiles.map(f => (
                        <div key={f} style={{ display: 'flex', alignItems: 'center', gap: '6px', background: '#22c55e15', border: '1px solid #22c55e30', borderRadius: '4px', padding: '4px 10px' }}>
                          <CheckCircle2 size={11} color="#22c55e" />
                          <span style={{ fontSize: '0.75rem', color: '#22c55e' }}>{f}</span>
                          <button onClick={() => setEvidenceFiles(p => p.filter(x => x !== f))} style={{ background: 'transparent', color: '#5a5a72', padding: '0 2px' }}>
                            <X size={11} />
                          </button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* Sign-off */}
              {needsSignOff && (
                <div style={{ marginBottom: '20px' }}>
                  <label style={{ display: 'block', fontSize: '0.78rem', fontWeight: 600, color: '#9090a8', marginBottom: '8px' }}>
                    Avatar Sign-Off
                  </label>
                  <div style={{ background: '#18181f', border: `1px solid ${signedOff ? '#22c55e40' : '#2a2a38'}`, borderRadius: '8px', padding: '16px' }}>
                    {signedOff ? (
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#22c55e', fontSize: '0.85rem' }}>
                        <Shield size={16} />
                        <span>Signed — wallet signature recorded in StepCompletionHolon</span>
                      </div>
                    ) : (
                      <>
                        <p style={{ fontSize: '0.82rem', color: '#9090a8', marginBottom: '12px' }}>
                          This step requires an Avatar wallet signature before the SOP can advance.
                        </p>
                        <button
                          onClick={() => setSignedOff(true)}
                          style={{ background: '#059669', color: '#fff', padding: '8px 18px', borderRadius: '6px', fontWeight: 600, fontSize: '0.82rem', display: 'flex', alignItems: 'center', gap: '6px' }}
                        >
                          <Shield size={13} /> Sign with Avatar Wallet
                        </button>
                      </>
                    )}
                  </div>
                </div>
              )}

              {/* Notes */}
              <div style={{ marginBottom: '24px' }}>
                <label style={{ display: 'block', fontSize: '0.78rem', fontWeight: 600, color: '#9090a8', marginBottom: '6px' }}>
                  Notes (optional)
                </label>
                <textarea
                  value={note}
                  onChange={e => setNote(e.target.value)}
                  rows={3}
                  placeholder="Add any context or notes for this step completion..."
                  style={{ resize: 'vertical' }}
                />
              </div>

              {/* Action buttons */}
              <div style={{ display: 'flex', gap: '10px' }}>
                <button
                  onClick={handleComplete}
                  disabled={needsSignOff && !signedOff}
                  style={{
                    flex: 1, background: '#6366f1', color: '#fff',
                    padding: '10px 20px', borderRadius: '8px',
                    fontWeight: 700, fontSize: '0.9rem',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px',
                  }}
                >
                  <CheckCircle2 size={15} />
                  {activeIdx === sop.steps.length - 1 ? 'Complete SOP Run' : 'Complete Step'}
                  <ChevronRight size={15} />
                </button>
                <button
                  onClick={() => setShowDeviation(true)}
                  style={{ background: '#18181f', border: '1px solid #2a2a38', color: '#f59e0b', padding: '10px 14px', borderRadius: '8px', display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.8rem' }}
                >
                  <AlertTriangle size={13} /> Flag
                </button>
                <button
                  style={{ background: '#18181f', border: '1px solid #2a2a38', color: '#9090a8', padding: '10px 14px', borderRadius: '8px', display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.8rem' }}
                >
                  <ChevronDown size={13} /> Skip
                </button>
              </div>

              {execution && (
                <div style={{ marginTop: '16px', padding: '10px 14px', background: '#22c55e10', border: '1px solid #22c55e20', borderRadius: '6px', fontSize: '0.75rem', color: '#22c55e', fontFamily: 'JetBrains Mono, monospace' }}>
                  Live run active · Execution ID: {execution.executionId}
                </div>
              )}
            </>
          )}
        </div>

        {/* Right — AI Co-pilot */}
        <div style={{
          borderLeft: '1px solid #1e1e2a', display: 'flex', flexDirection: 'column',
          background: '#0d0d14',
        }}>
          <div style={{
            flexShrink: 0, padding: '14px 16px',
            borderBottom: '1px solid #1e1e2a',
            display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '7px' }}>
              <Sparkles size={14} color="#a855f7" />
              <span style={{ fontSize: '0.8rem', fontWeight: 600, color: '#e2e2f0' }}>BRAID Co-pilot</span>
            </div>
            <button
              onClick={() => loadAI(activeStep?.id ?? '')}
              style={{ background: 'transparent', color: '#5a5a72', padding: '4px', display: 'flex' }}
              title="Refresh AI guidance"
            >
              <RefreshCw size={13} />
            </button>
          </div>

          <div style={{ flex: 1, overflowY: 'auto', padding: '16px' }}>
            {aiLoading ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {[80, 100, 65, 90, 55].map((w, i) => (
                  <div key={i} style={{ height: 12, borderRadius: 6, background: '#1e1e2a', width: `${w}%`, animation: 'pulse 1.5s ease-in-out infinite', animationDelay: `${i * 0.1}s` }} />
                ))}
                <style>{`@keyframes pulse { 0%,100% { opacity:0.3 } 50% { opacity:0.7 } }`}</style>
              </div>
            ) : (
              <>
                <div style={{
                  background: '#a855f710', border: '1px solid #a855f720', borderRadius: '8px',
                  padding: '14px', marginBottom: '12px',
                  fontSize: '0.82rem', color: '#d0c0e8', lineHeight: 1.7,
                }}>
                  {aiOutput}
                </div>

                {/* Quick actions */}
                <div style={{ marginBottom: '12px' }}>
                  <p style={{ fontSize: '0.68rem', textTransform: 'uppercase', letterSpacing: '0.08em', color: '#5a5a72', marginBottom: '8px', fontWeight: 600 }}>
                    Quick Ask
                  </p>
                  {[
                    'What could go wrong at this step?',
                    'Show me an example output',
                    'How long should this take?',
                    'What does the next step need?',
                  ].map(q => (
                    <button
                      key={q}
                      onClick={() => setAiOutput(`[BRAID response to "${q}"] — In production, BRAID would answer this in real time using the SOPHolon context, current run state, and historical StepCompletionHolon data from previous runs.`)}
                      style={{
                        width: '100%', textAlign: 'left', padding: '8px 10px', marginBottom: '4px',
                        background: '#18181f', border: '1px solid #2a2a38', borderRadius: '6px',
                        fontSize: '0.76rem', color: '#9090a8', cursor: 'pointer',
                        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                        transition: 'border-color 0.1s',
                      }}
                      onMouseEnter={e => e.currentTarget.style.borderColor = '#a855f740'}
                      onMouseLeave={e => e.currentTarget.style.borderColor = '#2a2a38'}
                    >
                      {q} <ChevronRight size={11} />
                    </button>
                  ))}
                </div>

                {/* Historical context */}
                <div style={{
                  background: '#18181f', border: '1px solid #1e1e2a', borderRadius: '8px',
                  padding: '12px', fontSize: '0.75rem',
                }}>
                  <p style={{ color: '#5a5a72', marginBottom: '8px', fontSize: '0.68rem', textTransform: 'uppercase', letterSpacing: '0.07em', fontWeight: 600 }}>
                    Historical Context
                  </p>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                    <span style={{ color: '#9090a8' }}>Avg duration for this step</span>
                    <span style={{ color: '#e2e2f0', fontFamily: 'JetBrains Mono, monospace' }}>38 min</span>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                    <span style={{ color: '#9090a8' }}>Deviation rate</span>
                    <span style={{ color: '#f59e0b', fontFamily: 'JetBrains Mono, monospace' }}>12%</span>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <span style={{ color: '#9090a8' }}>Runs with AI guidance</span>
                    <span style={{ color: '#22c55e', fontFamily: 'JetBrains Mono, monospace' }}>78%</span>
                  </div>
                </div>
              </>
            )}
          </div>

          {/* AI input */}
          <div style={{ flexShrink: 0, padding: '12px', borderTop: '1px solid #1e1e2a' }}>
            <div style={{ display: 'flex', gap: '6px' }}>
              <input
                placeholder="Ask BRAID anything..."
                style={{ flex: 1, fontSize: '0.8rem', padding: '8px 10px' }}
                onKeyDown={e => {
                  if (e.key === 'Enter' && e.currentTarget.value) {
                    const q = e.currentTarget.value
                    e.currentTarget.value = ''
                    setAiOutput(`[BRAID] Answering: "${q}"\n\nIn production, this would call POST /api/braid/run with the step context holon and return a real-time response from Anthropic Claude.`)
                  }
                }}
              />
              <button style={{ background: '#a855f7', color: '#fff', padding: '8px 12px', borderRadius: '6px', flexShrink: 0 }}>
                <Sparkles size={13} />
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
