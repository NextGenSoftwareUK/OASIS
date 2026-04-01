# Agent policy: fix root causes — no hacks, fallbacks, or workarounds

This file is for **AI / Cursor agents** and future sessions. It applies to **all work in this repo**: C#, C++, ZScript, APIs, scripts, and packaging — not only ODOOM or `star_api`.

When something fails or behaves wrong, **fix why it fails**, not a parallel “maybe this will work” path.

---

## A. General code (any language, any layer)

### What counts as a workaround (avoid)

- **Dual paths for the same responsibility** — e.g. “try new API, on failure call legacy API” when both are supposed to be the same contract. Either migrate callers and remove the old path, or fix the new path so it satisfies the contract.
- **Swallowing errors** — empty `catch`, log-only `catch`, returning default success when the operation did not succeed. Use **`OASISResult<T>`** / explicit errors; let the caller or user see a real failure unless the spec truly defines partial success.
- **Defensive duplication** — “also set this CVar in case the other layer forgot.” Fix the layer that should set it; one owner for one invariant.
- **Magic retries without a spec** — looping N times or sleeping to “shake loose” races. Fix ordering (await the right task, single message flow, one transaction) unless retry is a **documented** protocol (e.g. idempotent HTTP with backoff policy).
- **Stringly / format guessing** — multiple parsers for the same payload “for compatibility.” Pick one canonical format; fix producers and consumers to match.
- **Feature flags or `if (oldBehavior)`** to avoid fixing data or callers — temporary during a controlled rollout is fine; **shipping** with two behaviours for the same user action is debt. Remove the branch after migration.
- **Copy-paste “safety” checks** in every caller instead of **one validation** at the system boundary.

### What to do instead

1. **Name the invariant** — e.g. “After `StartQuest`, avatar active quest in DB matches cache used by `get_quests_string`.”
2. **Find the single place that broke the invariant** — wrong field updated, missing await, stale cache key, wrong quest id in persist.
3. **Change that place** (and tests if they exist). Remove redundant branches that existed only to mask it.
4. **Prefer one clear code path** — easier to reason about and matches user rules (focused diffs, no drive-by complexity).

### Legitimate patterns (not “fallbacks”)

- **Real optional features** — user or config explicitly chooses behaviour; both paths are specified and tested.
- **True external instability** — retries with policy for **documented** flaky I/O, with metrics and eventual failure surface to the user — not silent infinite retry.
- **Backward compatibility at a boundary** — e.g. API version negotiation **defined** by the protocol, not ad hoc “try parse as JSON then as XML.”

---

## B. Builds, deploy, and native boundaries

1. **Do not hide broken builds or deploy drift in application code.**  
   Examples of what **not** to do:
   - `dlsym` / `GetProcAddress` optional resolution for symbols that are **part of the shipped contract** (`star_api.h` exports the game links against).
   - “If new API missing, call old API and hope” dual paths for the same user action.
   - Swallowing errors and continuing as if success.

2. **Treat the symptom as a pipeline bug until proven otherwise.**  
   - `undefined symbol` at launch → native library next to the binary does not match headers / linker expectations → **rebuild and redeploy** the producer (e.g. STARAPIClient), **re-run** packaging (e.g. `BUILD_ODOOM.sh`), add or tighten **build-time checks** so the next person gets a clear failure instead of a runtime crash.

3. **Prefer fail-fast over masking.**  
   - Add verification in **build or deploy scripts** (e.g. `nm` / `objdump` on required exports).  
   - Prefer a loud script error + doc link over game code that “works” on half the installs.

4. **Two-tree / copy-step problems are fixed by sync and docs, not by forks in game logic.**  
   - ODOOM canonical sources live under `OASIS Omniverse/ODOOM/`; the running engine is built from `UZDOOM_SRC` after copy. See `OASIS Omniverse/Docs/ODOOM_UZDoom_Build_Sync.md`.  
   - The fix is: run the official build, fix the script if it’s wrong, document the step — not “if file A missing read file B.”

5. **Async ordering bugs are fixed by sequencing in the right layer** (e.g. C# chain after `StartQuestAsync` completes), not by “try persist again later” guesses in the client unless that retry is a specified, tested protocol.

---

## When you think you need a fallback

Stop and answer:

- What **invariant** is broken?
- What **single change** restores the invariant (code, data, or deploy)?
- Can a **test or script** enforce that invariant next time?

If yes, implement **that** — do not add a second path that hides the break.

---

## OASIS-specific pointers

| Situation | Root-cause fix (not a workaround) |
|-----------|-------------------------------------|
| Missing `star_api_*` symbol at launch | Rebuild/deploy STARAPIClient; full `BUILD_ODOOM.sh` / packaging; see `OASIS Omniverse/Docs/ODOOM_UZDoom_Build_Sync.md` (STAR native library section). If publish was skipped as “up to date”, ensure **`REQUIRED_STAR_EXPORTS`** in `STARAPIClient/Scripts/build-and-deploy-star-api-unix.sh` includes the new symbol so the script forces publish. **Also check `ODOOM/build/libstar_api.so`:** the game loads **`libstar_api.so`**, not only `star_api.so`; a stale `libstar_api.so` beside a new `star_api.so` causes this error (see `OASIS Omniverse/Docs/ODOOM_UZDoom_Build_Sync.md`). |
| ZScript / HUD “ignored” | Copy step + pk3 rebuild; same doc. |
| Quest / cache feels stale | Fix invalidation and single source of truth in STAR client / API; don’t duplicate cache logic “just in case.” |
| API returns wrong shape | Fix serializer or DTO in ONODE/WebAPI; don’t add a third parser in the game client. |

---

## Relation to user rules and `AGENTS.md`

User rules already ask for real implementations, **`OASISResult`**, and minimal unrelated refactors. This document adds: **one honest code path**; fix the producer of bad state; **fail visibly** when the system is misconfigured — in **all** subsystems, not only native glue.
