# OASIS Holonic SOP — Use Cases & Flow Walkthroughs

> Each use case below shows the SOP as a graph of holons, the CRE connector nodes used, the AI touchpoints, and how external systems are wired in.

---

## Use Case 1 — Customer Success Onboarding (Bryan's Context)

**Industry:** B2B SaaS / Automation Consulting  
**Pain points solved:** Inconsistent onboarding, no visibility on where customers get stuck, no audit trail for accountability, AI can't help because there's no structured context.

### SOP Definition (`SOPHolon`)

```
Name:                  "Enterprise Customer Onboarding v3"
EstimatedDurationMins: 2880  (48 hours)
RequiredRoles:         ["CustomerSuccessManager", "SolutionsEngineer", "CustomerAdmin"]
Status:                "Active"
Version:               "3.0.0"
```

### Step Graph

```
[trigger: webhook]           ← CRM fires when deal stage = "Closed Won"
        │
        ▼
[holon: SOPStepHolon]        STEP 1 — Send welcome email + kickoff booking link
  Role: CustomerSuccessManager
  Connector: webhook → calendar (auto-creates 3 kickoff slots)
  AI (braid): "Draft a personalised welcome email using customer's industry and use case"
  Output: CalendarEventId, EmailSentAt
        │
        ▼
[holon: SOPStepHolon]        STEP 2 — Technical discovery call
  Role: SolutionsEngineer
  Connector: avatar (sign-off required — marks call complete)
  AI (braid): "Summarise discovery answers and identify integration complexity score"
  Input: DiscoveryFormUrl
  Output: IntegrationComplexityScore (1–5), DiscoveryNotes
        │
        ▼
[condition: SOPDecisionPoint] BRANCH — Complexity score
  ├─ [score ≤ 2] → SIMPLE PATH (4 steps)
  └─ [score ≥ 3] → ENTERPRISE PATH (8 steps)
        │
        ▼ (SIMPLE PATH example)
[holon: SOPStepHolon]        STEP 3a — Self-serve setup guide sent
  Connector: webhook → Zendesk (create onboarding ticket)
  AI: "Generate a customised setup guide PDF from template"
        │
        ▼
[holon: SOPStepHolon]        STEP 4a — 7-day check-in call
  Role: CustomerSuccessManager
  Connector: avatar (sign-off)
  AI: "Review product usage data and flag if activation < 40%"
  Input: ProductUsageRate
        │
        ▼
[condition: SOPDecisionPoint] BRANCH — Activation
  ├─ [usage ≥ 40%] → STEP 5: Go-live confirmation → proof holon created
  └─ [usage < 40%] → DEVIATION flagged → escalation SOP triggered
        │
        ▼
[holon: SOPStepHolon]        STEP 5 — Go-live sign-off
  Role: CustomerAdmin (customer signs)
  Connector: avatar (wallet signature required)
  AI: "Generate go-live summary report with key milestones and next 90-day plan"
  Output: GoLiveDate, CustomerSignatureHash
        │
        ▼
[holon: SOPAuditHolon]       Auto-created: immutable audit log
[webhook]                    Push to Salesforce: Opportunity stage → "Customer"
[webhook]                    Push to Slack: #cs-wins channel notification
```

### Holons Created Per Run

| Holon | Count |
|-------|-------|
| SOPRunHolon | 1 |
| StepCompletionHolon | 5–10 (one per step) |
| SOPDeviationHolon | 0–N (AI-detected) |
| SOPAuditHolon | 1 |
| SOPAnalyticsHolon | Updated |

### What the Manager Sees (SOPIntel)

- % of onboardings reaching go-live in < 48 hours
- Which step has the highest deviation rate
- Which CSM has the highest activation rates
- AI suggestion: "Step 3a has 34% deviation on time-to-send — consider automating email send"

---

## Use Case 2 — Port Gate-In & Customs Clearance (Port OS / LFG)

**Industry:** Port Operations / Logistics  
**Pain points solved:** Gate delays not traced to root cause, customs holds not linked to cargo origin data, no cross-system view of a container's journey.

