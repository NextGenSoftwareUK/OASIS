# OASIS Kernel Specification (v0)

> **Status:** Binding  
> **Scope:** Kernel only  
> **Purpose:** Define what the OASIS kernel is, does, and refuses to do

---

## 1. Purpose of the Kernel

The OASIS kernel exists to enforce a single architectural claim:

> Digital identity can persist independently of any execution environment, provided that identity is defined minimally and meaning loss is made explicit.

The kernel does not optimize for adoption, performance, convenience, or economic value. It optimizes for correctness under failure.

> **If this claim cannot be upheld, the kernel is invalid.**

---

## 2. What the Kernel Is

The kernel is a constraint system, not a platform.

It defines:

- a primitive (the Holon),
- a set of invariants,
- and a small number of non-negotiable rules governing identity, persistence, and reconciliation.

The kernel does not:

- host applications, execute business logic, impose governance outcomes, or define economic behavior.

---

## 3. The Holon (Kernel Definition)

### 3.1 Definition

A Holon is a self-identifying digital unit whose identity is independent of any single execution environment and whose relationships to external systems are explicit and inspectable.

A Holon is the only object the kernel recognizes.

### 3.2 Holon Invariants (Non-Negotiable)

A Holon MUST satisfy all four invariants. Failure of any invariant invalidates the model.

#### 1. Self-Containment (Identity)

A Holon's identity must not be derived from: a blockchain, a database, a platform, or an authority.

Identity must persist even if all external systems fail.

> **Failure condition:** If identity collapses when a provider disappears, the kernel fails.

#### 2. Persistence Across Environments

- A Holon may be replicated, migrated, or reconstructed across environments.
- No environment is canonical by default.
- Persistence may degrade, but identity must not.

> **Failure condition:** If persistence requires a single privileged host, the kernel fails.

#### 3. Interoperability Without Identity Translation

- A Holon's identity must remain invariant across representations.
- Environments may translate representations, but not redefine identity.
- Any translation affecting identity must be explicit and attributable.

> **Failure condition:** If interoperability requires redefining identity, the kernel fails.

#### 4. Observability

A Holon's state, dependencies, and conflicts must be inspectable.

No silent mutation, silent reconciliation, or hidden authority is permitted.

> **Failure condition:** If conflicts are resolved invisibly, the kernel fails.

---

## 4. Identity vs Commitments

### 4.1 Identity

Identity answers: what this Holon is.

- Identity is stable.
- Identity does not fork.
- Identity does not depend on truth claims about external systems.

### 4.2 Commitments

Commitments are claims a Holon makes about the world: location, ownership, membership, state on external systems.

Commitments may fork, may become ambiguous, may fail, may be withdrawn or superseded.

Commitments do not redefine identity.

> **Kernel guarantee:** The kernel guarantees that loss of meaning is explicit, not that meaning is preserved.

---

## 5. Reconciliation Constraints (Kernel Rules)

When multiple environments report conflicting commitments, the kernel enforces constraints — not outcomes.

The kernel requires:

1. **No Silent Resolution** — All conflict resolution must leave a trace.
2. **No Privileged Final Arbiter** — No environment, provider, or authority is canonical by default.
3. **Explicit Ambiguity Is Permitted** — Uncertainty is preferable to false certainty.
4. **Attributable Resolution** — Any resolution must be traceable to its assumptions and mechanism.

Kernel does not require convergence.

> **Failure condition:** If reconciliation requires hidden authority or forced convergence, the kernel fails.

---

## 6. Kernel Boundaries (What the Kernel Does NOT Do)

The kernel explicitly excludes: application logic, user experience, orchestration, performance optimization, governance outcomes, economic incentives, token issuance, permissioning, access control.

These may exist only at the edge.

If removing a component breaks meaning, it may belong in the kernel. If removing it only reduces utility, it does not.

---

## 7. Kernel Evolution Rules

- Kernel changes are rare, explicit, and slow.

Any kernel change must: preserve all invariants, document dissent, and state new failure conditions.

> **If kernel evolution requires irreversible, arbitrary judgments to function, the kernel is considered compromised.**

---

## 8. Relationship to the Edge

The kernel defines constraints. The edge explores possibility.

Edges may: experiment, commercialize, fail, fork, or disappear.

The kernel must remain intact regardless.

---

## 9. Kernel Failure Conditions (Project-Ending)

OASIS must halt kernel development if any of the following are true:

1. Identity cannot persist without privileged infrastructure
2. Reconciliation requires silent or centralized authority
3. Kernel evolution becomes captive to economic or institutional pressure
4. Holon semantics require irreversible, arbitrary judgments to function

> **Continuation under false assumptions is failure.**

---

## 10. Closing Constraint

> The kernel exists to make OASIS harder, not easier.

Any feature that reduces discomfort at the cost of ambiguity, observability, or independence does not belong here.
