## Latest: actual cursor process restart verified — 26 October 2026

A settled three-provider page survives abrupt termination of its hosting process.
A distinct fresh host resumes the exact protected cursor using the unchanged
Windows DPAPI-protected persisted key. Three pages/nine provider attempts settle
through one parent reservation authority and the production HTTP authorization
client; a duplicate redemption is rejected without another execution start.
Readiness JSON is published atomically. This is actual process evidence, separate
from the earlier in-process host restart/key-rotation matrix.

Full native cohort:526settled operations/608physical attempts, unchanged sources,
seven matching producer/consumer pairs. Original twenty cursor requests and
existing writer-process recovery remain green. The owning fixture removed its
small test key/config directory after both process trees stopped; no private key
or controlled credential was archived. Native databases/services were cleaned.

Evidence: Verification/federation-cursor-process{.trx.gz,-second.trx.gz,
-source.json.gz,-binaries.json,-result.json,-pids.json}; earlier initial proof is
retained separately. Scope is same-user Windows DPAPI persistence. Multi-host,
container/KMS key deployments, broader failure windows, causal agreement,
repeatable distributed snapshots, distributed joins and the full remaining ORM
roadmap are still open. Core3,739 is the separate preceding regression gate.

# CQL federation HTTP v1

26 October 2026. Implemented with signed actual-controller acceptance on real MongoDB, SQLite and Neo4j:13requests,12settled operations,23provider attempts. Typed/decrypted results, per-target grants and exact durable billing are verified. Authenticated cursors and distributed joins remain open.

`POST /api/data/query-cql/federation` requires an authenticated WEB4 actor,
HyperDrive V2, the route's configured storage admission and explicit ordinary
query tariffs. It shares the existing CQL resource filter and usage attempt owner.
The request is at most 32 KiB. Unknown fields are rejected; clients cannot supply
grants, actors, tariffs, capabilities or executable query text.

## Scope and access

| Scope | Providers | Meaning |
|---|---|---|
| CurrentDefault (default) | Empty | Resolve and capture the existing default once. |
| Named | One to three unique concrete names | Every requested target must be eligible and available. |
| AllEligible | Empty | Select tariff-admitted MongoDB, SQLite and Neo4j targets; a required unavailable target fails. |

Concrete names are MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS. There is no
activation, implicit failover, retry or silent omission. The application-owned
ICqlReadAccessPolicy is captured per target before any native dispatch. Every
policy must bind the captured actor and that provider's authority. A later denial
rejects the entire request before earlier authorized targets can execute.
Unenrolled hosts retain the explicit owner/public contract; required-policy hosts
fail closed when their authorizer is missing or invalid. Metadata labels do not
grant access.

## Independent source and global windows

`source` is the existing bounded CqlV1Request filter/order description. Its
Provider must be omitted and Graph must be null. Source Take is 1–100 (default25),
with the existing source Skip bound. These windows apply independently at each
provider, after its mandatory access predicate. Global Skip/Take apply after
identity selection and global ordering. A source window deliberately limits the
participating source; results outside that window are not part of this intent.

IdentityMode is PreserveCopies (default) or ProviderPriority. The latter requires
ProviderPriority to name every resolved target exactly once. Priority selects a
deterministic provider before the global page; it does not mean newest. Observed
accessible copies are retained. CausalAgreementEstablished is false. Repeatable
snapshots, causal agreement, continuation cursors and distributed joins are not
part of this HTTP version.

## Budgets

| Property | Default | Admitted range |
|---|---:|---:|
| Skip | 0 | 0–1000 |
| Take | 25 | 1–100 |
| MaximumInputRows | 300 | 1–300 |
| MaximumRowsPerProvider | 100 | 1–100 |
| MaximumInputBytes | 4194304 | 1–16777216 |
| MaximumOutputBytes | 1048576 | 1–1048576 |
| MaximumWork | 100000 | 1–10000000 |
| MaximumProviderAttempts | 3 | 1–3, at least the selected target count |
| MaximumDurationMilliseconds | 5000 | 1–5000 |

The same cumulative budget spans complete native inputs, reconciliation,
ordering, typed mapping/decryption and canonical output. The successful public
JSON body must also fit MaximumOutputBytes and the one MiB hard response limit.
Errors return no partial rows. Native attempts already performed retain their
receipts when a later target or output check fails. Input truncation is never
reported as a complete source.

## Example

Use the application's WEB4 bearer token and its existing admitted route setup.

```json
{
  "scope": "Named",
  "providers": ["MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"],
  "source": {
    "filters": [
      {
        "metadataPath": ["category"],
        "scalar": "String",
        "operator": "Equal",
        "value": "coffee"
      }
    ],
    "orderBy": "Name",
    "skip": 0,
    "take": 100,
    "includeMetadata": false
  },
  "identityMode": "ProviderPriority",
  "providerPriority": ["SQLLiteDBOASIS", "MongoDBOASIS", "Neo4jOASIS"],
  "skip": 0,
  "take": 10
}
```

The OASISResult result contains version cql-federation-v1, captured providers,
MovingProviderReads consistency, ObservedInputRows, ResolvedRows and rows. Each
row has selected/copies, each with provider and the existing public CQL leaf row
DTO; provider keys, opaque revisions and runtime objects are not exposed.
SelectionReason and CausalAgreementEstablished describe the explicit choice.
Counts include only authorized participating source rows. The selected row and
each observed copy are projected using their own target policy.

