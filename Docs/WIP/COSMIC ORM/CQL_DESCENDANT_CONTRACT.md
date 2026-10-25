# Bounded native descendant existence

23 October 2026. Native descendant checkpoint; the full roadmap is not complete.

## SDK and admission

```csharp
var limits = CosmicQueryTraversalOptions.Create(
    maximumDepth: 4,
    maximumNodes: 250);

// Check limits.IsError before using Result.
var query = CosmicQuery<Holon>.Create()
    .WhereAnyDescendant(child => child.Name == "target", limits.Result)
    .OrderBy(parent => parent.Name)
    .Take(10);
```

The immutable node freezes the child expression with the existing scalar compiler. Traversal does not request graph output. WithChildren remains independent. Bind trusted read access before the root window; it applies to every hop. WhereAnyDescendant after Skip/Take rejects rather than changing query order. Fluent OASISResult composition preserves the first error.

Options require depth1-32 and nodes1-10000; defaults are depth8/nodes1000. Adapters declare DescendantExists1048576 together with ChildExists and an immutable CosmicQueryTraversalCapability. Its explicit certified bounds can be smaller than the generic absolute maxima. Missing capability or excessive requested bounds rejects at planning and actual-adapter readmission, before dispatch. A supplied plan cannot lend its traversal budget to the executing adapter. Existing public capability constructor signatures are preserved; new overloads carry traversal limits.

MongoDB, SQLite and Neo4j declare DescendantExists with depth32/nodes10000. Direct-child SDK/HTTP queries remain supported on all three stores; bounded descendant HTTP groups are implemented with shared relationship/filter budgets.

## Required native semantics

- Roots match if at least one qualifying canonical descendant is reached within MaximumDepth. Direct children have depth1; the root never matches itself.
- Each traversed edge requires current ParentHolonId and declared canonical role membership, or the explicit absent-manifest parent-link representation. Reparenting must invalidate an old path. Dangling declarations do not manufacture nodes.
- Mandatory trusted access applies before visiting/inclusion and before traversal through an intermediate node. An inaccessible branch cannot make a match or increase the logical visited-node count.
- MaximumNodes limits distinct visited visible canonical descendants for each root and relationship condition, independently of native whole-command work/deadline/byte budgets. The bounded reachable scope must satisfy the node budget before a result is published; overflow returns CQL_QUERY_LIMIT even when a matching node was encountered earlier. Never publish a positive or negative match from a truncated scope.
- A depth boundary deliberately limits the question; unvisited descendants beyond it are not an error. Duplicate equivalent physical edges are deduplicated. Conflicting canonical identities or an encountered identity repeated on the same path produce CQL_DATA_INVALID, rather than infinite traversal.
- Native traversal/filtering precedes root sorting/Skip/Take, under the provider's declared consistency owner. GraphSnapshot must not be inferred for Neo4j. No cross-provider common snapshot is implied.
- Cancellation/deadlines and invalid data publish no partial rows. All root/relationship work belongs to the admitted physical attempt and accounting receipt.

## Verification boundary

Core tests cover bounds, immutable expression/actor binding, independent output shape, post-window rejection, first-error preservation, explicit capability consistency, certified depth/node admission and actual-adapter zero-dispatch rejection. Native membership, policy pruning, cycles/duplicates, budgets, concurrency and transport evidence remain necessary before a provider may advertise this capability.

See [completion roadmap](CQL_COMPLETION_ROADMAP.md) and [direct-child semantics](CQL_RELATIONSHIPS.md).

Core23da2dcb5fc44d2bcbc447aaca5d7cec4a963840 verification39427 terminal0: fullCore1815/Shared968/joint98 pass unskipped. net10.0 and netstandard2.1 build cleanly. Final consumer source/PDB712Core/54Shared match CoreE6E7EE404AA0B2282036291A868E5C7F3CA108C3CE26148AD0F39EF866D44733 / Shared64C24839C7AD46FD710D5DB6DB32610374DC55B1A57B984E8C81A44469E19980. At that earlier contract-only checkpoint all adapters rejected native descendants; SQLite is implemented in the newer section below.

