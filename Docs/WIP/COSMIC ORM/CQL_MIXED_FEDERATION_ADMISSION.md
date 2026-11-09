# Mixed native/indexed federation admission — Mongo + Solana

28 October 2026. **Open native demo requirement**. Shared planning and mixed SDK execution now implemented/controlled-tested; live Mongo+Solana and HTTP/browser acceptance remain unverified.
Existing native federation and single-source indexed queries stay frozen except for
shared changes necessary to implement this missing composition. No cursor work.

## Reproduced boundary

`CosmicMixedFederationAdmissionTests` passes2 admission cases:

1. Capturing a Solana index grant on Mongo+Solana scope rejects CQL_SOURCE_SCOPE_INVALID.
2. Passing an indexed Solana target to native federation without a bound read requirement
   rejects CQL_SOURCE_BINDING_MISMATCH. No physical read is possible from that plan.

These are correct current rejections, not evidence of mixed federation support.
The retained Solana browser gate is separately green within its declared scope.

## Original gap audit (superseded by implementation checkpoint below)

- CosmicQuery.WithIndexedSource requires exactly one named logical source and freezes
  the requirement. This invariant must remain:Mongo cannot inherit a chain grant.
- CosmicFederationPlanner pins the same source query to each target; it has per-target
  read policies but no per-target index/freshness requirement capture.
- CosmicFederationInputCapture registers only ICosmicQueryStorageProvider adapters,
  admits native capability paths and dispatches CosmicQueryExecutor.
- CosmicIndexedQueryExecutor separately owns row binding, independent source reads,
  finalized head/checkpoint/freshness, receipt identity and completeness admission.
- CosmicFederationCopy/Result and HolonManager materialization currently preserve
  native provenance but have no explicit indexed checkpoint/head/receipt envelope.

Do not wrap the index as a native Solana adapter or insert evidence into user metadata.
Do not turn off source matching or apply one global index grant to both providers.

## Acceptance sequence (steps1–3 now implemented and controlled-tested)

1. Capture explicit immutable target-specific indexed read requirements in shared
   federation planning. Match every registered source/index/schema/catalogue/range;
   reject missing, foreign, duplicate, re-bound or unrequested bindings before I/O.
   Native Mongo remains native. Existing scope/read-policy intersections are preserved.
2. Add explicit indexed reader+independent observer registration to execution. Validate
   all native/indexed target bindings before first read. Dispatch each leaf through its
   existing owner; no implicit mode switching, retries, scans or result substitution.
3. Preserve indexed source/checkpoint/head/row receipts alongside copy provenance,
   through merge and HolonManager typed materialization. Charge evidence bytes/work,
   attempts, deadlines and native/indexed reads to the same cumulative operation.
   Do not weaken prepared-execution/receipt ownership or bypass billing admission.
4. SDK/controlled tests: mixed happy case, bad binding before any native read, stale head,
   receipt mismatch, source failure after Mongo success, cancellation/budget/all-input
   errors, access-before-window, provenance and exact final materialization.
5. Native Mongo+retained Solana acceptance:journal only synthetic Mongo writes; reuse
   schema3/coverage3325/index86d676b6-c2c5-4c8f-98b2-a88e0fb8bb11 and original source.
   No reseed/source transaction/reset. Prove Mongo and chain-index contributions,
   preserve source evidence, cleanup exact Mongo fixture and shut down validator.
6. Wire readable C#/HTTP/browser preset; verify actual browser controls and docs.

This is a new missing execution contract. A native-shaped shim that erases indexed
assurance would make the demo misleading. It is not grounds to resume paused LINQ depth.
Hosted Azure is independently pending a verified free-only authenticated setup.

Evidence: TestResults/DemoBreadthCurrent/mixed-federation-admission.trx/log.
No native service, environment, install, dependency restore or source write in this audit.

## Implemented planning checkpoint (not execution support)

The shared planner now has an explicit overload accepting a bounded list of immutable
CosmicFederationIndexRequirement grants alongside existing read policies. Each grant
must name exactly one requested authorized indexed target and match its complete
source/index binding. Missing/duplicate/foreign/unrequested grants reject before I/O;
source-wide captured grants cannot be rebound. Native Mongo keeps IndexedSource=null;
only the pinned indexed target captures its read/freshness requirement. Caller-list
mutation after planning cannot change captured target queries.

Both Core targets build;2198 CQL tests pass, including8 mixed-admission cases. Ordinary
native executor still rejects indexed work before either adapter reads. This planning
milestone does not certify mixed execution or a Mongo+Solana demo. Exact next step:
explicit indexed reader/observer registration, all-target validation, existing indexed
executor dispatch, evidence retention and cumulative budget accounting (steps2–3).
No native adapter shim, metadata evidence injection, cursor changes or resumed LINQ depth.

## Implemented dispatch/evidence checkpoint

Explicit `CosmicFederationIndexedReader(reader, observer)` registration supplies the
indexed leaf separately from native adapters. All source/index/capability/access and
prepared billing bindings validate before the first read. Dispatch uses the existing
CosmicIndexedQueryExecutor, retaining its receipt/freshness/visibility/completeness
checks. Either leaf failure rejects the entire federation; no partial publication.
Native adapters do not receive indexed grants or impersonate the chain index.

Each indexed copy retains IndexedEvidence(Read.Checkpoint,Read.Head,Receipt).
Result.IndexedReads retains checkpoint/head even with no indexed rows. HolonManager
preserves both through typed hydration and clean tracking. Evidence stays outside user
metadata and is included in cumulative input/output byte/work accounting. The same
operation's row/attempt/deadline bounds span both native/indexed leaves. Existing
prepared indexed tariff/actor/reservation checks now have a pure preflight shared with
leaf dispatch; no admission bypass or replacement receipt owner.

Both Core targets build;4298full Core tests pass.16new execution cases cover mixed
raw/manager success, empty evidence, missing/changed/duplicate/access/cancel preflight,
stale/identity/visibility/duplicate/incomplete failures after native success, prepared
billing rejection before dispatch, and evidence-only input/output budget exhaustion.
No native Mongo+Solana runtime acceptance yet. Exact next:step5 using retained source,
index and acknowledgement without reseeding, then explicit teaching HTTP/browser.

## Native Mongo + Solana checkpoint

Live shared mixed SDK acceptance now passes:2physical copies(Mongo1/Solana1) of the
retained synthetic logical ID, native Mongo control excluded, original chain commit/
version identity and source/index/checkpoint/head preserved. Per-provider authoritative
access bound before windows. Exact synthetic Mongo cleanup and both service shutdowns
verified. submittedNewSeed=false/sourceWrites0. [Proof](CQL_MONGO_SOLANA_NATIVE_VERIFICATION.md).
Actual mixed HTTP/browser preset remains open; no hosted-chain/production auth claim.


Mixed HTTP/browser is now verified for Mongo+retained Solana on loopback5091:
2matching physical copies and0empty, retained chain receipt/version and indexed read
assurance for empty output, independent bounded ingestion and exact Mongo cleanup.
See [live proof and launcher](CQL_MONGO_SOLANA_NATIVE_VERIFICATION.md#mixed-httpbrowser-milestone).
Production mixed WEB4 authentication/billing and hosted chain acceptance remain open.
