# OASIS Holonic SOP — Technical Build Plan

> **Goal:** Transform SOPs from static documents into living, executable, auditable, AI-native procedures — stored as holons, run via the CRE workflow engine, and published on STARNET.

---

## Phase 0 — Foundation (Already Live)

The following infrastructure exists and can be used today:

| Layer | Component | Status |
|-------|-----------|--------|
| Persistence | HolonManager — multi-provider (MongoDB, Solana, IPFS) | ✅ Live |
| Workflow runtime | `WorkflowController` — `api/workflow` (save/execute/verify) | ✅ Live |
| AI reasoning | `BraidController` — `api/braid` (Anthropic/OpenAI) | ✅ Live |
| Visual builder | `CRE/workflow-builder` — React Flow canvas with 22 connector types | ✅ Live |
| Identity | Avatar JWT, KarmaController, multi-chain wallets | ✅ Live |
| OAPP registry | STAR WebAPI, starnet-manifest.json | ✅ Live |

The SOP system is a **configuration and extension** of this stack — not a rebuild.

---

## Phase 1 — SOPTemplate OAPP DNA

**Deliverable:** `STAR_Templates/star_dna/SOPTemplate.json`  
**Status:** Complete (see file)

### Zome Structure

```
SOPTemplate
├── SOPZome
│   ├── SOPHolon               ← procedure definition
│   └── SOPVersionHolon        ← immutable history snapshot
├── StepZome
│   ├── SOPStepHolon           ← individual instruction
│   ├── SOPDecisionPointHolon  ← branching logic node
│   └── SOPConditionBranchHolon ← named branch from a decision point
├── RunZome
│   ├── SOPRunHolon            ← execution instance
│   ├── StepCompletionHolon    ← evidence + Avatar signature
│   └── SOPDeviationHolon      ← AI-detected anomaly
├── AuditZome
│   ├── SOPAuditHolon          ← immutable audit log entry
│   └── SOPAnalyticsHolon      ← aggregated run metrics
└── IntegrationZome
    ├── IntegrationTriggerHolon ← what fires the SOP
    ├── WebhookConfigHolon      ← external system connection
    └── ExternalSystemLinkHolon ← CRM / ticketing / calendar bindings
```

---

## Phase 2 — SOPBuilder (Workflow Canvas Extension)

**Deliverable:** Extended `CRE/workflow-builder` with SOP-specific node types  
**Effort:** ~3–4 days

### New Node Types to Add to `ConnectorType`

```typescript
| 'sop'          // create/update SOPHolon
| 'sop_step'     // create SOPStepHolon
| 'sop_decision' // create SOPDecisionPointHolon
| 'sop_sign'     // Avatar sign-off (extends 'avatar' connector)
| 'sop_ai'       // BRAID guidance injection (extends 'braid' connector)
```

### Builder UX Requirements

1. **Template Mode** — drag SOP nodes from palette to canvas, connect with typed edges (sequential, branching, parallel)
2. **Role Assignment** — each step node has a role picker (pulls from Avatar organization structure)
3. **Input/Output Schema** — define what data enters/exits each step; enforced at runtime
4. **Decision Builder** — condition node with named branches and fallback
5. **Integration Picker** — configure webhooks inline (CRM field, Slack channel, calendar event)
6. **AI Prompt Field** — each step can carry a BRAID prompt; AI fills this in SOPRunner

### Canvas → Holon Mapping

When "Publish SOP" is clicked:
1. Builder calls `POST /api/workflow/save` with the `WorkflowDefinition`
2. ONODE persists as `SOPHolon` (WorkflowId = HolonId)
3. Each step node → `SOPStepHolon` with `SOPId` FK
4. Each decision node → `SOPDecisionPointHolon` with branches → `SOPConditionBranchHolon[]`
5. Trigger config → `IntegrationTriggerHolon` + `WebhookConfigHolon`
6. Builder returns `SOPHolon.Id` for distribution

---

## Phase 3 — SOPRunner (Execution Interface)

**Deliverable:** Web app for the person executing an SOP step-by-step  
**Effort:** ~5–7 days

### View States

```
[SOP Overview]
  ↓ Start Run
[Step 1 — AI briefing panel]
  ↓ Complete / Upload evidence
[Decision Point — choose branch]
  ↓
[Step N — sign-off required]
  → Avatar wallet signs → StepCompletionHolon created
  ↓
[Run Complete — proof holon generated]
```

### Key Interactions

