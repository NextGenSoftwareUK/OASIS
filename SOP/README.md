# OASIS Holonic SOP System

> **Standard Operating Procedures that are executable, auditable, portable, and AI-native.**

---

## What Is This?

The OASIS SOP system replaces static documents and disconnected checklists with **holonic SOPs** — living procedures stored as a graph of holons on the OASIS COSMIC hierarchy, executable through the CRE workflow runtime, auditable on-chain, and authored/guided/improved by AI (BRAID).

Every SOP is an OAPP built from `SOPTemplate`. Every run of an SOP is an immutable `SOPRunHolon`. Every deviation, sign-off, and improvement suggestion is a holon too — making the entire procedure lifecycle queryable, portable, and verifiable.

---

## Folder Structure

```
SOP/
├── README.md              ← you are here
├── BUILD_PLAN.md          ← full technical implementation plan
├── USE_CASES.md           ← SOP flow walkthroughs (onboarding, port ops, compliance)
└── sop_dna/               ← organisation-specific SOP OAPP DNA files (generated)
```

DNA template: `STAR_Templates/star_dna/SOPTemplate.json`
Holon reference: `STAR_Templates/docs/SOP_HOLONS.md`

---

## The Core Idea in 60 Seconds

| Old SOP | Holonic SOP |
|---------|-------------|
| Word doc / Confluence page | `SOPHolon` — versioned, typed, owned by an Avatar |
| Step = bullet point | `SOPStepHolon` — has role, inputs, outputs, connector type |
| "If X then Y" footnote | `SOPDecisionPointHolon` — executable branching logic |
| No run history | `SOPRunHolon` — every instance tracked with timestamps |
| No evidence | `StepCompletionHolon` — evidence hash, Avatar signature |
| Deviation = undiscovered | `SOPDeviationHolon` — AI-detected, severity-tagged |
| SOP in a drawer | STARNET — published, discoverable, forkable by any org |

---

## Key API Endpoints

| Action | Endpoint |
|--------|----------|
| Save workflow | `POST /api/workflow/save` |
| Execute workflow | `POST /api/workflow/execute` |
| Get run status | `GET /api/workflow/execution/{id}` |
| Verify proof holon | `GET /api/workflow/verify/{holonId}` |
| List my workflows | `GET /api/workflow/my` |
| Browse public | `GET /api/workflow/public` |
| AI reasoning (BRAID) | `POST /api/braid/run` |

All endpoints require Bearer JWT from `POST /api/avatar/authenticate`.

---

## Quick Start — Create and Run an SOP

### 1. Build your SOP OAPP

Instantiate from the OAPP template:

```bash
curl -X POST http://localhost:5001/api/OAPPs/create \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @STAR_Templates/star_dna/SOPTemplate.json
```

### 2. Define your SOP in the Workflow Builder

Open `CRE/workflow-builder` and connect nodes:

- **trigger** node → what fires the SOP (webhook, schedule, manual)
- **holon** nodes → create `SOPStepHolon` for each step
- **condition** nodes → branching decision points
- **braid** node → AI guidance/deviation detection
- **webhook** nodes → notify CRM / Slack / ticketing systems
- **avatar** node → request Avatar sign-off on critical steps

### 3. Execute via API

```bash
curl -X POST http://localhost:5001/api/workflow/execute \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "workflowId": "<your-sop-holon-id>", "inputs": { "customerId": "...", "assignedAvatarId": "..." } }'
```

### 4. Verify the proof

```bash
curl http://localhost:5001/api/workflow/verify/<proof-holon-id> \
  -H "Authorization: Bearer $TOKEN"
```

---

## The Three-Layer Product

```
┌─────────────────────────────────────────────────────────┐
│  SOPBuilder  — visual canvas for authoring SOPs         │
│  (CRE workflow-builder, extended with SOP node types)   │
├─────────────────────────────────────────────────────────┤
│  SOPRunner   — task interface for executors             │
│  (guided step-by-step, AI co-pilot, sign-off prompts)  │
├─────────────────────────────────────────────────────────┤
│  SOPIntel    — dashboard for managers                   │
│  (run analytics, deviation heatmaps, AI improvement)   │
└─────────────────────────────────────────────────────────┘
```

---

## STARNET Marketplace

SOPs published to STARNET can be:
- Discovered and forked by any organisation
- Rated by Avatar karma (real trust signal)
- Licensed commercially (treasury connector handles payment)
- Versioned — forks inherit the parent `SOPVersionHolon` chain

Think of it as **GitHub for business processes** — except every run is audited, every deviation is detected, and every improvement is AI-suggested.

---

## Links

- Full DNA: `STAR_Templates/star_dna/SOPTemplate.json`
- Holon reference: `STAR_Templates/docs/SOP_HOLONS.md`
- Workflow builder: `CRE/workflow-builder/`
- CRE architecture: `CRE/Docs/MASTER_IMPLEMENTATION_BRIEF.md`
- Workflow use cases: `CRE/Docs/OASIS_WORKFLOW_LIBRARY_USE_CASES.md`
- Bryan meeting context: `Docs/BRYAN_ZOOM_MEETING_MAR27.md`
