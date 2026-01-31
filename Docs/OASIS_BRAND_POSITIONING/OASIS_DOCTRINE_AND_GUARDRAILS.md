# OASIS Doctrine & Guardrails

(Derived from the Founding Canon)

---

> **Status:** Binding  
> **Applies to:** All kernel, brand, economic, partnership, and ecosystem decisions  
> **Purpose:** Define how OASIS behaves when adherence to the Canon becomes inconvenient

---

## 1. Purpose of This Doctrine

The Founding Canon defines what OASIS believes to be true. This Doctrine defines how OASIS must act when:

- trade-offs appear attractive,
- pressure builds to "just make an exception,"
- success tempts erosion of constraints,
- or failure tempts abandonment of principle.

If the Canon is the constitution, this Doctrine is the case law.

---

## 2. Non-Negotiable Axioms

These are hard constraints. Violating any one of them invalidates the decision.

### 2.1 Identity Precedes Infrastructure

- No kernel feature may depend on a single execution environment.
- No identity may derive its authority from a provider, chain, or platform.
- If a feature requires a "primary" host to function correctly, it is not kernel-safe.

> **Test:** "If this provider disappears, does identity still exist without reinterpretation?" If the answer is no → reject.

### 2.2 The Kernel Is Non-Commercial

- The kernel cannot be monetized.
- The kernel cannot be tokenized.
- The kernel cannot be used as leverage for revenue capture.

All economic activity must occur at the edge.

> **Test:** "Does this introduce economic pressure that could influence kernel constraints?" If yes → reject or relocate to edge.

### 2.3 No Silent Authority

- No system, process, or actor may resolve ambiguity without leaving a trace.
- Convenience is never justification for silent resolution.
- "Users won't notice" is not an acceptable rationale.

> **Test:** "Can an external observer inspect how and why a resolution occurred?" If not → reject.

### 2.4 Ambiguity Is Preferable to False Certainty

- Explicit ambiguity is a valid state.
- Forced convergence is a design failure, not a feature.
- "Picking one" for simplicity is not allowed at the kernel level.

> **Test:** "Are we collapsing uncertainty purely to make the system easier to reason about?" If yes → reject.

### 2.5 Interfaces Expose Meaning, Not Control

- Interfaces may interpret.
- Interfaces may translate.
- Interfaces may not orchestrate, enforce, or decide.

No interface becomes canonical by default.

> **Test:** "Does this interface become a gatekeeper if widely adopted?" If yes → redesign or reject.

---

## 3. Kernel vs Edge Decision Rules

### 3.1 What Belongs in the Kernel

Only elements that:

- define identity invariants,
- preserve persistence,
- expose conflict,
- or constrain authority.

If removing the component would break meaning, it may belong in the kernel.

### 3.2 What Must Stay at the Edge

Anything that:

- optimizes UX,
- accelerates adoption,
- generates revenue,
- introduces incentives,
- or encodes values or preferences.

If removing the component would only reduce utility, it belongs at the edge.

### 3.3 Default Bias

When uncertain:

- Assume edge, not kernel
- Assume optional, not required
- Assume replaceable, not permanent

The kernel grows only under proof, never optimism.

---

## 4. Stewardship Rules

### 4.1 Stewardship Is Provisional

- No steward role is permanent.
- No steward role is unquestionable.
- All steward decisions must be documented.

Disagreement is not failure. Suppressed disagreement is.

### 4.2 Documentation Over Resolution

- If consensus cannot be reached, document divergence.
- Preserve dissent.
- Prefer reversible decisions.

**Rule:** If a decision cannot be revisited without breaking continuity, it must be rejected.

### 4.3 Capture Detection

Any of the following triggers mandatory review:

- single funder >50% of kernel support
- single infrastructure provider becomes de facto required
- single partner dictates roadmap constraints
- branding language shifts toward inevitability or dominance

If capture risk is identified and cannot be mitigated → halt progress.

---

## 5. Brand & Narrative Guardrails

### 5.1 Language Discipline

**Avoid:** platform, "ecosystem we own", "the future of", inevitable, revolution

**Prefer:** constraints, conditions, experiments, "may fail", "optional participation"

If marketing language increases certainty, it violates the Canon.

### 5.2 Visual Discipline

- No diagrams implying control, flow ownership, or central orchestration.
- No logos, chains, or products near the core.
- Visual complexity increases outward, never inward.

If a diagram makes OASIS look powerful at the center, it is wrong.

---

## 6. Partnerships & Collaborations

### 6.1 Allowed

- Edge-level collaborations
- Experimental projects
- Tooling and SDK integrations
- Independent communities

### 6.2 Forbidden

- Partnerships that require kernel changes
- Agreements that imply endorsement or primacy
- Deals that trade neutrality for distribution

**Rule:** If a partner needs the kernel to bend, the partnership is invalid.

---

## 7. Economic Design Guardrails

- Tokens, pricing, and incentives may exist only at the edge.
- Economic success must not feed back into kernel authority.
- Kernel evolution cannot depend on token price, market cycles, or investor timelines.

If kernel decisions become correlated with economic outcomes → stop.

---

## 8. Failure Is a Valid Outcome

OASIS is allowed to end.

If:

- identity cannot persist without authority,
- reconciliation requires silent resolution,
- ambiguity cannot be tolerated,
- or stewardship becomes irreversible,

then the project has fulfilled its purpose by discovering its limits.

Continuation under false assumptions is failure.

---

## 9. Doctrine Enforcement Rule

When a decision is challenged, the question is not: "Does this help us move forward?"

The question is: "Does this preserve the conditions under which forward movement is meaningful?"

If the answer is no, forward movement is irrelevant.

---

## Closing

> This Doctrine exists to make OASIS harder to run, not easier.

If it slows progress, that is evidence it is working. If it becomes uncomfortable, it should be consulted more often. If it is ignored, the project has already drifted.