- **AI co-pilot panel:** Each step renders its BRAID prompt. BRAID receives `SOPHolon` context + current `SOPRunHolon` state and returns contextual guidance in real time.
- **Evidence upload:** Attach files (stored via FilesController), hash stored in `StepCompletionHolon.EvidenceHash`.
- **Avatar sign-off:** Critical steps require wallet signature — `StepCompletionHolon.AvatarSignature` set.
- **Deviation alert:** If AI detects step took >2σ from mean duration, or evidence missing, `SOPDeviationHolon` created automatically.
- **External system push:** On step complete, runner calls `WebhookConfigHolon.Url` with completion payload.

### Runtime API Flow

```
POST /api/workflow/execute { workflowId, inputs }
→ executionId returned

Poll GET /api/workflow/execution/{executionId}
→ { status, steps[], currentStepId }

On user action: POST /api/workflow/execution/{executionId}/step/{stepId}/complete
→ creates StepCompletionHolon, advances run

POST /api/braid/run { holonId: SOPHolonId, prompt: stepBraidPrompt }
→ AI guidance text returned

On run complete: GET /api/workflow/verify/{proofHolonId}
→ tamper-proof audit certificate
```

---

## Phase 4 — SOPIntel (Analytics Dashboard)

**Deliverable:** Manager-facing dashboard for SOP performance  
**Effort:** ~3–4 days

### Dashboard Panels

| Panel | Data Source |
|-------|-------------|
| Run completion rate | `SOPAnalyticsHolon.CompletionRate` |
| Avg step duration | `SOPAnalyticsHolon.AvgStepDurationSeconds` |
| Deviation heatmap | `SOPDeviationHolon[]` grouped by `StepId` |
| On-time compliance | `SOPRunHolon.EndedAt - StartedAt` vs `SOPHolon.EstimatedDurationMinutes` |
| Avatar performance | `StepCompletionHolon.CompletedByAvatarId` aggregated by role |
| AI improvement suggestions | BRAID batch job over `SOPDeviationHolon[]` |
| Audit trail | `SOPAuditHolon[]` per SOP, exportable |

### AI Improvement Loop

```
Nightly BRAID batch:
1. Load all SOPDeviationHolon for the SOP
2. Cluster by StepId and DeviationType
3. Generate suggested rewrites for affected SOPStepHolon fields
4. Store as SOPVersionHolon (status: "AIProposed")
5. Manager reviews and approves → SOPHolon.Version incremented
```

---

## Phase 5 — STARNET Publishing & Marketplace

**Effort:** ~1 day per SOP template published

### Publishing Flow

1. Author marks `SOPRunHolon.SourcePublicOnSTARNET = true`
2. STAR API indexes `SOPHolon` as a discoverable OAPP template
3. Any org can browse → fork → instantiate their own SOP OAPP from the template
4. Forks inherit `ParentSOPId` → version lineage is traceable

### Monetisation Options

- **Free:** publish with MIT licence, anyone forks
- **Licensed:** treasury connector on fork action — payment required before instantiation
- **Karma-gated:** minimum organisational karma score required to fork a high-compliance SOP

---

## Integration Reference

### Supported External Systems (via webhook connector)

| System | Trigger Event | Action |
|--------|---------------|--------|
| Salesforce / HubSpot | Step completed | Update CRM record / opportunity stage |
| Zendesk / Freshdesk | SOP started | Create ticket; on complete, resolve |
| Slack / Teams | Deviation detected | Post alert to channel |
| Google Calendar / Outlook | Step started | Create calendar event / reminder |
| DocuSign / HelloSign | Sign-off step | Send signature request; wait for completion |
| Zapier / Make | Any event | Fan out to hundreds of downstream apps |
| Custom API | Any event | Configurable payload via `WebhookConfigHolon` |

### AI Provider Support (via BraidController)

| Provider | Use |
|----------|-----|
| Anthropic Claude | Primary — reasoning, guidance, deviation analysis |
| OpenAI GPT-4 | Fallback |
| Local/custom LLM | Configurable via `OASIS_DNA` |

---

## Build Priority Order

```
✅ Phase 0 — Foundation (already live)
🔲 Phase 1 — SOPTemplate DNA (this session)
🔲 Phase 2 — SOPBuilder node types (Week 1)
🔲 Phase 3 — SOPRunner interface (Week 1–2)
🔲 Phase 4 — SOPIntel dashboard (Week 2)
🔲 Phase 5 — STARNET marketplace (Week 3)
```

---

## Success Metrics

- Time to create an SOP: < 30 minutes (vs days for Word doc + approval chain)
- Run completion rate visibility: real-time (vs never)
- Deviation detection: automated (vs never discovered)
- SOP portability: any org can fork from STARNET (vs emailing a Word doc)
- Audit certificate: cryptographic, instant (vs filing cabinet)