### SOP Definition

```
Name:                  "Container Gate-In & Customs Pre-Clearance"
EstimatedDurationMins: 60
RequiredRoles:         ["GateOperator", "CustomsOfficer", "YardPlanner"]
CrossTemplateFKs:      ContainerId → PortOSTemplate.ContainerHolon
                       ConsignmentId → AgriTraceabilityTemplate.ConsignmentHolon (if EUDR)
```

### Step Graph

```
[trigger: webhook]           ← OCR gate camera fires VehicleRegistration + ContainerId
        │
        ▼
[holon]                      STEP 1 — Validate container against booking
  Connector: holon (lookup ContainerHolon, ShipmentHolon)
  AI: "Check if ContainerId matches any active ShipmentHolon. Flag if mismatch."
  Output: BookingMatch (bool), HazmatFlag (bool)
        │
        ▼
[condition]                  BRANCH — HazmatFlag
  ├─ [true]  → HAZMAT PATH → alert + specialist SOP triggered
  └─ [false] → STANDARD PATH
        │
        ▼
[holon]                      STEP 2 — EUDR compliance check
  Connector: holon (lookup ConsignmentHolon.EudrRequired)
  AI: "Cross-reference ConsignmentHolon with AgriTraceabilityTemplate. Return RED/AMBER/GREEN."
  Output: EudrStatus ("RED" | "AMBER" | "GREEN")
        │
        ▼
[condition]                  BRANCH — EudrStatus
  ├─ [RED]   → customs hold → CustomsTriageHolon created → customs SOP triggered
  ├─ [AMBER] → manual review required → avatar sign-off
  └─ [GREEN] → proceed to yard assignment
        │
        ▼
[holon]                      STEP 3 — Yard slot assignment
  Connector: holon (create YardPositionHolon)
  AI: "Recommend optimal yard block based on dwell time, vessel ETD, and block capacity."
  Output: YardBlock, SlotCode
        │
        ▼
[holon]                      STEP 4 — Gate-in confirmation
  Connector: avatar (GateOperator sign-off)
  Connector: webhook → TOS (transmit gate-in event)
  Output: GateInTimestamp
        │
        ▼
[holon: SOPAuditHolon]       Immutable record of entire gate-in process
```

### Cross-Template Power

Because `ContainerHolon` exists in `PortOSTemplate` and `ConsignmentHolon` exists in `AgriTraceabilityTemplate`, this SOP can traverse the entire supply chain graph in a single query — farm-of-origin → ship → gate — with no ETL, no sync job, and a full EUDR audit trail.

---

## Use Case 3 — New Employee Onboarding (HR / People Ops)

**Industry:** Any company with 10+ employees  
**Pain points solved:** Inconsistent day-1 experience, IT provisioning delays not visible, compliance training completion not audited.

### Step Graph (condensed)

```
[trigger: webhook]      ← HRIS fires on "new hire confirmed" event
[holon]   STEP 1        Send offer letter for signature (DocuSign webhook)
[holon]   STEP 2        IT provisioning request (webhook → Jira/ServiceNow ticket)
[condition]             IT ticket resolved? Poll webhook until closed.
[holon]   STEP 3        Day-1 welcome pack + equipment check (avatar sign-off)
[holon]   STEP 4        Compliance training sent (LMS webhook)
[condition]             Training complete? Poll LMS completion webhook.
[holon]   STEP 5        Manager 1:1 scheduled (calendar webhook)
[avatar]  STEP 6        30-day check-in — employee Avatar signs satisfaction survey
[holon]   AUDIT         SOPAuditHolon created, HR records updated
```

**AI touchpoints:**
- Step 1: Personalise welcome message to role and team
- Step 4: Recommend training modules based on role
- Step 6: Analyse satisfaction survey and flag retention risk

---

## Use Case 4 — Investment Due Diligence (RWA / BusinessEntity templates)

**Industry:** Venture Capital / Real Estate / Family Offices  
**Pain points solved:** DD is done differently every time, no institutional memory, sign-off chain not audited.

