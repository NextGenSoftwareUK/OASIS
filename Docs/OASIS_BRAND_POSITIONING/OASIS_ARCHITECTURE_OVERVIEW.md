# OASIS — Architecture Overview

![OASIS kernel diagram](oasis-kernel-diagram.svg)

The OASIS architecture is organized as concentric layers, ordered by constraint.

Constraint and authority are highest at the center and decrease outward. Freedom, experimentation, and failure increase toward the edge.

> **This is not a stack. It is an ontological gradient.**

---

## Layer 1: The Holon (Core)

At the center is the Holon.

The Holon is the identity-first unit and the only object the kernel recognizes. It exists independently of any execution environment and precedes all systems that host it.

Nothing replaces the Holon. Everything else orbits it.

---

## Layer 2: Non-Negotiable Invariants

Surrounding the Holon are four invariants enforced by the kernel. These are laws, not features.

### 1. Self-Containment (Identity)

Identity is not derived from infrastructure or authority.

### 2. Persistence Across Environments

Identity survives migration, replication, and failure.

### 3. Interoperability (No Identity Translation)

Representations may vary; identity must not.

### 4. Observability (No Silent Resolution)

State, dependencies, and conflicts must be inspectable.

> **If any invariant fails, the kernel fails.**

---

## Layer 3: Kernel Constraints

The kernel defines constraints, not outcomes.

- Identity vs Commitments
- Reconciliation Constraints
- Explicit Ambiguity Permitted
- Attributable Resolution

This layer governs how disagreement, conflict, and uncertainty are handled — without enforcing convergence or truth.

---

## Layer 4: Interfaces

Interfaces are lenses, not gates. They expose, inspect, and express kernel-constrained reality without defining it.

Examples include: SDKs, APIs, CLIs, Visualizers, Indexers

Interfaces must not redefine identity, impose authority, or become mandatory.

---

## Layer 5: The Edge

The edge is where everything else lives. Experiments · Applications · Economies · Culture

This includes: use cases and products, communities and collaborations, art/commerce/token systems/research, and execution environments (blockchains, databases, clouds, storage networks, runtimes).

The edge is free to evolve, fork, fail, or disappear.

> **Nothing at the edge is required for the kernel to remain valid.**

---

## Core Principles

> Constraint is highest at the center and decreases outward.

> Identity is defined at the core; meaning is negotiated at the edge.

> The kernel enforces invariants, not outcomes.

> Everything beyond the kernel is optional — including success.
