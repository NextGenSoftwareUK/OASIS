# Durable holon delivery recovery evidence

Updated 19 October 2026. Local SQLite/store/worker implementation; authenticated reconciliation and process-crash recovery remain open.

## Invariant

An uncertainty marker and its original native transaction identities must survive worker exit with the blocked delivery state. Exception objects and bounded human-readable error messages are not a durable recovery record. Native identities are copied only from `INativeMutationReconciliationRequired`, using the existing NativeMutationReconciliation helper; no transaction identity is parsed from exception text or invented when a provider exposes none.

`HolonDeliveryRecoveryEvidence` contains the explicit reconciliation flag and a detached, read-only, ordinal-deduplicated list of opaque original identity strings. A marker without an exposed id is represented by true plus an empty list. Opaque strings are preserved exactly, including structured native session/transaction references and values longer than the error-message bound. The DTO accepts no exception, signed command or serialized application object.

## Strict worker capability

`IHolonReplicationRecoveryOutbox` extends the existing outbox contract with fenced evidence-bearing completion and read-only inspection. Existing claim/renew/finish entry points remain available, but `HolonOutboxDispatcher` requires the stricter capability. It never drops recovery evidence to use a weaker completion path.

SQLite uses the same native writer transaction and lease-token comparison as delivery-state completion. The versioned recovery JSON and SHA256 are stored with the declared state. RequiresNativeReconciliation may only be persisted with ReconciliationRequired: neither Completed nor automatic Retry can override it. Stale workers cannot overwrite the current lease's evidence. A failed evidence update rolls back the state transition too.

Ordinary worker outcomes also record a versioned false/empty evidence value. Older obligations and direct calls to the original finish API can have no evidence; absence remains null and is not presented as a proof of native outcome. A delivery blocked for a lineage conflict can therefore differ from a native-uncertainty block without guessing from message text.

## Inspection and schema

`InspectAsync(sequence)` returns read-only operational identity, target, state, attempts, error diagnostics and typed recovery evidence. It excludes the business payload and claim token. It checks the evidence/digest pair, exact schema version/fields/types and agreement with the state, failing visibly on corruption. This is an internal trusted-host store API, not an authenticated HTTP operator endpoint or a per-tenant authorization service.

Migration `20261020000000_AddCosmicHolonRecoveryEvidence` adds nullable JSON/digest fields. Existing state and payload bytes are retained; missing old evidence is not synthesized. Downgrade refuses to discard retained evidence. The same declaration is applied by the existing EnsureCreated schema-upgrade owner. Checksums detect inconsistent storage, not malicious database writers or source authenticity.

## Verification

Five Core cases cover detached opaque identities, marker/identity admission and explicitly unexposed identities. Two worker cases cover nested/duplicate provider markers, rejection of ordinary exception text as an id source, and uncertainty without an invented identity. Eleven native SQLite cases cover reconstruction, identities over 4KiB, atomic failure, stale ownership, illegal completion/retry, checksum/schema/state corruption and migration retention. Full suites pass **1,017 Core unit, 566 shared unit and 657 SQLite native cases**, zero failures/skips. Core net10.0/netstandard2.1 and initial shared/SQLite producers build successfully. A cross-provider rerun initially stopped before native dispatch during the concurrent session's shared CAS-wrapper edit. After that owner corrected the edit and confirmed the build window, the final rerun passes **all four SQLite/MongoDB combinations**, zero failures/skips, against the stricter recovery store. Full results/hashes are recorded in [verification](VERIFICATION.md).

## Remaining gates

The evidence-bearing completion occurs after target I/O returns. It does not close a process-death window before that completion: durable target attempt preparation/native submission journaling and actual crash/wire-loss tests remain required. No authenticated reconciliation/resume, source baseline handover, target reconfiguration/backfill, retention/queue limits or default host scheduling is implemented by this evidence DTO. MongoDB primary enrollment and [delivery/inspection](MONGO_OUTBOX_DELIVERY.md) are now implemented; Neo4j remains open. This record is not proof that an uncertain target transaction committed or rolled back; an operator must reconcile the original receipt without replaying a user edit.

The concurrent provider session was notified before the additive Core capability change. Oracle/OCI per-key CAS work, published enum/pins and shared backend hooks are outside this batch and preserved.

MongoDB follow-on: the recovery codec is now shared without a format change. Mongo native outcome/evidence atomicity and original delivery-state commit-uncertainty proof are documented in [delivery evidence](MONGO_OUTBOX_DELIVERY.md).
