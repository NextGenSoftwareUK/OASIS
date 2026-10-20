# Durable conditional-write preparation

20 October 2026. This is an implementation checkpoint. The provider-neutral claim contract and native SQLite storage are implemented; manager/V2 integration and encrypted logical retry acceptance remain open.

## Ownership invariant

One logical operation retains one winning protected preparation at one concrete provider authority. A claim is not a holon mutation or committed acknowledgement. After claiming, Core must authenticate every returned record in its authorized runtime context, compare the complete original logical intent and conditions, and dispatch only the winning prepared graph. Storage does not decrypt the candidate or decide whether two plaintext inputs mean the same thing.

The protected per-record payload must retain each node's own authorization boundary. The shared preparation cipher binds operation, actor, authority, record, context and key identities; metadata and preparation are separate authenticated domains. See [crypto evidence](ENCRYPTION_CONTEXT.md#protected-preparation-crypto-owner--20-october-2026).

## Shared provider contract

Core exposes `IHolonWritePreparationStorageProvider`, `HolonWritePreparation` and `ProtectedHolonPreparation`. Claim sync/async operations retain the first candidate and return its original ordered opaque records, including when a later same-actor candidate contains different bytes or record identities. Returning the winner does not accept changed logical intent: the subsequent Core authentication/comparison owner must reject it before mutation. A different actor receives an explicit error with no preparation contents.

The immutable candidate captures between one and 4,096 unique, nonempty record identities. Individual envelope text is limited to two Mi characters; the aggregate limit is sixteen Mi characters. Admission accepts only the preparation prefix and valid Unicode. It does not claim cryptographic authentication. Record references and ciphertext remain untrusted until Core authenticates them. Operation and actor identities must be nonempty, and the provider authority must be concrete.

These are explicit character/count limits, not native scan deadlines or deployment quotas. The SQLite escaped JSON transport has a separate bounded read admission. There is no process-local claim cache, nonce derivation, ciphertext fingerprint omission, cross-provider fallback or automatic retention cleanup.

## SQLite native owner

`CosmicWritePreparation` stores operation ID as its native primary key, actor ID, opaque record JSON and UTC claim time. An IMMEDIATE native transaction reserves the writer before `INSERT ... ON CONFLICT DO NOTHING` and the winner read. Independent clients converge on one row. The first prepared bytes remain stable across provider reconstruction; no holon row is written during a claim.

Actor and authority mismatches are errors without returned contents. Corrupt/duplicate/invalid stored record JSON rejects without replacing it. The claim owner separates pre-commit failure, uncertain commit or rollback, and a known acknowledgement followed by cleanup failure. An uncertain outcome carries `SqlitePreparationReconciliationException`, the original operation ID, and the shared native reconciliation marker; no native transaction ID is invented. These failure-state branches still require injected fault acceptance beyond the initial native conformance tests.

Both activation upgrade and EF migration create the table. Downgrade refuses to discard any retained claim. Retention, abandoned preparations, operational reconciliation and authenticated tenant admission are still required; do not delete claims to make a retry appear new.

## Evidence and remaining integration

Initial focused native claim cohort: eight executed, eight passed, zero skipped (`TestResults/SQLiteCurrent/preparation-store-native-focused.trx`). These storage tests intentionally use opaque random tokens, not authenticated Core envelopes, and do not certify encrypted logical replay. Three new Core contract tests cover detached ordering, immutable publication, duplicate/null/identity admission and count/payload limits. Full Core initially passes 1,511/1,511; final valid-Unicode admission evidence is recorded in the integration boundary after rebuilding.

Next work:

- Capture exact logical intent and prepared per-node snapshots in the protected payload, including ordered roots, graph membership/roles, write conditions, operation flags and replication enrollment.
- Route claims through V2's existing authorization, billing and pinned-authority execution owner. Unsupported providers must refuse the required capability before any persistence.
- Authenticate and compare the winning preparation before dispatch; do not trust returned selectors or provider data.
- Recover the original prepared graph without publishing caller/tracking state until the normal committed acknowledgement is verified.
- Verify same-intent races, altered intent, key rotation/denial, tampering, crash/uncertain outcomes, sync/async single/batch/child graphs and supported provider parity.

The [encrypted retry regression](CONDITIONAL_WRITES.md#encrypted-preparation-replay-defect--20-october-2026) remains two passing unencrypted controls and two failing encrypted cases. Having a claim table alone does not fix that regression.

Final acceptance: Core1,511/1,511 and SQLite1,011/1,011 pass, zero failures/skips, terminal0; native run129s. These reports are preparation-store-final-core-full.trx and preparation-store-final-native-full.trx in their existing Core/SQLite result folders. The separate final replay report preparation-store-final-replay-regression.trx executes all four cases, passes two and fails two, terminal1. XML counters confirm all stated totals/executed counts. Core source is locally committed as f2bd54c. Exact assembly hashes are retained in the integration boundary. The owned fixture DB is absent after all runs; no push/deployment.
