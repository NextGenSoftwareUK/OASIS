# WEB4 sandbox CQL admission — 30 October 2026

Current work: C:/Source/OASIS-cosmic, branch work/cosmic-cql.
Frontend: existing C:/Source/Web4Site, same branch. Public query success remains red.

## Root causes and verified changes

- Existing Railway project712f50f6-3b7b-46d4-b229-44b49922f16b, dev,
  WEB4 OASIS API: SUBSCRIPTION_USAGE_ENABLED=true, but no
  PREPARED_STORAGE_ADMISSION_JSON. Read-only CLI inspection; no configuration changed.
- Live authenticated CQL returns503 CQL_ADMISSION_REQUIRED before provider lookup.
- Startup incorrectly required one attempt for every CQL path, including federation.
  Core now allows bounded federation attempts and verifies maximum tariff times the
  full attempt reservation fits the budget. Leaf routes still require exactly one;
  native fee exposure must remain zero. No provider capability or visibility bypass.
- The old illustrated cql.query meter was rejected by the real subscription service.
  Corrected it to api.request. The deployable profile uses the actual supported meter.
- Scripts/Sandbox/cql-free-admission.json enrolls only row/scalar/federation over
  MongoDB/SQLite/Neo4j: explicit $0 service tariffs, one/one/three attempts,
  measurement required, one API quota unit and zero native fees. No quote credentials,
  native chains, paid subscription or pay-as-you-go enrollment.

## Verification

Core ConfiguredStorageAdmissionTests:52 passed/0failed/0skipped, including new
leaf/federation budget, normalized path and explicit-free-profile cases.
WEB4 SandboxCqlAdmissionTests:3 passed/0failed/0skipped. Tests read the actual shipped
profile, pass startup, and reach the real subscription authority's repository quota
check with api.request. A controlled repository denial proves free pricing does not
bypass authorization; this is not durable-host/native query proof.
Bounded Core build and both test builds pass, cache-only/no restore or installs.

## Remaining deployment gates and next step

1. Integrate the verified Core fix and parent pin/profile/tests into a scoped
   Development batch; preserve other contributors and publish only verified changes.
   Core task commit: 7e9cd721172a810ca9be6bac8e96516f045a28b2.
2. Latest dev deployment65e8e281-940d-4885-a427-db7e9c4c48c9 (835653f31) is FAILED,
   not still building. Last successful deployment5bbdaf97-ef50-4d89-82a6-3e8afd8bda03
   is23d56bae. Its Core6c475b9 predates the fix. Do not apply the federation profile
   to that revision or claim the new configuration is deployed.
3. Verify the existing accounting opening-balance marker/legacy inventory before
   enabling admission. No ledger inspection has proved it ready. Never set
   SUBSCRIPTION_LEDGER_INITIAL_STATE=empty-new-installation on an unknown/shared
   database; reviewed signed opening state remains required if data exists.
4. A fresh live demo avatar sandbox-038f1fe4 has zero subscription records via
   authenticated GET /api/subscription/subscriptions/me (HTTP200). Thus configuring
   routes alone will not suffice. Design free subscription provisioning plus cleanup
   for ephemeral demo identities using the existing billing authority; do not waive
   subscription checks, invent a paid plan or leave unbounded orphan records.
5. After host readiness, set PREPARED_STORAGE_ADMISSION_JSON from the shipped file
   on the existing dev WEB4 service with --skip-deploys, verify exact read-back, then
   deploy one verified batch. Retain the existing Mongo reservation/outbox/settlement
   path. Re-test browser rows, scalar, federation and visitor isolation with owned
   records; then expand provider enrollment. Do not reset/reseed any existing source.

## Compact evidence (raw files stay out of Docs)

- `TestResults/Web4SandboxCqlCurrent/admission-controls.trx`: 85015B, SHA256 `e23d3fe5152c2c29f39ee1cb0392505ef48c4ebb0ad462b7ca2ae413f0857bb9`.

- `TestResults/Web4SandboxCqlCurrent/host-admission-controls.trx`: 6325B, SHA256 `9aed676a0168ee898352660e200062ba789e18d175d0d7d1436d134024956f48`.

- `TestResults/Web4SandboxCqlCurrent/subscription-readiness.json`: 297B, SHA256 `1e20c2696ceed4c29fbf686a1036c27ac29e4230e908b17b39f1537e60184763`.