## SQLite native execution

One correlated recursive CTE traverses canonical membership/current-parent edges and trusted visibility at every hop, tracks depth/path/cycles and caps materialization at nodes+1. It validates the bounded scope before matching the scalar predicate, then roots are sorted/paged. Independent clauses share the existing900-binding/native VM/deadline owner. WithChildren remains independent; descendant queries alone return leaf rows. A deferred Serializable read owner supports read-only connections and reuses caller Serializable transactions without committing them.

Connection-local provider functions record explicit cycle/node/manifest outcomes; the owning reader propagates typed errors before any result publication and unregisters both functions before releasing the connection. Business predicates/traversal stay in SQL. Canonical manifest validation uses the single strict Core parser under the native read budget, with only one cached parent observation. Both direct-child and descendant queries validate a parent's observation before testing child existence, including empty parents. Malformed source cannot silently become a negative match.

FullSQLite1248 and focused45 native relationship/graph controls pass, zero skips. A genuine empty-parent malformed-manifest red2 proved the omission and green controls prove the common-owner fix. Tests cover depth1/2, partial graph-independent output, exact Int64, independent clauses, root paging, hidden intermediate pruning, unlisted branches, node overflow/reuse, cycles/function teardown, read-only storage and caller transaction rollback. Broader concurrency, cancellation and all-provider descendant certification remain roadmap work.

Final SQLite58545/joint96796 terminal0: full1248/focused45/joint98 pass unskipped. Consumers match712Core/54Shared source/PDB: CoreE6E7EE404AA0B2282036291A868E5C7F3CA108C3CE26148AD0F39EF866D44733 / Shared45E098A99A5308A7DC78B9ECD8CCD3EC969A45DCCC97F09CC9F3846DE7A93617. Fixed SQLite fixture and owned HTTP/native resources are absent; reusable services stopped.

## Neo4j native execution

