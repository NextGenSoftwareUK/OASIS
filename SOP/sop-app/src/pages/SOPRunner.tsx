import { useState, useEffect, useRef, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { X, ChevronDown } from 'lucide-react'
import {
  CheckCircle, Circle, UploadSimple, PencilSimple, Warning,
  Sparkle, ArrowClockwise, Clock, User, ShieldCheck,
  ChartBar, CaretRight
} from '@phosphor-icons/react'
import { getWorkflow, executeWorkflow, getExecution, completeStepApi, runBraid, type WorkflowDefinition, type WorkflowExecution, type ExecutionStep } from '../api/client'
import { Badge } from '../components/Badge'

const DEMO_SOP: WorkflowDefinition = {
  id: 'demo-onboarding', name: 'Enterprise Customer Onboarding v3',
  description: 'Full onboarding from Closed Won to Go-Live sign-off.', version: '3.0.0',
  steps: [
    { id: 's1', name: 'Send welcome email + kickoff link', connector: 'email',        action: 'send',              inputs: { role: 'CustomerSuccessManager', requiresSignOff: false, requiresEvidence: false }, onFailure: 'abort' },
    { id: 's2', name: 'Technical discovery call',          connector: 'sop_signoff',  action: 'sign',              inputs: { role: 'SolutionsEngineer',      requiresSignOff: true,  requiresEvidence: false }, onFailure: 'abort' },
    { id: 's3', name: 'Complexity score branch',           connector: 'sop_decision', action: 'evaluate',          inputs: { conditionField: 'complexityScore', requiresSignOff: false, requiresEvidence: false }, onFailure: 'abort' },
    { id: 's4', name: 'Setup guide + Zendesk ticket',      connector: 'zendesk',      action: 'create_ticket',     inputs: { role: 'CustomerSuccessManager', requiresSignOff: false, requiresEvidence: false }, onFailure: 'skip' },
    { id: 's5', name: '7-day activation check-in',         connector: 'sop_step',     action: 'complete',          inputs: { role: 'CustomerSuccessManager', requiresSignOff: false, requiresEvidence: true  }, onFailure: 'abort' },
    { id: 's6', name: 'Go-live sign-off (customer)',        connector: 'sop_signoff',  action: 'sign',              inputs: { role: 'CustomerAdmin',          requiresSignOff: true,  requiresEvidence: false }, onFailure: 'abort' },
    { id: 's7', name: 'Update Salesforce + notify Slack',  connector: 'salesforce',   action: 'update_opportunity',inputs: { role: 'System',                 requiresSignOff: false, requiresEvidence: false }, onFailure: 'skip' },
  ],
  inputSchema: {}, isPublic: true,
}

const DEMO_STEPS: ExecutionStep[] = DEMO_SOP.steps.map((s, i) => ({
  stepId: s.id, name: s.name,
  status: i === 0 ? 'success' : i === 1 ? 'success' : i === 2 ? 'running' : 'pending',
}))

const CONNECTOR_LABEL: Record<string, string> = {
  email: 'Email', slack: 'Slack', salesforce: 'Salesforce', hubspot: 'HubSpot',
  zendesk: 'Zendesk', docusign: 'DocuSign', google_calendar: 'Calendar',
  jira: 'Jira', zapier: 'Zapier', sop_step: 'Step', sop_decision: 'Decision',
  sop_signoff: 'Sign-off', sop_ai_guide: 'AI Guide',
}

const AI_DEMOS: Record<string, string> = {
  s1: 'Suggest subject: "Welcome to OASIS — your personalised onboarding starts now." Include the customer use case, three kickoff slots next week, and a Day 1–2 preview. Pre-fill the calendar link with their timezone.',
  s2: 'Focus on: API integration complexity, number of data sources, and team technical maturity. Key question: "Do you have a dedicated engineer?" This determines Simple (score 1–2) vs Enterprise (3–5) path.',
  s3: 'Score ≥ 3 → Enterprise path (8 steps). Score < 3 → Simple path (4 steps). Evaluated automatically from the IntegrationComplexityScore recorded in Step 2.',
  s4: 'Creating Zendesk ticket: priority High, tagged "onboarding-q1-2026". Auto-resolves on go-live completion. I will attach the setup guide specific to their tech stack.',
  s5: 'Target: ≥ 40% of core features activated by Day 7. Below threshold triggers a SOPDeviationHolon and escalation. Benchmark: customers at 40% by Day 7 show 94% 6-month retention.',
  s6: 'Requires Avatar wallet signature from CustomerAdmin. Confirmation text: "By signing, you confirm your team is ready to go live." Once signed, a StepCompletionHolon is created with the signature hash.',
  s7: 'Updating Salesforce Opportunity stage to "Customer" and posting to #cs-wins. SOPAuditHolon created with full run summary, proof holon ID, and all evidence hashes.',
}

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
  const [aiInput, setAiInput] = useState('')
  const fileRef = useRef<HTMLInputElement>(null)

  const activeStep = sop.steps[activeIdx]

  // Load workflow from STAR API when navigating to a real run
  useEffect(() => {
    if (runId && runId !== 'demo-onboarding') getWorkflow(runId).then(setSop).catch(() => {})
  }, [runId])

  // ── Execution polling — syncs step statuses from STAR API every 3s ─────
  useEffect(() => {
    if (!execution?.executionId) return
    const id = setInterval(async () => {
      try {
        const live = await getExecution(execution.executionId)
        setExecution(live)
        // Merge live step statuses into local state
        setSteps(prev => prev.map(s => {
          const liveStep = live.steps.find(ls => ls.stepId === s.stepId)
          return liveStep ? { ...s, status: liveStep.status, output: liveStep.output, error: liveStep.error } : s
        }))
        // Advance cursor to first non-complete step
        const firstPending = live.steps.findIndex(s => s.status === 'running' || s.status === 'pending')
        if (firstPending !== -1) setActiveIdx(firstPending)
        // Stop polling when done
        if (live.status === 'completed' || live.status === 'failed') clearInterval(id)
      } catch { /* API not available — stay in local mode */ }
    }, 3000)
    return () => clearInterval(id)
  }, [execution?.executionId])

  const loadAI = useCallback(async (stepId: string) => {
    if (AI_DEMOS[stepId]) { setAiOutput(AI_DEMOS[stepId]); return }
    setAiLoading(true); setAiOutput('')
    try {
      const res = await runBraid('sop-step-guidance', { stepId, sopName: sop.name }, sop.id)
      setAiOutput(res.output)
    } catch { setAiOutput('STAR API not reachable. In production, BRAID guidance streams here using SOPHolon context and run history.') }
    finally { setAiLoading(false) }
  }, [sop])

  useEffect(() => { if (activeStep) loadAI(activeStep.id) }, [activeIdx, activeStep, loadAI])

  function selectStep(idx: number) { setActiveIdx(idx); setSignedOff(false); setEvidenceFiles([]); setNote('') }

  // Advance step locally (optimistic) + call STAR API when in a live run
  async function completeStep() {
    const stepId = activeStep?.id
    // Optimistic local update
    setSteps(prev => prev.map((s, i) => i === activeIdx ? { ...s, status: 'success' } : s))
    const next = activeIdx + 1
    if (next < sop.steps.length) {
      setSteps(prev => prev.map((s, i) => i === next ? { ...s, status: 'running' } : s))
      setActiveIdx(next)
    }
    setEvidenceFiles([]); setSignedOff(false); setNote('')

    // Persist to STAR API if we have a live execution
    if (execution?.executionId && stepId) {
      try {
        await completeStepApi(execution.executionId, stepId, {
          completedBy: 'avatar-user',
          note: note || undefined,
          evidence: evidenceFiles.length ? evidenceFiles : undefined,
        })
      } catch { /* API not available — local-only */ }
    }
  }

  async function startRun() {
    try {
      const exec = await executeWorkflow(sop.id ?? 'demo', {})
      setExecution(exec)
      // Sync initial step statuses
      if (exec.steps?.length) {
        setSteps(exec.steps)
        const firstRunning = exec.steps.findIndex(s => s.status === 'running' || s.status === 'pending')
        if (firstRunning !== -1) setActiveIdx(firstRunning)
      }
    } catch {
      // STAR API not reachable — enter demo live mode so UI still works
      setExecution({ executionId: `local-${Date.now()}`, workflowId: sop.id ?? 'demo', status: 'running', steps, startedAt: new Date().toISOString() })
    }
  }

  const completed = steps.filter(s => s.status === 'success').length
  const progress = Math.round((completed / steps.length) * 100)
  const needsSignOff = activeStep?.inputs?.requiresSignOff === true || activeStep?.connector === 'sop_signoff' || activeStep?.connector === 'docusign'
  const needsEvidence = activeStep?.connector === 'sop_step' || activeStep?.connector === 'zendesk'

  return (
    <div style={{ height: '100vh', display: 'flex', flexDirection: 'column', background: 'transparent' }}>

      {/* Top bar */}
      <div style={{ flexShrink: 0, padding: '10px 22px', background: 'rgba(8,8,8,0.9)', borderBottom: '0.5px solid rgba(255,255,255,0.07)', display: 'flex', alignItems: 'center', gap: '16px', backdropFilter: 'blur(8px)', position: 'relative', zIndex: 2 }}>
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '5px' }}>
            <h1 style={{ fontFamily: 'var(--font-sans)', fontSize: '0.9rem', fontWeight: 600, color: '#D4D4D4', letterSpacing: '-0.01em' }}>
              {sop.name}
            </h1>
            <Badge variant="default">v{sop.version ?? '1.0.0'}</Badge>
            {execution && <Badge variant="active">Live</Badge>}
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <span style={{ fontSize: '0.73rem', color: '#505050' }}>Step {activeIdx + 1} of {sop.steps.length}</span>
            <div style={{ width: 160, height: 3, background: '#1F1F1F', borderRadius: 2, overflow: 'hidden' }}>
              <div style={{ width: `${progress}%`, height: '100%', background: 'var(--accent, #2DD4BF)', borderRadius: 2, transition: 'width 0.3s ease' }} />
            </div>
            <span style={{ fontSize: '0.73rem', fontWeight: 600, color: '#707070', fontFamily: "'JetBrains Mono', monospace" }}>{progress}%</span>
          </div>
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          {!execution && <button onClick={startRun} className="btn-primary" style={{ fontSize: '0.78rem' }}>Start Live Run</button>}
          <button onClick={() => navigate('/intel')} className="btn-secondary" style={{ display: 'flex', alignItems: 'center', gap: '5px', fontSize: '0.78rem' }}>
            <ChartBar size={12} /> Analytics
          </button>
        </div>
      </div>

      {/* 3-col layout */}
      <div style={{ flex: 1, display: 'grid', gridTemplateColumns: '220px 1fr 280px', overflow: 'hidden' }}>

        {/* Step list */}
        <div style={{ borderRight: '0.5px solid rgba(255,255,255,0.07)', overflowY: 'auto', background: 'transparent' }}>
          <div style={{ padding: '10px 14px 6px', fontSize: '0.6rem', textTransform: 'uppercase', letterSpacing: '0.09em', color: '#505050', fontWeight: 700 }}>Steps</div>
          {sop.steps.map((step, idx) => {
            const exec = steps[idx]
            const isActive = idx === activeIdx
            const isDone = exec?.status === 'success'
            const isRunning = exec?.status === 'running'
            return (
              <button key={step.id} onClick={() => selectStep(idx)} style={{ width: '100%', textAlign: 'left', padding: '8px 14px', display: 'flex', alignItems: 'flex-start', gap: '8px', background: isActive ? 'rgba(45,212,191,0.05)' : 'transparent', borderLeft: `2px solid ${isActive ? '#2DD4BF' : 'transparent'}`, cursor: 'pointer', border: 'none', transition: 'background 0.1s' }}
                onMouseEnter={e => { if (!isActive) e.currentTarget.style.background = '#141414' }}
                onMouseLeave={e => { if (!isActive) e.currentTarget.style.background = 'transparent' }}>
                <div style={{ flexShrink: 0, marginTop: 2 }}>
                  {isDone ? <CheckCircle size={13} color="#22C55E" weight="fill" />
                    : isRunning ? <div style={{ width: 13, height: 13, borderRadius: '50%', border: '2px solid #E4E4E4', borderTopColor: 'transparent', animation: 'spin 0.8s linear infinite' }} />
                    : <Circle size={13} color="#383838" />}
                </div>
                <div style={{ overflow: 'hidden' }}>
                  <div style={{ fontSize: '0.78rem', color: isActive ? '#2DD4BF' : isDone ? '#505050' : '#888888', fontWeight: isActive ? 500 : 400, lineHeight: 1.35, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 160 }}>
                    {step.name}
                  </div>
                  <span style={{ fontSize: '0.62rem', color: '#383838', marginTop: 2, display: 'block' }}>
                    {CONNECTOR_LABEL[step.connector] ?? step.connector}
                  </span>
                </div>
              </button>
            )
          })}
          <style>{`@keyframes spin{to{transform:rotate(360deg)}}`}</style>
        </div>

        {/* Step card */}
        <div style={{ overflowY: 'auto', padding: '24px 28px', background: '#111111' }}>
          {showDeviation && (
            <div style={{ background: 'rgba(251,191,36,0.06)', border: '1px solid rgba(251,191,36,0.2)', borderRadius: '7px', padding: '12px 16px', marginBottom: '18px', display: 'flex', gap: '10px', alignItems: 'flex-start' }}>
              <Warning size={14} color="#FBBF24" weight="fill" style={{ flexShrink: 0, marginTop: 1 }} />
              <div>
                <div style={{ fontSize: '0.82rem', fontWeight: 600, color: '#FBBF24', marginBottom: 2 }}>Deviation detected</div>
                <div style={{ fontSize: '0.77rem', color: '#707070' }}>Step exceeded the 60-minute timeout. A SOPDeviationHolon has been logged. Review in SOPIntel.</div>
              </div>
              <button onClick={() => setShowDeviation(false)} style={{ background: 'transparent', color: '#383838', padding: '2px', marginLeft: 'auto' }}><X size={12} /></button>
            </div>
          )}

          {activeStep && (
            <div style={{ background: '#181818', borderRadius: '8px', border: '1px solid #1F1F1F', overflow: 'hidden' }}>
              <div style={{ padding: '20px 24px', borderBottom: '1px solid #1A1A1A' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '10px' }}>
                  <span style={{ fontSize: '0.63rem', fontFamily: "'JetBrains Mono', monospace", color: '#505050', fontWeight: 600 }}>
                    {(activeIdx + 1).toString().padStart(2, '0')}
                  </span>
                  <span style={{ fontSize: '0.7rem', color: '#666666', background: '#1F1F1F', border: '1px solid #1F1F1F', padding: '1px 7px', borderRadius: 4, fontWeight: 500 }}>
                    {CONNECTOR_LABEL[activeStep.connector] ?? activeStep.connector}
                  </span>
                  {needsSignOff && <Badge variant="warning"><PencilSimple size={8} style={{ marginRight: 3 }} />Sign-off</Badge>}
                  {needsEvidence && <Badge variant="active"><UploadSimple size={8} style={{ marginRight: 3 }} />Evidence</Badge>}
                </div>
                <h2 style={{ fontFamily: 'var(--font-serif)', fontSize: '1.2rem', fontWeight: 400, color: '#DCDCDC', letterSpacing: '-0.01em', marginBottom: '10px' }}>
                  {activeStep.name}
                </h2>
                <div style={{ display: 'flex', gap: '16px' }}>
                  {activeStep.inputs?.role && (
                    <span style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: '0.77rem', color: '#666666' }}>
                      <User size={11} color="#383838" /> {String(activeStep.inputs.role)}
                    </span>
                  )}
                  <span style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: '0.77rem', color: '#666666' }}>
                    <Clock size={11} color="#383838" /> Timeout: 60 min
                  </span>
                </div>
              </div>

              <div style={{ padding: '20px 24px' }}>
                <div style={{ background: '#1F1F1F', border: '1px solid #1F1F1F', borderRadius: '6px', padding: '12px 14px', marginBottom: '18px' }}>
                  <p style={{ fontSize: '0.85rem', color: '#707070', lineHeight: 1.7 }}>
                    {activeStep.connector === 'sop_decision'
                      ? 'Decision point — routes automatically based on the complexity score from Step 2. Review BRAID analysis in the co-pilot panel.'
                      : activeStep.connector === 'salesforce'
                        ? 'Automated step — Salesforce and Slack notifications run automatically. No action required.'
                        : 'Complete the work for this step. Use BRAID guidance, upload evidence if required, then sign off to advance.'}
                  </p>
                </div>

                {needsEvidence && (
                  <div style={{ marginBottom: '18px' }}>
                    <label style={{ display: 'block', fontSize: '0.77rem', fontWeight: 500, color: '#707070', marginBottom: '7px' }}>Evidence upload</label>
                    <div
                      onDragOver={e => { e.preventDefault(); setDragging(true) }}
                      onDragLeave={() => setDragging(false)}
                      onDrop={e => { e.preventDefault(); setDragging(false); setEvidenceFiles(p => [...p, ...Array.from(e.dataTransfer.files).map(f => f.name)]) }}
                      onClick={() => fileRef.current?.click()}
                      style={{ border: `1px dashed ${dragging ? '#505050' : '#2A2A2A'}`, borderRadius: '6px', padding: '18px', textAlign: 'center', cursor: 'pointer', background: dragging ? '#1D1D1D' : 'transparent', transition: 'all 0.12s' }}
                    >
                      <UploadSimple size={16} color="#383838" style={{ margin: '0 auto 6px' }} />
                      <p style={{ fontSize: '0.79rem', color: '#666666' }}>Drag files or <span style={{ color: '#D4D4D4', textDecoration: 'underline' }}>click to upload</span></p>
                      <p style={{ fontSize: '0.68rem', color: '#505050', marginTop: '3px' }}>SHA-256 hash stored in StepCompletionHolon</p>
                      <input ref={fileRef} type="file" multiple style={{ display: 'none' }} onChange={e => setEvidenceFiles(p => [...p, ...Array.from(e.target.files ?? []).map(f => f.name)])} />
                    </div>
                    {evidenceFiles.length > 0 && (
                      <div style={{ marginTop: '8px', display: 'flex', flexWrap: 'wrap', gap: '6px' }}>
                        {evidenceFiles.map(f => (
                          <div key={f} style={{ display: 'flex', alignItems: 'center', gap: 5, background: 'rgba(34,197,94,0.08)', border: '1px solid rgba(34,197,94,0.2)', borderRadius: '4px', padding: '3px 8px' }}>
                            <CheckCircle size={10} color="#22C55E" weight="fill" />
                            <span style={{ fontSize: '0.72rem', color: '#4ADE80', fontWeight: 500 }}>{f}</span>
                            <button onClick={() => setEvidenceFiles(p => p.filter(x => x !== f))} style={{ background: 'transparent', color: '#383838', padding: '0 2px' }}><X size={10} /></button>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                )}

                {needsSignOff && (
                  <div style={{ marginBottom: '18px' }}>
                    <label style={{ display: 'block', fontSize: '0.77rem', fontWeight: 500, color: '#707070', marginBottom: '7px' }}>Avatar sign-off</label>
                    <div style={{ background: signedOff ? 'rgba(34,197,94,0.06)' : '#1D1D1D', border: `1px solid ${signedOff ? 'rgba(34,197,94,0.2)' : '#2A2A2A'}`, borderRadius: '6px', padding: '14px', transition: 'all 0.2s' }}>
                      {signedOff
                        ? <div style={{ display: 'flex', alignItems: 'center', gap: 7, color: '#4ADE80', fontSize: '0.85rem', fontWeight: 500 }}><ShieldCheck size={14} weight="fill" /> Signed — signature recorded in StepCompletionHolon</div>
                        : <>
                            <p style={{ fontSize: '0.79rem', color: '#666666', marginBottom: '12px' }}>This step requires an Avatar wallet signature before advancing.</p>
                            <button onClick={() => setSignedOff(true)} className="btn-primary" style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: '0.79rem' }}>
                              <ShieldCheck size={12} /> Sign with Avatar Wallet
                            </button>
                          </>
                      }
                    </div>
                  </div>
                )}

                <div style={{ marginBottom: '20px' }}>
                  <label style={{ display: 'block', fontSize: '0.77rem', fontWeight: 500, color: '#707070', marginBottom: '6px' }}>Notes (optional)</label>
                  <textarea value={note} onChange={e => setNote(e.target.value)} rows={3} placeholder="Any context for this completion…" style={{ resize: 'none' }} />
                </div>

                <div style={{ display: 'flex', gap: '8px' }}>
                  <button onClick={completeStep} disabled={needsSignOff && !signedOff} className="btn-primary" style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', padding: '10px 16px', fontSize: '0.85rem' }}>
                    <CheckCircle size={13} weight="bold" />
                    {activeIdx === sop.steps.length - 1 ? 'Complete SOP Run' : 'Complete Step'}
                    <CaretRight size={13} weight="bold" />
                  </button>
                  <button onClick={() => setShowDeviation(true)} style={{ background: 'rgba(251,191,36,0.06)', border: '1px solid rgba(251,191,36,0.2)', color: '#FBBF24', padding: '10px 12px', borderRadius: '6px', display: 'flex', alignItems: 'center', gap: 4, fontSize: '0.79rem', fontWeight: 500 }}>
                    <Warning size={12} weight="bold" /> Flag
                  </button>
                  <button style={{ background: 'transparent', border: '1px solid #1F1F1F', color: '#505050', padding: '10px 12px', borderRadius: '6px', display: 'flex', alignItems: 'center', gap: 4, fontSize: '0.79rem' }}>
                    <ChevronDown size={12} /> Skip
                  </button>
                </div>

                {execution && (
                  <div style={{ marginTop: '12px', padding: '8px 12px', background: 'rgba(96,165,250,0.06)', border: '1px solid rgba(96,165,250,0.15)', borderRadius: '5px', fontSize: '0.7rem', color: '#60A5FA', fontFamily: "'JetBrains Mono', monospace" }}>
                    Live · {execution.executionId}
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        {/* BRAID co-pilot */}
        <div style={{ borderLeft: '1px solid #1D1D1D', display: 'flex', flexDirection: 'column', background: '#111111' }}>
          <div style={{ flexShrink: 0, padding: '12px 14px', borderBottom: '1px solid #1A1A1A', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <div style={{ width: 24, height: 24, borderRadius: '5px', background: 'rgba(167,139,250,0.1)', border: '1px solid rgba(167,139,250,0.2)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Sparkle size={12} color="#A78BFA" weight="fill" />
              </div>
              <div>
                <div style={{ fontFamily: 'var(--font-sans)', fontWeight: 600, fontSize: '0.78rem', color: '#D4D4D4', letterSpacing: '0.04em' }}>BRAID</div>
                <div style={{ fontSize: '0.62rem', color: '#505050' }}>AI co-pilot · step {activeIdx + 1}</div>
              </div>
            </div>
            <button onClick={() => loadAI(activeStep?.id ?? '')} style={{ background: 'transparent', color: '#505050', padding: '4px' }}><ArrowClockwise size={11} weight="bold" /></button>
          </div>

          <div style={{ flex: 1, overflowY: 'auto', padding: '12px' }}>
            {aiLoading ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', padding: '4px' }}>
                {[80, 100, 68, 90, 58].map((w, i) => (
                  <div key={i} style={{ height: 9, borderRadius: 4, background: '#1F1F1F', width: `${w}%`, animation: `pulse 1.4s ease-in-out ${i * 0.1}s infinite` }} />
                ))}
                <style>{`@keyframes pulse{0%,100%{opacity:.25}50%{opacity:.7}}`}</style>
              </div>
            ) : (
              <>
                <div style={{ background: '#181818', border: '1px solid #1F1F1F', borderRadius: '6px', padding: '12px', marginBottom: '12px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: '0.6rem', color: '#A78BFA', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', marginBottom: 8 }}>
                    <Sparkle size={8} weight="fill" /> BRAID
                  </div>
                  <p style={{ fontSize: '0.8rem', color: '#707070', lineHeight: 1.7 }}>{aiOutput}</p>
                </div>

                <p style={{ fontSize: '0.58rem', textTransform: 'uppercase', letterSpacing: '0.09em', color: '#383838', fontWeight: 700, marginBottom: '6px' }}>Quick ask</p>
                {['What could go wrong?', 'Example output', 'How long should this take?', 'What does next step need?'].map(q => (
                  <button key={q} onClick={() => setAiOutput(`[BRAID] ${q}\n\nIn production, BRAID streams a response using SOPHolon context and run history.`)}
                    style={{ width: '100%', textAlign: 'left', padding: '7px 10px', marginBottom: '3px', background: 'transparent', border: '1px solid #1D1D1D', borderRadius: '5px', fontSize: '0.76rem', color: '#666666', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'space-between', transition: 'border-color 0.1s, color 0.1s' }}
                    onMouseEnter={e => { e.currentTarget.style.borderColor = '#383838'; e.currentTarget.style.color = '#E4E4E4' }}
                    onMouseLeave={e => { e.currentTarget.style.borderColor = '#1D1D1D'; e.currentTarget.style.color = '#666666' }}>
                    {q} <CaretRight size={9} weight="bold" />
                  </button>
                ))}

                <div style={{ marginTop: '12px', background: '#181818', border: '1px solid #1F1F1F', borderRadius: '6px', padding: '11px' }}>
                  <p style={{ fontSize: '0.58rem', color: '#383838', textTransform: 'uppercase', letterSpacing: '0.09em', fontWeight: 700, marginBottom: '8px' }}>Historical</p>
                  {[['Avg duration', '38 min', '#888888'], ['Deviation rate', '12%', '#FBBF24'], ['AI usage', '78%', '#22C55E']].map(([l, v, c]) => (
                    <div key={l as string} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '5px' }}>
                      <span style={{ fontSize: '0.76rem', color: '#505050' }}>{l}</span>
                      <span style={{ fontSize: '0.76rem', fontWeight: 600, color: c, fontFamily: "'JetBrains Mono', monospace" }}>{v}</span>
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>

          <div style={{ flexShrink: 0, padding: '10px', borderTop: '1px solid #1D1D1D' }}>
            <div style={{ display: 'flex', gap: '6px' }}>
              <input value={aiInput} onChange={e => setAiInput(e.target.value)} placeholder="Ask BRAID anything…"
                style={{ flex: 1, fontSize: '0.79rem', padding: '7px 10px', background: '#181818', border: '1px solid #1F1F1F' }}
                onKeyDown={e => { if (e.key === 'Enter' && aiInput.trim()) { const q = aiInput; setAiInput(''); setAiOutput(`[BRAID] Answering: "${q}"`) } }} />
              <button onClick={() => { if (aiInput.trim()) { const q = aiInput; setAiInput(''); setAiOutput(`[BRAID] Answering: "${q}"`) } }}
                style={{ background: 'rgba(167,139,250,0.12)', border: '1px solid rgba(167,139,250,0.2)', color: '#A78BFA', padding: '7px 10px', borderRadius: '5px', flexShrink: 0 }}>
                <Sparkle size={12} weight="fill" />
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
