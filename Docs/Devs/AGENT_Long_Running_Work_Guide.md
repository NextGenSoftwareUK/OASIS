# Long-Running AI Agent Work — Guide

How to run AI coding agents (Claude, GPT and others) on multi-hour or multi-day OASIS work without wasting time, tokens or disk.

## 1. One worktree per concurrent agent

When two or more agents work at the same time, each gets its own git worktree and branch.

- One agent may stay in the main checkout (`C:\Source\OASIS`); every other agent gets one long-lived worktree, e.g. `C:\Source\OASIS-cosmic` on `work/cosmic-cql`.
- Create it once: `git worktree add <path> -b <branch> <base>`, then `git submodule update --init --recursive`, and put any submodule you change on a matching branch.
- Agents do not message each other about builds, leases or file ownership — their folders and build outputs are separate. They coordinate only when both must change the same Core/Shared public API.
- Each agent merges verified work into `Development` once or twice a day and merges `Development` back at least daily, so branches do not drift.
- Add each new worktree to `AdditionalBuildRepositories` in `Scripts/tools/cleanup-emergency-space.targets.psd1` so its build and test output is cleaned too. Remove it when the worktree is removed.

**Why:** in a shared checkout, agents spend a large share of their tokens and time negotiating ("are you building? can I touch Core?"). Every message is paid for twice (written by one agent, read by the other), stays in both contexts for the rest of the session, and blocks progress while one waits for the other.

**Cost:** each worktree plus build output is roughly 10–30 GB. Use the fewest worktrees that remove the contention — usually one per extra agent.

## 2. Model and reasoning effort

| Work | Suggested model | Effort |
|------|-----------------|--------|
| Design, query semantics, Core/Shared changes, hard debugging | Most capable model available (e.g. Claude Fable 5.1 or Opus 5.5) | High |
| Repeating a proven pattern (e.g. next provider family through an existing conformance suite) | Cheaper model (e.g. Claude Sonnet 5.5) | Medium |
| A stuck, difficult bug | Most capable model | Max, briefly |

- **Light/low effort is a false economy** for long runs. Long runs fail mostly through drift and wrong turns; shallow reasoning makes both more likely, and the redo costs more than the tokens saved.
- **On a budget:** run low/medium for routine steps and switch to high only for design decisions, tricky bugs and anything touching Core.
- To compare models fairly, give two of them the same well-defined task and compare time, cost and adherence to the brief.

## 3. Keep agents on track cheaply

1. **Written plan.** Keep a short checklist in `Docs/WIP/` (e.g. provider family → test → commit). Agents drift and redo work when the goal is vague.
2. **Keep context lean.** A long session re-reads its whole history on every turn, so cost keeps climbing.
   - *Interactive work:* start a fresh session after each milestone, with a short handoff doc in `Docs/WIP/`.
   - *Goal-driven overnight runs* (e.g. Codex goals running for days): restarting is impractical, and the tool compacts context automatically. Instead, have the agent keep a living status/handoff file in `Docs/WIP/` (done, next, open problems) updated at each milestone, so compaction loses nothing important. Keep the goal text short and stable, avoid pasting large logs into the conversation (write them to files and summarise), and let it drop finished detail.
3. **Cross-check.** Have one agent briefly review the other's commit before it merges to `Development`; catching a wrong turn early is far cheaper than fixing it later.
4. **Commit only your own files** in any shared checkout — never `git add -A` or `git commit -a`.

## 4. Test data and disk

- Everything under `TestResults`, `bin` and `obj` is regenerable and is removed by `Scripts/tools/cleanup-emergency-space.ps1`.
- The only exception is `TestResults/AWSProviderEvidence` (hash-pinned DynamoDB Local runtime and its retained data), listed under `Preserve` in the cleanup targets. It must stay under 200 MB (`PreservedSizeLimitMB`); the cleanup script warns when it does not.
- Scripts must reference test data with repo-relative paths, never another checkout's absolute path.

## Related

- `AGENTS.md` — repo rules for agents
- `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md` — no hacks or fallbacks
- `Scripts/tools/cleanup-emergency-space.ps1` — disk cleanup