Neo4j uses quantified path patterns with inline EXISTS membership/current-parent and trusted child access predicates, pruning excluded branches during traversal. This feature requires Neo4j5.9+; the retained native certification runtime is5.26.12. The official [Neo4j5 path-pattern guide](https://neo4j.com/docs/cypher-manual/5/patterns/variable-length-paths/) documents inline pruning. No APOC, new procedure or alternate evaluator is installed.

Each scope retains distinct endpoint/cycle/match states capped at nodes+1; duplicate equivalent physical edges cannot inflate the node budget. Simple prefixes retain their first repeated endpoint as a typed cycle outcome. Parent and child projection lookups use separate MATCH clauses so a self-loop can reuse its one canonical projection relationship and reach cycle validation. Independent conditions retain separate scope aliases and exact scalar parameters.

Native error scopes are outside root sorting/Skip/Take, under the same read-committed transaction/deadline owner as result scopes. Cycle or node-budget failures publish no rows; the reader consumes typed native flags. Root business/access predicates restrict the scope before traversal. Read committed remains explicit; no snapshot guarantee is inferred. WithChildren remains independent of filtering.

Final50927 terminal0: Neo316unit/45native/joint109 pass unskipped. Positive/negative controls cover depth, private intermediate pruning, exact Int64 and independent clauses, unlisted branches, duplicate edges, budget/reuse, root paging and cycles. Root cause red1 after introducing inline pruning identified MATCH relationship uniqueness suppressing a self-loop; separate canonical projection lookups fixed it. Earlier4failed assertions were an incorrect fixture-name literal and were corrected separately. Two completed-stage fixture reconciliations used exact hashes and validated native absence/all9SQLite rows before removal; no operation replay/reset.

Consumers match712Core/54Shared source/PDB: CoreE6E7EE404AA0B2282036291A868E5C7F3CA108C3CE26148AD0F39EF866D44733 / SharedF51B52FE83B3F22422BEED382F2EA2B53AB7D0323176FC73BAC432032AD8692B. The newer Mongo owner is described below; broader acceptance gates remain open; bounded descendant HTTP is implemented.

## MongoDB native execution

Mongo uses a flat breadth-first aggregation, with one correlated lookup per depth level. It supports the generic depth32/node10000 bounds without recursively nesting lookup pipelines. Every hop derives current parent identity, canonical declared membership and trusted child visibility before inclusion. Child predicates are compiled once through the existing exact scalar translator; roots are excluded from their own business matches. No client collection scan, temporary collection, JavaScript function or native extension is used.

Each bounded lookup fetches at most MaximumNodes+1 visible canonical candidates. An independent logical node counter and visited-ID set span all levels of one root/condition. First repeated/conflicting identities produce a data error; overflow remains an error even when a match was found earlier. Further lookups get an empty frontier after a failure. Every condition completes before the final business conjunction, so an earlier negative match cannot hide a later scope overflow.

A protected native envelope keeps persisted fields such as row/error/frontier/outcomes from shadowing internal state. A separate facet branch preserves the first error outside root sorting/Skip/Take; the reader checks the provider-owned error field before publishing any detached row. Positive and error outcomes share the same snapshot as canonical membership validation and the existing aggregate/graph deadline owner. Graph output remains independent.

The retained certification engine is Mongo8.3; other engine versions have not been certified by these runs. Native limits are additional to logical traversal limits: the existing command BSON1MiB/depth90 guards, a preallocation/pre-dispatch1000-stage guard, no disk spilling, engine stage memory limits, native result/document limits and the existing30-second/64MiB read budgets. Mongo documents [pipeline limits](https://www.mongodb.com/docs/manual/core/aggregation-pipeline-limits/) and the [facet memory limit](https://www.mongodb.com/docs/manual/reference/operator/aggregation/facet/).

An actual native negative control lowers the owned replica's facet budget to1byte rather than allocating a large dataset. It exposed the mapper handling292 but missing named146/ExceededMemoryLimit. The mapper now recognizes both; the [Mongo8.3 facet executor](https://github.com/mongodb/mongo/blob/r8.3.0/src/mongo/db/exec/agg/facet_stage.cpp) uses the named memory error. The test verifies146, CQL_QUERY_LIMIT, absent partial output, exact parameter restoration in finally and successful reuse.

The first completed native batch passed58 cases; the expanded negative batch passed59/60 and exposed the mapping defect. After that root fix,219 unit tests,60 native cases and109 joint cases passed without skips. Final40754 actual terminal0:220 unit tests,61 native tests (19 descendant controls) and109 joint tests pass with no skips, including both-order negative/overflow scopes and preallocation rejection. Source/PDB712Core/54Shared verified: CoreE6E7EE404AA0B2282036291A868E5C7F3CA108C3CE26148AD0F39EF866D44733 / Shared325CF326FCF0B58D6E2FBAAF84BBE27CE4851112DE500F11E5D2A2707CEB038A. Evidence is retained in TestResults/CosmicCqlDemo/mongo-descendant-source.log; no Core/Shared source change is part of this provider checkpoint.

WEB4 anyDescendants now compiles through this immutable contract with server actor binding, explicit depth/node bounds and shared direct/descendant group/filter budgets. FullCore1830/joint109 pass, including owner/outsider signed native pruning and denial-budget controls across all3 stores. No production Core/Shared changes. See [transport](CQL_V1_HTTP_CONTRACT.md#bounded-descendant-existence-over-http).

## Exact-boundary and moved-subtree certification

The certified acceptance fixture creates a real33-edge canonical chain (34nodes), rather than passing depth32 over a shallow graph. Identical linked test code asks depth31/nodes31 for node32 (absent), depth32/nodes32 for node32 (present), depth32 for node33 (absent) and depth32/nodes31 (typed overflow without rows). The existing provider save path seeds it; no alternate representation is introduced.

Existing owned reparent fixtures also now query a grandchild through the moved intermediate: the old path must stop matching despite its retained declaration, and the new path requires declared membership or the explicit absent-manifest parent representation. Both declared-empty and absent-manifest cases are included on each store. Final88038/45463 terminal0: fullSQLite1249/focused46, Mongo62 and jointNeo110 pass without skips. New same-source linked controls run natively on all3stores; source/PDB checks match all3consumer binaries to CoreE6E7/712 and Shared9AB8A4F9CEA284087F249AFDA1D2D630814C8C48CF7CE0C3CF151595D9B59E81/55. SQLite consumer was refreshed through its normal build after an assembly-stamp change; no DLL copy or identity-guard bypass. Fixed fixtures are absent and native runtime teardown completed. No new runtime/database layout or Core/Shared production edit was required.

## Read-interruption and concurrent-reparent controls in progress

SQLite48 andMongo64 pass current controls. SQLite cancels after actual native reader creation, verifies no result/loaded flag and released transaction/connection, then succeeds on reuse. Its WAL reparent control observes the old path at the reader boundary and the new path on a fresh query. Mongo pins the membership snapshot, blocks traversal through an owned native aggregate failpoint, performs an acknowledged intermediate reparent and verifies old in-flight/fresh membership results, with declared-empty and absent new-owner cases.

All4newNeo controls pass: cancellation after actual Fetch without publication and successful reuse, a31-second cursor-delivery delay classified by the30-second budget, and both new-owner reparent variants. Read committed permits the already-fetched old result; fresh queries must reflect current membership. The joint114 run initially passed112 with an HTTP/1 transport incident and dependent retained-namespace guard; those are not failed Neo traversal controls. Exact-hash fixture reconciliation now passes: 133 settled operations and two absent trailing operation IDs were verified before cleanup. The fresh joint 114-case run and full Core 1,834-case run pass with zero skips. Strict interruption of an outstanding native driver await remains a separate open gate.

These post-read controls do not prove prompt cancellation of an already-blocked Neo driver await. The retained driver4.2 exposes no cancellation token on the inspected async query/cursor methods; current budget checks surround awaits. Strict in-flight await/deadline latency and interruption ownership remain an explicit open audit gate. Do not describe the delayed-delivery classification test as proof of a hard wall-clock abort.

## Remaining blocked-I/O cancellation audit - 25 October 2026

Neo4j CQL creates a cumulative 30-second read budget and a server transaction
timeout. Its budget checks surround cursor delivery, but the awaited driver
BeginTransactionAsync, RunAsync and FetchAsync calls do not receive a caller
cancellation token. Existing tests prove cancellation after an actual fetch and
a 31-second delayed delivery returning timeout; they do not prove interruption
of an indefinitely blocked driver await. Strict cancellation remains unchecked.

The actual native test consumer resolves **Neo4j.Driver.Signed 4.3.2**, not the
also-cached unsigned 4.2.0. Its DLL matches the cached net5.0 signed DLL at SHA256
27D1AE1380B2073633D1A5D933FF6C5F2741133F3029DE450123CB0017CC6BB9.
Neo4jClient 4.1.18 and Neo4jOgm 0.21.8 bring the driver dependencies transitively.
The pinned XML API exposes async close/rollback, but does not establish that
concurrent close safely interrupts a blocked call. Do not implement detached
Task.WhenAny cancellation, global driver disposal or privilege-dependent server
termination as an implicit substitute for that missing lifecycle guarantee.

Next acceptance requires native blocked-call cancellation/deadline tests for
rows, projections, scalar and grouped queries, no partial publication, bounded
teardown and subsequent owner reuse. Verify the driver cancellation/teardown
contract before changing execution ownership. No dependency change or runtime
installation has been made during this audit.

Primary references: [Neo4j 4.3 async session source](https://github.com/neo4j/neo4j-dotnet-driver/blob/4.3/Neo4j.Driver/Neo4j.Driver/Internal/AsyncSession.cs)
and [official session close contract](https://neo4j.com/docs/api/dotnet-driver/current/api/Neo4j.Driver.IAsyncSession.CloseAsync.html).
The latter describes current documentation; pinned 4.3.2 XML was inspected locally.

### Native blocked-response reproduction

Focused gate 48483 reproduced the cancellation gap against the retained Neo4j
server and actual signed driver. An in-process loopback relay forwarded real Bolt
setup, then held one server response. Caller cancellation did not finish within
one second while that response was held. Releasing the exact bytes allowed the
read to finish with CQL_QUERY_CANCELLED and no published result. This is a real
blocked transport await, rather than a delayed post-fetch callback.

The probe failed at the intended interruption assertion. Its finally block
released the response and joined the read before deactivation; the fixture
journal and owned relay/listener were absent afterward. Evidence:
TestResults/Neo4jCurrent/neo4j-cql-interruption-current.trx (SHA256
E6EA596051994CA01FC37042257A2816611F650E87AA13E550E05868F9BA719F) and
TestResults/CosmicCqlDemo/neo-blocked-cancellation-red.log.

The new regression and focused runner mode remain uncommitted pending the
production fix. Existing passing cancellation tests do not supersede this red
result. Source-level evidence also confirms rollback drains unconsumed results
before closing: [pinned driver transaction implementation](https://github.com/neo4j/neo4j-dotnet-driver/blob/4.3/Neo4j.Driver/Neo4j.Driver/Internal/AsyncTransaction.cs).

### Owned transport integration: warm pass, cold handshake still red

The candidate lifecycle now leases a bounded reusable, exclusive driver for each
CQL read owner across row, projection, scalar and grouped execution. Its linked
30-second timer includes pool waits and native I/O. Cancellation closes only the
owned driver; teardown is joined before a retired slot is replaced. Four pool
unit controls pass, and all provider source compiles without new dependencies.

Focused native gate 45642 passes both warm-connection cases and fails both cold
handshake cases. The signed 4.3.2 driver can abort established I/O, but closing it
does not stop a connection still negotiating its first Bolt response. After the
relay releases that response, both cold reads return cancellation with no data.
The stronger four-case probe is retained unchanged; warming connections alone
would not certify cancellation during reconnects.

Evidence: TestResults/Neo4jCurrent/owned-transport-cold-warm-red.trx,
read-transport-pool-current.trx and TestResults/CosmicCqlDemo/
neo-blocked-cancellation-green.log (the filename records the attempted gate,
not its outcome). Actual result is **2 passed / 2 failed**, no skips. All owned
reads were joined, the fixture journal removed and ports17788/17687 absent.

The lifecycle and tests remain uncommitted until the cold connection creation
owner is fixed and broader regressions pass. Inspecting newer public driver
APIs has not established a drop-in fix for explicit-transaction cancellation;
no driver package upgrade or source download has been performed. Next work
must fix connection-creation ownership rather than weaken this acceptance test.

### Patched cold/warm transport gate — 25 October 2026

Gate 95686 completed successfully: all four real Bolt relay cases pass, zero
skips, in three seconds. Cold and warm reads return CQL_QUERY_CANCELLED, with
no loaded/result publication, before the held response bytes are released.
Both caller-only cancellation and the explicit exclusive-driver close controls
pass. This supersedes the earlier two-cold-case red result for this precise
probe; it does not certify every query shape or DNS/TCP/TLS cancellation phase.

The runner verified source-driver producer/provider/native consumer identity
20A42D3B68F7FF531DAD1B755AA9117F048D11A896B22A9969E599213E4E3324,
plus current Core and Shared producer identities. The integration test project
now disables transitive build-time OpenAPI generation because its entry point
belongs to the test platform; shipping HTTP implementations remain linked.
Concurrent peer SQLite source edits mean this Neo-only result is not SQLite
source/PDB certification.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-interruption-current.trx,
SHA256 A0E4E7A182C4EFDEF7541E321AD78A28035896B7B436D559A6E47D456A3773A5;
TestResults/CosmicCqlDemo/neo-patched-driver-interruption.log.
Owned runtime stopped, fixture journal absent and TCP listeners17687/17788
absent after terminal. Full Neo CQL/unit regressions, all-shape interruption,
deadlines, teardown failure and reuse acceptance remain required before commit.

### All native query shapes — 25 October 2026

Gate81715 passes all10cases, zero skips, four seconds: cold/warm row, projection,
scalar and grouped cancellation, plus explicit row driver-close controls. Each
executor returns CQL_QUERY_CANCELLED, IsLoaded=false and default/no result before
held Bolt bytes are released. Source-driver hash remains20A42D3B...E4E3324.
TRX SHA25687EBE625105CA564098C79E321368D2194767DEA7F769B636CC25F602A145A12;
log TestResults/CosmicCqlDemo/neo-all-shape-interruption.log. Owned runtime stopped,
ports17687/17788 and fixturejournal absent after terminal. No new resources or
packages; reusable build outputs retained. Full regressions/deadline and broader
connection phase certification remain open. SQLite edits settled before this
build; this Neo-only gate makes no SQLite behavior certification claim.

25 October full Neo unit regression:465 executed,464passed/1failed/0skip.
New factory-failure capacity recovery and failed-close retirement controls passed.
Existing OriginalConnectivityAndCloseFailuresAreNotSuccess caught premature
provider deactivation publication. Neo4jOASIS now retains storage/read-pool
references and activation state until both close tasks acknowledge success.
The follow-up rebuild stopped at the occupied normal global lease, before any
build/runtime work. TRX read-transport-full-unit-current.trx preserves the red
result. Fixed reusable log path neo-read-transport-full-unit.log; no install,
download or fixture introduced. SDK/cache/provider/unit bin/obj reused (sizes
unmeasured); compact TRX retained, generated outputs reusable. Updated production
fix still requires rerun. Full CQL native regression remains pending.

## Neo4j interruption regression checkpoint — 25 October 2026

The pinned source-driver/read-transport lifecycle batch passes full465provider
unit tests and259nativeCQLtests, zero skips. Native duration4m3s. All10cold/warm
paused-Bolt cases pass for rows, projections, scalar and grouped reads. Failed
creation returns pool capacity; failed close reports its error and retires the
transport. Deactivation retains resource ownership until both close tasks
acknowledge success. No result publishes on cancellation.

Exact evidence: native TRX SHA256
6CAAA45E0B7C362CD0BBC53BC2811ADAB56B2D01ABB98FB5FAFEA066FC1A46D6;
unit TRX SHA256
A02FB67264B525C91C8CC71ACC75D4664B6CC1E2104BF799E11201DF80556D06.
Both are under TestResults/Neo4jCurrent; compact build/run log is
TestResults/CosmicCqlDemo/neo-read-transport-full-regression.log.
Producer/native driver, Core and Shared hashes verified before execution.
Owned runtime stopped; graph fixture journal and listeners17687/17788 absent.
Free disk after gate64605769728bytes; the reusable Neo volume and generated
outputs remain retained/shared, size attribution unmeasured. No new download,
installation or environment during this gate. No shared data was deleted.

This is a verified scoped batch, not full CQL completion. Strict deadline latency,
DNS/TCP/TLS connection phases, broader non-CQL driver regressions, joins,
federation and the unchecked authorization/scalar/composition roadmap remain.
The full original COSMIC ORM goal remains active.

25 October identity correction: driver-specific friend compilation exposed that
upstream IdentityPubKey.snk tokenc08b0bc1c355082b differs from shipping signed
NuGet4.3.2 tokenb646bc66d277ac07. Prior identity compatibility statement was wrong;
previous passing cohorts used source-built consumers, not binary compatibility
proof. CompatibilityPublicKey.snk now contains ONLY the288-byte public key read
from cached Neo4j.Driver.Signed4.3.2/net5.0 DLL. No secret/privatekey/download.
KeySHA25697C3A47F03F35F1A1D7A663C00DC4593E5067CD762909B64AC15661CD320D2F1.
Originalupstreamkeypreserved. ExactprovenanceCOMPATIBILITY_IDENTITY.json;
buildhashguardincludescompatibilitykey, native runner checks exactshipping
assemblyFullName beforefixturestart, driverunitidentitycontroladded.
Testproject-onlyofflinecache restorePASS, firstbuildfailedfriendkeymismatch;
no DNS/nativefixturecreated. Key-correctedbuild/DNSredcontrol pending.

25 October gate53997 TERMINAL1: driver6PASS0skip; native249PASS/18FAIL0skip,
7m58s. All18failure reports originate PausedBoltRelay.DisposeAsync/PumpAsync network
read ConnectionReset (10054). Teardown exceptions can mask earlier assertions;
these reports do not prove that interruption assertions passed.
New connector omitted priorSocket.ShutdownBoth and directlydisposed unread
socket, changing gracefulteardowntoRST. Restoring explicitShutdownBoth before
stream/socket disposal at theconnectionowner. No exceptioncatch/testweakening.
FullnativeTRXretained; no graphfixturejournal remains. RunnerstoppedNeo, no
providerunit465executedafterfailednativegate. Regression outcomeisNOTgreen.
Sourceidentity b646 verified; consumerdriverhash678EC6F...A56C0509. Follow-up
connectorhash/provenancemanifest and native interruption retryrequired.

## Connection creation and native deadlines — 25 October 2026

Gate61225 completed successfully on the corrected shipping assembly identity:
6driverunits,18nativeinterruption/deadlinecases and465providerunits pass, zero
skips. Native18duration4m5s. Real30second deadlines cover cold/warm rows,
projections, scalar and grouped queries; caller cancellation remains covered.
DNS resolution/cache waits, closed-owner rejection and a held real TLS
ClientHello are covered by the driver tests. Shutdown is joined before reuse;
TCPShutdownBoth is preserved before stream/socket disposal.

NativeTRXSHA2562296A0EF80DA02FBA71C115700E77CE2B769CD98FE7A251A7F059069C9DA5E4B;
driverunitTRXSHA2569B5EDF30184E9B9DC609D7A813629D52385F99F2B142630583EF52A0424B3A3C;
providerunitTRXSHA2565E565A7A7A2CB4C325478DFF95E1C6F22ED790285A6F7D69E16FBCB2CF439087.
Currentfiles are in TestResults/Neo4jCurrent. Log:
TestResults/CosmicCqlDemo/neo-graceful-interruption-current.log.
Driverproducer/provider/nativeconsumer hash agrees:
C9A8D3524484B39671FD3CCAD97B7BB3AFEA566B06B93279C6F4C40B304857F3;
exactassemblyidentity is Neo4j.Driver4.3.2.2/publickeytokenb646bc66d277ac07.
Upstreamoriginalkeypreserved; CompatibilityPublicKey.snk/public-only288bytes
is independently hash-pinned. PATCH.json records9modifiedupstreamfiles.

OwnedNeo runtime stopped; listeners17687/17788/17789 and graphfixturejournal
absent. Freeafter65940660224bytes. ExistingSDK/cache/Neo volume and fixedbuild
outputs retained/reusable; sizesunmeasured. Newtestsource and compactevidence
retained; no download/install/newenvironment or unrelated cleanup. OwnerCOSMICORM.

The priorfull267run passed249normalcases but failed18relayteardowncases on the
pre-ShutdownBoth connector. This new18casegate supersedes those interruption
failures, but is NOT a freshfull267green cohort. Fullcurrent267/nativeWEB4/non-CQL
regressions remain pending, alongside joins/federation and the rest of the
unchecked CQL/ORM roadmap. No fullcompletionclaim; goalremainsactive.

## Committed full Neo4j CQL regression — 25 October 2026

Gate5299 passes all267nativeCQLtests, zero failures/skips,7m59s, on committed
867182ad8. This includes all18cold/warm caller-cancellation and actual30second
deadline cases. It supersedes the earlier249pass/18relayteardown failure for the
full native CQL cohort. Driver6/provider465unit checkpoints remain recorded
separately; no simultaneous new unit run is implied.

TRX: TestResults/Neo4jCurrent/neo4j-cql-native-current.trx, SHA256
05CD4F94E962676593FFE19BCEAFFF9EAC3628E922E804223165547976D176FA.
Log: TestResults/CosmicCqlDemo/neo-committed-full-cql.log.
The runner verified shipping assembly identityb646bc66d277ac07 and matching
source-driver/provider/nativeconsumer SHA256
6BC713F3D9C6F6771AC9C40AFA46E12160873F04FC9C01D180B9DB6A93C1E330.
Core9F4B2979 andSharedB7271397producer/nativeconsumer hashes also agree.
Concurrent SQLite ordinary-save count source changes are outside this Neo-only
behavior/PDB certification; no SQLite source-identity claim is made.

OwnedNeo runtime stopped; fixturejournal and listeners17687/17788/17789 absent.
Freeafter65228353536bytes. ExistingSDK/cache/Neo volume and fixedbuildoutputs
remain retained/reusable (individualsizesunmeasured), compactTRX/log retained.
No download/install/newenvironment or unrelateddeletion. OwnerCOSMICORM.

FreshSDK/HTTPdemo and broadernon-CQLregression acceptance remain separate gates.
Joins, federation, remaining scalar/operator/access and grouped-certification
requirements remain on the full roadmap. This is not full CQL/ORM completion.

25 October demo19161 TERMINAL1 beforeHTTP: newSQLiteordinaryselectionguard
rejectedfixture's hand-written persisted rolemanifest lackingstoreobservation.
Fresh87SDK/HTTPnotreached; oldreportpreserved/hashfenced. ExactmarkerE062B6BA6D240DE26CCD74D2F7DF0B4A9C2B923C569EEBA0BC7175BEA7ADA73F
stagebefore-create; originalfailureTRX9E2D50C429A563CDC95D21DEE4106D0C8A8D3B5F9D66463552EF4377F4A3AEA8.
Expectedacknowledgedpartialseed:Mongo9holons,SQLite3roots,Neo0;137scalarIDsnotwritten,
requests/scalarWritesempty. Pendingexact nativeverification beforeanycleanup.
PreserveNeo4jCurrent/cql-v1-web4-fixture.json +cql-v1-web4.sqlite/WAL/SHM andowned
Mongo cosmic_cql_v1_web4_storage/usage namespaces; noreset/replay. Exactreviewed
reconciliationtestadded; allownruntime stoppedbyrunner, resourcespreservedfailure.
SQLiteordinaryrelationshipmanifest persistence/ackgap is beingowned bypeer;
noSQLproductioneditsfromthissession. Rootfixturerequireslogicalchildren, notforged
storedobservations. Newjoincontract documentsopenmilestone, nojoincapabilityclaim.

25 October reconciliation49390 TERMINAL0:1nativePASS2seconds. Exactoriginal
marker/failureTRX hashes verified; native reads confirmed9Mongo-owned holons,
3SQLite-owned roots,0Neo holons, usageDBabsent and allotherMongo collections empty.
No query/save replay. Beforecleanup retained originalintent6114bytes and12record
snapshot-hashproof2139bytes at C:/Source/OASIS/TestResults/Neo4jCurrent/
cql-v1-role-seed-original.json andcql-v1-role-seed-reconciliation.json.
ReconcileTRXSHA2569698C2C38DE3779EAC1640DE6EBA5E63057D5CD1AF7D83F8EF4CFCBF586BF8A4.
RemovedonlyownedMongo cosmic_cql_v1_web4_storage and, afterprocess exit, exact
C:/Source/OASIS/TestResults/Neo4jCurrent/cql-v1-web4.sqlite (704512bytes).
WAL/SHMalreadyabsent; fixturemarkerretiredbyrunner. Cleanupreceipt retained at
C:/Source/OASIS/TestResults/Neo4jCurrent/cql-v1-web4-cleanup.json. Allownedports
27029/17687/17788/17789absent; systemMongo27017not touched. Lifecycle nowremoved
forfailedfixture, compactevidencepreserved; recreateonlywithcorrectedfreshdemo.
No install/download/newenvironment. Initial9177inspection failedXMLBOMparsing
beforeDBreads; byte-streamXMLreaderfixedthenreconciliationpassed. Currentfresh
demo87SDK/87HTTPstillpendingSQLiteordinaryrolepersistence/rootfixturefix.
SQLitecount/snapshot44e5ff5c9handoffcomplete; thissessionownsSQLroles+Mongoordinary
selection guard audit. No production fixclaim before red/green native tests.
