2026-11-18 UTC — RECONCILIATION VALIDATOR FIX / RETEST: first combined run44316616718 stopped BEFORE compilation because the older structural rollout validator expected direct expiry registration and a single reconciliation enable argument. Current Development correctly uses the maintenance helper and rollout-enable AND reconciliation-pause guard. New candidate007b7364b0140cd78471fb500e8e2bbaa0a15185 changes only validator/helper/eight pure regression tests/one CI test command over4e260; runtime and DNA blobs are unchanged. The strict check requires BOTH guards, checks their real helper implementations, rejects OR/removal/unconditional duplicates and keeps expiry/telemetry rollout-gated. Eight test methods passed in RAM (including mutation subcases); frozen candidate Startup/control contract passed. [Exact replacement run44317710369](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44317710369) is IN PROGRESS, immutable tag cosmic-cql-worker-gates-007b7364b014. Previous combined candidate is NOT qualified; earlier accepted910d evidence remains scoped to910d. No Development push/deploy/shared Atlas operation.

# CQL Development reconciliation — 18 November 2026

## Source and invariant

Development: `0f7ebae449039c6580acd0f25c9709dc41ead529`.
Accepted exact CQL/network source: `910d6f1f1020d77bbb51ac25ff69a2dff877de4d`.
Combined candidate: `4e260e6ec001429c02e53effd487fb6f93a0d1f1`.
Immutable tag: `cosmic-cql-dev-reconciled-4e260e6ec001`.

Preserve current Development authentication, maintenance and settlement pauses, audit/seal changes, provider schema, unrelated APIs and intentional DNA differences while adding the verified CQL work. No shared Atlas operations or deployment are authorized by this reconciliation.

## Evidence and resolution

The broad old feature/Development comparison had385 shared-touched paths:234 identical and151 different. A blind merge produced105 conflicts and was rejected as a release approach; its tree was never committed or published.

Instead, the accepted qualification lineage starts at `d6984cf58b913d071ce9d6d6e126076d246d7414`, which is an ancestor of current Development. Its74 changed paths form a bounded integration scope. The normal three-way merge with current Development produced only two conflicts: Core gitlink and its manifest pin.

Both resolve to `c76d0b55cf619f63c9fe80f9894ab0e1d1de44ac`. Its ancestry includes actual Development Core `30c89aaf55ff7f2eae25d1217b2736e50fd0452a`, verified with merge-base --is-ancestor. The resolved manifest starts from Development's exact file and changes only OASIS_API_CORE_COMMIT. Candidate has two parents, Development and the accepted910d candidate; it differs from Development in72 paths after unchanged settings are naturally retained.

All23 paths in the latest Development37d8-to0f7 batch are asserted byte-identical in the combined tree. This includes Startup, access-log behavior, worker controls, seal policy/refresh, subscription policy overrides and their tests/docs. All changed non-gitlink blobs were checked for unresolved conflict markers. Dependency manifest/seven Dockerfiles/gitlink pre-push validation passed.

No physical checkout/index reset, branch switch, force push, additional branch/worktree/environment, live DNA write, PIPER write or shared database call occurred. The source-only merge tree and immutable qualification tag are retained as evidence. The one temporary Git index was automatically removed.

## Qualification and remaining gates

[Isolated exact-source run44316616718](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44316616718) is running. It builds WEB4 and runs selected CQL/compiler/HTTP tests, real SQLite cases and three real native-store suites in owned CI fixtures. Existing910d340+18+3 acceptance remains valid for910d; it does not prove the new combined source until this run passes.

No merged-source image is published yet. Previouse53 image is qualified for its own source only. Full hosted SQL/WEB6/mixed browser acceptance, general EF/NHibernate parity and hybrid placement/cache execution remain open. Shared Atlas stand-down and no deployment remain in force. Root feature working-tree divergence is preserved; this candidate is not permission to reset or sweep other contributors' changes.
