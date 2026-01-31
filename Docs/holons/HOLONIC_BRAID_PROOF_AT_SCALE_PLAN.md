# Holonic BRAID: Proof at Scale with Actual Agents

## Purpose

We have a **small-scale holonic demo** (agent memory sharing, parent-child, storage efficiency) that proves the holonic **memory** pattern. It does **not** yet include BRAID (reasoning graphs). This document outlines how to **prove Holonic BRAID at larger scale with actual agents running**—so we can show that shared BRAID graphs in holons reduce cost and increase reuse when multiple agents are involved.

---

## 1. What We’ve Already Proven (Small Scale, No BRAID)

| Proven | What | Where |
|--------|------|--------|
| Holonic memory sharing | Multiple “agents” write facts to one parent holon; all see the same shared knowledge | `holonic-demo/` |
| Storage efficiency | Deduplication when multiple agents learn the same fact | Same demo, metrics |
| Parent-child in UI | Nested holons, connection lines, “query agent” vs “shared knowledge” | Same demo |
| Real storage | Actual localStorage / OASIS API for bytes and savings | Same demo |

**Gap:** No BRAID. No reasoning graphs, no generator/solver split, no “shared graph holon” that many agents reuse. The next step is to add the BRAID layer and then scale the number of agents.

---

## 2. What “Proof at Scale with Actual Agents” Means

**Larger scale**

- **More agents:** e.g. 5–10+ distinct agent processes (not just 3 buttons in one UI).
- **More tasks:** e.g. 50–500 tasks (questions or reasoning problems) so graph reuse is non-trivial.
- **Shared graph store:** One parent holon (e.g. “BRAID Graph Library”) whose children are BRAID graphs; all agents read/write that store via OASIS.

**Actual agents**

- **Minimum:** Separate runnable processes (scripts or services) that each can (a) receive a task, (b) look up or create a BRAID graph, (c) run a solver using that graph. These can be thin wrappers (e.g. Node/Python) around OASIS API + one LLM API.
- **Stronger:** Real agent runtimes—e.g. OpenSERV agents, or OASIS A2A agents—that implement the same “check graph holon → generate if missing → solve” loop and call OASIS for graph storage.
- **Ideal:** Same as above, but using OpenSERV’s BRAID pipeline (generator + solver) when available; until then, a BRAID-**like** pipeline (e.g. “generate Mermaid plan” → “execute plan”) is enough to prove the holonic **infrastructure** for graphs.

**Success = we show**

1. **Graph reuse:** Many tasks hit the same “task type” and reuse the same graph holon instead of regenerating.
2. **Cost drop:** Total cost (or “cost per task”) goes down as we add agents and tasks, compared to “each agent generates its own graph every time.”
3. **Correctness (predictable, accurate AI):** Solver outputs remain correct when using a cached graph from the holon store (no regressions from sharing). This aligns with the BRAID team’s goal—“we want predictable AI, accurate AI more than anything”—by ensuring that reusing shared graphs does not reduce accuracy and that the same graph yields consistent, predictable behavior across agents and requests.

---

## 3. Phased Plan

### Phase 1: BRAID Layer in the Existing Holonic World (Single Process, Simulated Agents)

**Goal:** Prove “BRAID graphs as holons” in the same mental model as the current demo, without scaling agents yet.

**Scope:**

1. **Graph holon model**
   - One parent holon: “BRAID Graph Library” (or “Reasoning Graph Library”).
   - Child holons = individual graphs. Each has `metaData`: e.g. `mermaid_code`, `task_domain`, `accuracy`, `usage_count`, `ppd_score`.
   - Reuse existing OASIS save-holon / load-holons-for-parent (or equivalent in your stack).

2. **BRAID-like pipeline (can be simulated)**
   - **Generator:** Given “task type” or “problem sketch,” produce a Mermaid flowchart (or a fixed string for the demo). In a first version, this can be hardcoded or a single LLM call that returns Mermaid.
   - **Solver:** Given Mermaid + concrete task, return an answer. Can be simulated (e.g. mock answers) or a real small model.
   - **Holon flow:** Before “solving,” check if a graph for this task type exists in the Graph Library (load-holons-for-parent + filter by `task_domain`). If yes → use it (solver only). If no → call generator, save new child holon, then solve.

3. **UI / demo**
   - Extend the holonic demo (or add a “BRAID” tab/section): show “Graph Library” as a parent, individual graphs as children, and a simple “Run task” flow that (a) picks task type, (b) loads or creates graph, (c) shows “reused” vs “generated” and optionally cost.
   - No need for multiple processes yet; “Agent A / B / C” can be buttons that share the same in-process graph library backed by real holons.

