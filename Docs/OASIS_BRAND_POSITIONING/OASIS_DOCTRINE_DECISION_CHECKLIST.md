# OASIS — Doctrine Decision Checklist (v1)

## Purpose

This checklist exists to evaluate whether a proposal, change, partnership, feature, narrative, or dependency is compatible with the OASIS Founding Canon and Doctrine.

If a proposal cannot be evaluated with this checklist, it is not ready.

---

## 0. Decision Context

- Item under review:
- Type: (Kernel / Edge / Interface / Partnership / Brand / Funding / Infra)
- Proposed by:
- Cycle:
- Decision deadline:

---

## 1. Identity Invariance Test (Hard Gate)

Question: If this component disappears, fails, or is removed, does Holon identity still exist without reinterpretation?

- [ ] Yes — identity persists unchanged
- [ ] No — identity collapses or must be redefined
- [ ] Unclear — requires interpretation or narrative repair

> **If "No" → REJECT | If "Unclear" → ESCALATE (kernel risk)**

Notes:

---

## 2. Kernel vs Edge Classification

Question A: If this component is removed, does it break meaning or only reduce utility?

- [ ] Breaks meaning → Kernel candidate
- [ ] Reduces utility only → Edge only

Question B: Does this component introduce new invariants or merely consume existing ones?

- [ ] Introduces invariants → Kernel risk
- [ ] Consumes invariants → Edge-compatible

**Rules:** Kernel additions require cycle-level review; Edge components must remain replaceable without semantic loss

Classification decision:

---

## 3. Translation & Mediation Test

Question: Does this introduce a translation layer that reinterprets identity, rather than representing it?

- [ ] No — representation only
- [ ] Yes — identity is transformed
- [ ] Ambiguous

> **If "Yes" → REJECT | If "Ambiguous" → MUST document ambiguity explicitly**

Notes:

---

## 4. Capture Detection Scan

Evaluate current and trajectory risk.

### 4.1 Economic Capture

- [ ] Any single funder >50% influence?
- [ ] Revenue dependency tied to one actor?
- [ ] "Too big to lose" pressure emerging?

### 4.2 Infrastructure Capture

- [ ] Becomes de facto required infra?
- [ ] Failure would halt the kernel?
- [ ] Exit path unclear or costly?

### 4.3 Narrative Capture

- [ ] Language of inevitability used?
- [ ] "Official", "blessed", or "canonical" implied?
- [ ] Alternatives framed as inferior by default?

> **If 2+ boxes checked → CAPTURE RISK FLAG**

Notes:

---

## 5. Ambiguity Handling Test

Question: When ambiguity arises, does this proposal:

- [ ] Preserve ambiguity explicitly
- [ ] Make ambiguity inspectable
- [ ] Attribute resolution assumptions

OR

- [ ] Collapse ambiguity silently
- [ ] Enforce false certainty
- [ ] Hide conflict for UX or growth reasons

> **Silent collapse → REJECT**

Notes:

---

## 6. Reversibility Test

Question: Can this decision be reversed without breaking continuity of identity or meaning?

- [ ] Fully reversible
- [ ] Partially reversible (documented cost)
- [ ] Irreversible

> **If irreversible → MUST justify against Canon requirements**

Notes:

---

## 7. Stewardship Integrity Check

Question: Does this decision expand steward power beyond mandate?

- [ ] No — within constraints
- [ ] Yes — expands authority
- [ ] Unclear

If "Yes":

- [ ] Time-bound?
- [ ] Contestable?
- [ ] Documented dissent preserved?

Notes:

---

## 8. Brand & Narrative Guardrails

Does this proposal introduce or rely on:

- [ ] "The future of ..."
- [ ] "Inevitable"
- [ ] "Revolutionary"
- [ ] Claims of totality or replacement
- [ ] Moral superiority framing

> **Any checked → MUST REWRITE**

Notes:

---

## 9. Failure Condition Alignment

Question: If this decision later proves false, will failure be:

- [ ] Visible
- [ ] Attributable
- [ ] Acknowledged

OR

- [ ] Obscured
- [ ] Externalized
- [ ] Rationalized

> **If failure cannot be made visible → REJECT**

Notes:

---

## 10. Final Disposition

- [ ] APPROVE
- [ ] APPROVE WITH CONDITIONS
- [ ] DEFER TO NEXT CYCLE
- [ ] REJECT

Rationale (required):

---

## 11. Dissent Record (Required if contested)

Objections recorded:

- [ ] Yes
- [ ] No
- Summary of dissent:
- Reason dissent not adopted:

---

## Closing Reminder

> If this checklist feels obstructive, it is working.

OASIS does not optimize for speed, dominance, or persuasion. It optimizes for coherence under pressure.