Authentication/admission failures are rejected before lookup or execution.
Malformed intent returns400; authorization denial403; missing/invalid required
policy503; an expired cumulative deadline504; canonical/public output overflow413.
Other execution failures preserve their explicit error code. Error responses may
contain the error envelope even when a caller selected a very small output limit.

## Verification and remaining gates

Core dual-target and shipping WEB4 builds passed. Full Core3,636 tests passed,
including nineteen hosted federation cases covering three target policies,
source/global paging, exact per-target tariff receipts, captured actor/default,
late denial before dispatch, invalid descriptions, authentication/admission,
unknown grant/tariff fields and actual JSON wire size. The existing shipping WEB4
suite passed320 with one existing Stripe sandbox skip.

Retained proof: Verification/federation-http-{core,web4,initial-core}.trx.gz and
federation-http-identities.json. Signed native controller/grant/decryption/billing and annotated SDK/HTTP demo acceptance now pass; see the native checkpoint below.
See CQL_FEDERATION_CONTRACT.md and CQL_COMPLETION_ROADMAP.md.

## 26 October: signed native federation and demos verified

The actual shipping controller passes13requests (12admitted/settled, one anonymous)
with23durable provider attempts on real MongoDB, SQLite and Neo4j. Cases cover
provider-specific private grants, visibility before global paging, priority/copy
provenance, revocation, late denial and authority mismatch with zero attempts,
input/output exhaustion, empty sources, schema rejection, captured default,
three-store metadata decryption, wrong-key rejection and unchanged stored
ciphertext. Every admitted operation settles with the expected exact tariff cost;
completed read receipts survive later service errors. No billing account is used.

The reusable demo also executes the spaced/commented
Examples/CosmicCqlDemo/V1FederationWalkthrough.cs: one global SDK example and one
signed global HTTP example, in addition to138SDK/138HTTP per-provider examples.
Swagger has a federation request example; its token interceptor derives allowed
paths from the shared CQL catalogue, including composition and federation. Demo
federation admission reserves three attempts; leaf routes retain one.

The complete cohort passes505settled operations/567attempts,14SDK federation
scenarios and process-restart recovery on all3providers. Native source snapshots
are unchanged and seven producer/consumer pairs match. The controller's source
hash matches the earlier HTTP capture; the reusable snapshot list now includes
that file explicitly for future runs. Owned fixtures/services were verified and
removed; no installation, download or new environment was used.

Evidence: Verification/federation-hosted-native{.trx.gz,-source.json.gz,-binaries.json,-result.json}
and federation-hosted-controller-identity.json. Core3,636 tests and the existing
shipping320tests/one Stripe sandbox skip are separate prior regression gates.
Authenticated cursors, causal agreement/repeatable distributed snapshots,
distributed joins and full remaining ORM roadmap acceptance remain open.

26 October HTTP cursor integration controlled gate: explicit opt-in, protected original intent/offset/pinned targets/fixed expiry/next key, fresh target policies and durable admitted-handle binding implemented. Shared budget-owner read-set preflight runs before dispatch; per-read capture also respects source Take. Whole-request active time, host work and canonical+reserved wire bytes accumulate; final protection/framing reserves100ms/4096token characters with checked publication. Exhausted chains report hasMore with null continuation/BudgetExhausted. Core dual-target build/all3,737tests and shippingWEB4build pass, ten new endpoint/protocol cases (including three pages, concurrent atomic claim, changed key/intent/policy and captured default). No native continuation acceptance yet; earlier505/567 evidence predates this feature. Existing outbox owns replay; no key store, environment or provider I/O in this gate. See CQL_MANUAL.md for enrollment and request sequence. Signed/native paging, key rotation/restart, uncertain failures and broader distributed features remain open.


Final HTTP cursor gate: all3,739Core tests pass, twelve new endpoint/protocol cases including missing enrollment and depleted protected state preserving no-dispatch evidence. Shipping build passes with original257warnings; new host XML/nullability warnings corrected. Required Verification/federation-cursor-http-{core,initial,first-core,expiry-core}.trx.gz and federation-cursor-http-identities.json retained. Whole-request active time includes host preparation/execution/projection and checked100ms finalization reservation; fixed expiry bounds dispatch/publication. Signed native continuation/restart/key-rotation and broader roadmap acceptance remain open.

26 October native cursor gate verified: actual shipping controller20requests/18settlements/32attempts, two duplicate rejections, three global pages, pinned default, changed-key/intent/policy/expiry/budget controls, two fresh host restarts and real Data Protection key rotation/revocation over bounded memory-only XML repository. Full native cohort523settlements599attempts; unchanged source snapshots/seven matching binary pairs; owned fixture/services cleaned. Evidence Verification/federation-cursor-native{.trx.gz,-source.json.gz,-binaries.json,-result.json}. This proves host restart, not OS-process cursor persistence; that acceptance and broader distributed/ORM gates remain open. CurrentCore3,739 is a separate earlier regression gate. See WEB4_VISION_AND_AI_NEXT_STEPS.md for strategy and evidence boundaries.