### Step Graph (condensed)

```
[trigger: manual]       Partner initiates DD for a BusinessEntityHolon
[holon]   STEP 1        AI document ingestion (upload IM, financials, cap table)
           AI: "Extract key metrics: ARR, burn, runway, team size, legal red flags"
[holon]   STEP 2        ESG screening (lookup ConservationImpactTemplate holons if applicable)
[condition]             ESG score >= threshold?
[holon]   STEP 3        Legal review step (avatar sign-off — external counsel)
[holon]   STEP 4        Financial model review (avatar sign-off — CFO)
[holon]   STEP 5        IC memo generation
           AI: "Draft investment committee memo from all StepCompletionHolon evidence"
[avatar]  STEP 6        IC vote — each IC member Avatar signs approve/reject
[holon]   STEP 7        Term sheet issued (DocuSign webhook)
[holon]   AUDIT         SOPAuditHolon — immutable DD record on-chain
           Links: BusinessEntityHolon.Id, all IC Avatar.Ids, DocumentHashes[]
```

**STARNET angle:** A VC firm publishes their DD SOP template. Other firms fork it, customise for their thesis, and each run is auditable by LPs. Regulatory compliance (FCA, SEC) has a cryptographic audit trail per deal.

---

## Use Case 5 — ESG / Conservation Impact Reporting (ConservationImpactTemplate)

**Industry:** Conservation NGOs, Carbon Markets, Sustainability Teams  
**Pain points solved:** Impact data is unverifiable, reporting takes weeks, donors can't see real outcomes.

### Step Graph (condensed)

```
[trigger: schedule]     Monthly — fires automatically
[holon]   STEP 1        Pull SurveyDataHolon for the period (holon connector)
[holon]   STEP 2        AI analysis
           AI: "Compute biodiversity delta, carbon sequestration, and habitat recovery rate vs baseline"
[holon]   STEP 3        Flag any ProjectHolon below target (SOPDeviationHolon if < KPI)
[condition]             Any deviations?
  ├─ [yes] → Ranger sign-off + corrective action plan created
  └─ [no]  → proceed to report generation
[braid]   STEP 4        Generate impact report (AI drafts from holon data)
[avatar]  STEP 5        Executive director signs report (wallet signature)
[webhook] STEP 6        Publish to donor dashboard + carbon registry API
[holon]   AUDIT         SOPAuditHolon — on-chain proof of every data point
```

**Why holons are essential here:** Every biodiversity measurement, every sensor reading, every survey response is a holon with a canonical ID. The report is not a summary of copies — it is a traversal of the living data graph. The on-chain proof holon means carbon credits issued from this SOP are verifiably backed by real, timestamped, Avatar-signed field data.

---

## Cross-SOP Patterns

### Pattern 1 — SOP Chaining
A completed `SOPRunHolon` can be a trigger for another SOP. Example: Customer onboarding complete → quarterly business review SOP starts automatically.

### Pattern 2 — Deviation Escalation
Any `SOPDeviationHolon` with severity `Critical` can trigger an escalation SOP — a separate procedure specifically for handling process failures.

### Pattern 3 — Parallel Steps
Steps with no data dependencies can run in parallel (multiple active `StepCompletionHolon` records, same `SOPRunHolon.Id`, different `StepId`). Example: IT provisioning and compliance training both start on Day 1.

### Pattern 4 — Cross-Template FK Navigation
Any SOP step can look up holons from other OAPPs using their FK convention (`HolonId` fields). The gate-in SOP navigates `PortOSTemplate` and `AgriTraceabilityTemplate` in the same run. The DD SOP navigates `BusinessEntityTemplate` and `ConservationImpactTemplate`.

### Pattern 5 — STARNET Fork-and-Customise
Organisation A publishes "Enterprise Sales SOP v2" on STARNET. Organisation B forks it, removes 2 steps for their smaller deal size, adds a custom compliance step. Both run on the same `SOPHolon` schema — their runs are separate, their analytics are separate, but their lineage is shared (`ParentSOPId`).
