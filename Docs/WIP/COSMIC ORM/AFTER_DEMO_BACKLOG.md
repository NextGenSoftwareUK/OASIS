# Return after the MVP demo

Updated 31 October 2026. These remain part of the full goal; they are deferred, not complete.

Priority now: reproducible demo blockers, actual hosted breadth/presets and scoped HyperDrive gates. Do not redo green scopes or expand parity depth ahead of the demo.

| Later work | Verified foundation and remaining scope |
|---|---|
| Operator recovery / late enrollment | Durable reviewed application, target receipts and fenced source completion exist; all6native routes/manager continuations/interleavings pass. Wider hosted/operator authorization and complete operational acceptance remain red. |
| Primary-authority placement cutover | Replica baseline-to-live delivery is not migration of write authority. Define authority epochs, admission and in-flight operation ownership before implementation. |
| Large-store baseline/migration | Existing bounded receipt audits and migration batches are not scale certification. Preserve independent target state and all original obligations. |
| Production uncertain commits | Three-store application ACK loss and real loopback HTTP disconnect pass. Production controller/auth/settlement, native-DB connection faults, process crashes and larger/protected graphs remain separate. |
| Mixed read/write representations | One admission factory rejects overlapping native signers and unsigned/query-only providers. Design operation/representation-scoped authorization/pricing and native/HTTP tests; do not remove the fence or silently substitute sources. |
| Deep LINQ / EF / NHibernate parity | Resume red rows, especially post-entity-selected terminals, reselection and graph composition. Keep green scopes/cursors frozen absent a bug. |
| POCO mapping and migrations tooling | Unrestricted non-IHolon entities, conventions/model mapping, schema diff/up/down/scaffolding remain deferred. |
| Full ORM acceptance audit | Reconcile tracking/save ownership, conditional concurrency, child load/save/depth parameters, rollback/history/deltas and migration/placement against original requirements. Focused proofs are not100%acceptance. |
| Broad provider production depth | Read-only MVPs do not certify owned native writes, auth/settlement/scaling, replication or distributed snapshots. Public-chain RPC finality is not independent settlement proof. |

The hosted SQLite6/8barrier was explicitly repaired; it is not an outstanding blocker. Preserve the original evidence and do not resave/reset it. Hosted Azure/Oracle remain deferred; no signup/payment re-asks. V2 implementation only, legacy issues document-only.

Recovery: [CURRENT_STATUS](CURRENT_STATUS.md). Acceptance: [checklist](CQL_PARITY_CHECKLIST.md). Details: [recovery](REPLICA_BASELINE_AND_RECOVERY.md), [expansion](CQL_DEMO_EXPANSION_ROADMAP.md). [Superseded backlog](Archive/STATUS_BEFORE_PUBLIC_READ_PROFILE_RECONCILIATION_2026-10-31.md).

## Native activation diagnostics found during XRPL demo

Reproducible operator gap: `CqlV1Providers.ActivateReadSourceAsync` discards the provider OASISResult.Exception when raising CQL_SOURCE_ACTIVATION_FAILED; `CqlHostedSources` records/logs only the outer InvalidOperationException and public CQL_SOURCE_UNAVAILABLE. Deployed XRPL endpoint returned HTTP402, but identifying that required a read-only in-container RPC probe. Later preserve a sanitized native failure type/stage/HTTP status for operators, with tests proving credentials, URLs/tokens and raw exception messages stay out of public discovery/logs. Do not add retries or weaken readiness. Evidence and exact original/correction deployments: [XRPL status](XRPL_PUBLIC_SOURCE_STATUS.md#hosted-endpoint-correction). Current demo fix is the explicit, tested free endpoint migration; this diagnostics improvement remains open.

Algorand public-source qualification (1 November): anonymous algod versions and indexer block0 agree Mainnet genesis; account address checksum works. Free indexer explicitly rejects account `round` rewind with HTTP400. A future account profile must expose current-state semantics explicitly, or use separately qualified immutable headers; do not promise pinned account snapshots or silently remove a requested round. No Algorand CQL admission granted.

## Reproduced demo blocker — session renewal (do before further breadth)

SandboxController.StartSession creates/authenticates a temporary avatar but omits the generated refresh cookie that AvatarAuthController issues. Acceptance tooling retained only JWT, so deployment waits outlived the original bearer. Normal avatar/refresh-token successfully renews the same owned avatar. Fix shared cookie issuance and browser/harness credential retention/rotation, with tests and explicit unknown-outcome handling. Preserve account identity and original demo copies; do not extend bearer expiry, replace the owner or mint test bearers directly. HTTP Radix acceptance used documented owned-fixture credential recovery, not automatic browser renewal. No David action needed.

Session root-fix progress: shared issuance/CORS/proxy correction is live and same-avatar secure rotation passes. Live logout owner check fails on the intentionally redacted request token list; Coreb835014 authoritative-owner correction passes5V2 tests, consumer/deploy verification pending. Frontend publication and actual browser lifecycle remain open. Preserve the original known Unauthorized response and continue only through the explicitly journalled correction path. [Current rollout](SESSION_RENEWAL_STATUS.md).

Session lifecycle HTTP correction and frontend publication completed at d9332469d/backend and9e18b7d/website. Original avatar secure rotation/revoke/rejected-reuse verified;0provider saves. Reproduced issuance/proxy/redacted-owner blockers resolved in this scope. Actual browser interaction/expiry observation remain separate open gates; do not replay completed cohort. [Proof](SESSION_RENEWAL_STATUS.md#live-lifecycle-and-page-publication-complete).