**Deliverables:** One end-to-end path where “running a task” uses a BRAID graph stored in a holon, with reuse when the same task type is chosen again. Design so Phase 2 can replace “buttons” with real processes.

**Proof:** “We can store and reuse BRAID graphs in the same holonic system we use for shared memory.”

---

### Phase 2: Multiple Processes, Shared Graph Holon (Actual “Agents”)

**Goal:** Several **separate** processes that all use the **same** graph library holon, so we measure reuse and cost across “agents.”

**Scope:**

1. **Agent process**
   - One small program (e.g. Node or Python) that:
     - Takes a task (e.g. from CLI, env, or a simple queue).
     - Calls OASIS to load the Graph Library’s children (load-holons-for-parent).
     - If a graph exists for the task’s type → use it (solver only).
     - Else → call generator, save new child holon via OASIS, then run solver.
     - Logs: “reused graph” vs “generated new graph,” task type, and optionally cost.
   - No need for full OpenSERV/A2A yet; “agent” = this process.

2. **Orchestration**
   - Run 5–10 such processes in parallel (e.g. 5–10 terminals, or a small launcher that spawns N workers).
   - Feed them tasks from a fixed list (e.g. 50–200 tasks) with a limited set of task types (e.g. 5–10 types) so that reuse is expected.
   - All processes use the **same** Graph Library parent holon id (e.g. from config or env).

3. **Metrics**
   - **Reuse rate:** (tasks that used existing graph) / (total tasks).
   - **Graph generations:** number of new graph holons created.
   - **Cost (if using real LLMs):** total spend; cost per task. Compare to a “no-cache” baseline (every task generates a new graph).

**Deliverables:** Scripts/instructions to run N agent processes against one shared graph holon, plus a minimal “results” summary (reuse rate, generations, optional cost).

**Proof:** “Multiple independent processes reuse the same BRAID graph holons, and we see a measurable drop in graph generations and cost.”

---

### Phase 3: Real Agent Runtimes (OpenSERV / A2A) + Real BRAID (If Available)

**Goal:** Use **actual** agent runtimes (OpenSERV or OASIS A2A agents) and, if possible, OpenSERV’s BRAID pipeline, while keeping the same holonic graph store.

**Scope:**

1. **Agent runtime**
   - Each “agent” is an OpenSERV agent or an OASIS A2A agent that:
     - Exposes a “run task” (or “solve”) interface.
     - Inside: “resolve graph for task type” via OASIS (load-holons-for-parent, choose or create graph), then “solve with that graph” (using BRAID solver when available, or a BRAID-like “plan + execute”).
   - Graph storage stays in OASIS (save-holon for new graphs, load-holons-for-parent for lookup). No duplication of graph store in OpenSERV.

2. **BRAID integration**
   - **If OpenSERV exposes BRAID:** Agent calls OpenSERV for “generate graph” and “solve with graph”; we only need to (a) persist the graph to OASIS after generation, and (b) check OASIS before calling “generate.”
   - **If not:** Keep the BRAID-like pipeline from Phase 1/2 (e.g. “generate Mermaid” + “execute”), but run it inside the real agent process. The proof is still “shared graph holon + multiple agents.”

3. **Scale**
   - Run e.g. 10+ agents, 100+ tasks, 10+ task types. Measure reuse, generations, cost, and latency.

**Deliverables:** Integration guide (how an OpenSERV/A2A agent uses OASIS for the graph library), plus benchmark results (reuse, cost, comparison to no-cache).

**Proof:** “Real agents, in real runtimes, share BRAID (or BRAID-like) graphs via OASIS holons, and we see the expected efficiency gains at scale.”

---

### Phase 4: Benchmarks and “Larger Scale” Numbers

**Goal:** Publishable-style numbers: agents, tasks, task types, reuse rate, cost vs baseline.

**Metrics to collect:**

| Metric | Description | Target |
|--------|-------------|--------|
| **Agents** | Number of distinct agent processes | ≥ 10 |
| **Tasks** | Total tasks executed | ≥ 100 |
| **Task types** | Distinct graph types | ≥ 5 |
| **Reuse rate** | % of tasks that used existing graph | High (e.g. > 80% after warm-up) |
| **Graph generations** | New graph holons created | << tasks (e.g. ≤ task types) |
| **Cost (Holonic BRAID)** | Total $ for run | Baseline to compare |
| **Cost (no-cache baseline)** | Same tasks, each agent generates every time | Higher than Holonic BRAID |
| **Latency** | p50/p95 “task end-to-end” | Acceptable for demo |

**Experiment design:**

- **Warm-up:** First K tasks may trigger one generation per task type; then the rest should reuse.
- **Baseline:** Same tasks, same agents, but “always generate, never read from holon”—measure cost and time.
- **Report:** “With N agents and M tasks, Holonic BRAID achieved X% reuse, Y total graph generations, and Z% cost reduction vs baseline.”

---

## 4. What “Actual Agents” Can Be (Concrete Options)

| Option | Description | Best for |
|--------|-------------|----------|
| **A. Lightweight workers** | Node/Python scripts that call OASIS API + one LLM API (e.g. OpenAI/Anthropic). Each process = one “agent.” | Phase 2, fastest path to “multiple processes, shared holon.” |
| **B. OpenSERV agents** | Agents built on OpenSERV that call OASIS for graph storage and use BRAID (or BRAID-like) for generation/solving. | Phase 3 when OpenSERV + BRAID are in the loop. |
| **C. OASIS A2A agents** | Agents registered in OASIS (e.g. via A2A/SERV), each running a “solve task” capability that uses the Graph Library holon. | Phase 3 if you want everything inside OASIS ecosystem. |
| **D. Hybrid** | OpenSERV or third-party “solver” for BRAID, OASIS only for graph persistence and lookup. | Phase 3 when BRAID lives outside OASIS but storage is holonic. |

For “proof at scale,” **A** is enough to get to multiple processes and shared graph holons. **B/C/D** strengthen the story to “real agents in real runtimes.”

---

## 5. Technical Prerequisites

- **OASIS (or compatible API):** save-holon, load-holon, load-holons-for-parent. The existing holonic demo already uses these (or equivalents).
- **Graph library parent holon:** Create once, pass its id to all agents (e.g. via config/env).
- **Task taxonomy:** Even a simple one (e.g. `task_domain`: “math”, “code”, “instruction”) so that “task type” → “which graph to load or create” is well-defined.
- **BRAID or BRAID-like pipeline:** Either OpenSERV BRAID or a minimal “generate Mermaid → execute” flow. The former is ideal; the latter is enough to prove the holonic graph-store pattern.

---

## 6. How This Connects to the Holonic Demo

- **Current holonic demo:** Shows shared **memory** (facts) in a parent holon; agents are UI-controlled and single-frontend.
- **Holonic BRAID proof:** Uses the **same** parent-child, save/load-holons-for-parent idea, but the “content” is **BRAID graphs** and the “agents” are **runnable processes** (and later real agent runtimes).
- **Reuse of existing work:** Phase 1 can live next to the current demo (e.g. “BRAID” mode or tab: “Graph Library” parent, graph children, “run task” that uses them). Phase 2 reuses the same OASIS graph-store contract from Phase 1 and just moves “agent” into separate processes.

---

## 7. Success Summary

We’ve **proven** Holonic BRAID at scale when:

1. **Multiple agents** (separate processes or real runtimes) all use the **same** BRAID graph library backed by OASIS holons.
2. **Reuse** is measurable: reuse rate goes up, graph generations stay low.
3. **Cost** drops vs a “no shared graphs” baseline when we hold tasks and agents fixed.
4. **Correctness** holds: solver behavior with shared graphs matches or improves on “generate every time.”

Phases 1–4 above get you from “BRAID graphs in holons in one process” to “many agents, many tasks, shared graph store, and clear numbers.” Actual agent runtimes (OpenSERV / A2A) fit in at Phase 3 when you’re ready to move from “worker scripts” to “real” agents.

---

## 8. Fast Path to Actual Agents (Summary)

**You proved** small-scale holonic memory (shared parent, dedup, real storage). **You want** larger scale + actual agents running.

**Short answer:** Do **Phase 1** (BRAID graphs as holons + lookup-or-create in one process), then **Phase 2** (many processes sharing one Graph Library via OASIS). Phase 2 is where “actual agents” = separate runnable processes; no OpenSERV/A2A required to prove reuse and cost drop.

| Phase | What | “Actual agents”? |
|-------|------|-------------------|
| **1** | BRAID Graph Library holon + child graph holons; “run task” → check holon by task type → reuse or generate → solve | No — single process, buttons or simulated agents |
| **2** | Same logic in **Node or Python scripts**; each process = one agent; shared `GRAPH_LIBRARY_ID` via env; run 5–10 in parallel | **Yes** — separate processes |
| **3** | Same graph store, but agents = OpenSERV or OASIS A2A agents | Yes — real runtimes |
| **4** | Formal benchmarks (reuse %, cost vs baseline) | — |

**Concrete steps:**

1. **Phase 1 (prerequisite):** In the holonic demo (or a “BRAID” tab), add:
   - Create/fetch a “BRAID Graph Library” parent holon (once).
   - Child holons = graphs with `metaData`: `mermaid_code`, `task_domain`, `usage_count`.
   - “Run task” flow: pick task type → `loadHolonsForParent(graphLibraryId)` → filter by `task_domain` → if match use it (solver only), else call generator → `saveHolon` new child → run solver. Show “reused” vs “generated” in UI.
2. **Phase 2 (actual agents):** Implement a **worker script** (Node or Python) that:
   - Reads `GRAPH_LIBRARY_ID` and `TASK` (or task list file) from env/config.
   - Per task: load holons for parent, filter by task type; reuse or generate+save; run solver; log `reused` / `generated`.
   - Run 5–10 copies in parallel (different terminals or a launcher), all using the same `GRAPH_LIBRARY_ID`. Use a fixed task list with 5–10 task types and 50–200 tasks so reuse is high.
3. **Measure:** Reuse rate, number of new graph holons, and (if using real LLMs) cost vs “always generate.”

The holonic demo already uses `saveHolon`, `loadHolon`, `loadHolonsForParent` in `oasis-api.js`; Phase 1 reuses those. Phase 2 reuses the same OASIS API from a headless script.

### Phase 2 worker contract (for “actual agents”)

Each **worker** is one process. Same contract for Node or Python:

**Inputs (env or config):**

- `OASIS_BASE_URL` — e.g. `http://localhost:5003` or `https://api.oasisweb4.com`
- `OASIS_TOKEN` or `OASIS_USERNAME` / `OASIS_PASSWORD` — for authenticated API calls
- `GRAPH_LIBRARY_ID` — parent holon id for the BRAID Graph Library (same for all workers)
- `TASK_TYPE` — e.g. `math`, `code`, `instruction` (or read from a shared task list file)
- Optional: `TASK_ID`, `TASK_INPUT` for a single task; or `TASK_LIST_PATH` for a file with one task per line (e.g. `task_type,input_text`)

**Behaviour:**

1. Load children of `GRAPH_LIBRARY_ID` via `GET /api/data/load-holons-for-parent/{GRAPH_LIBRARY_ID}`.
2. Filter children by `metaData.task_domain === TASK_TYPE` (or equivalent).
3. If a matching graph exists → use its `metaData.mermaid_code` (solver only). Log `reused`.
4. Else → call generator to produce Mermaid, create child holon with `parentHolonId: GRAPH_LIBRARY_ID`, `metaData: { mermaid_code, task_domain, usage_count: 0 }`, save via `POST /api/data/save-holon`, then run solver. Log `generated`.
5. (Optional) Increment `usage_count` on the graph holon and save.
6. Exit with stdout/stderr suitable for aggregation (e.g. `reused` / `generated`, task id, latency).

**How to run at scale:**

- Shared task list: e.g. `tasks.csv` with columns `task_type, input`. Each worker consumes one or more tasks (e.g. via a queue or by splitting the file).
- Run N workers in parallel, all with the same `GRAPH_LIBRARY_ID` and `OASIS_BASE_URL` (and auth). Example: 5 workers, 50 tasks, 5 task types → expect high reuse after warm-up.
- Aggregate logs: count `reused` vs `generated`, total new graph holons created, and optionally cost.

---

## 9. References

- **Holonic BRAID proposal:** `Docs/holons/HOLONIC_BRAID_PROPOSAL.md`
- **Holonic demo (memory, no BRAID):** `holonic-demo/`
- **Backend-first BRAID demo:** `Docs/holons/HOLONIC_BRAID_DEMO_BACKEND_FIRST.md` — demo is driven by ONODE; real processes run on the backend.
- **Shared holon pattern:** `Docs/holons/SHARED_HOLON_PLAYGROUND_GUIDE.md`
- **OASIS holon ops:** `Docs/holons/HOLONIC_ARCHITECTURE_OVERVIEW.md`

---

*This plan is the canonical “how we prove Holonic BRAID at scale with actual agents.” Use it to scope implementation and to report progress (e.g. “Phase 1 done,” “Phase 2 reuse rate X%”).*
