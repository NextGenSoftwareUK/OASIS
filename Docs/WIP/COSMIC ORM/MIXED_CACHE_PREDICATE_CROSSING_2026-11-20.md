# Mixed cached/native predicate crossing regression

2026-11-20 CONFIRMED MIXED CACHE PREDICATE-CROSSING BUG / GUID PACKAGE ACCEPTED: new3store real HTTP regression44657797476/job136028234944 exact2e3dd5a3 failed3/3,0skip. Strict2266byte14594148377/SHA256fd6f893cb57eb0376be7df219e1d1406e3f36f745590a88a5d537479a6ef1b84 contains only failed TRX: every store returns cached1 where WHEREtext=zz shouldreturn0 because live native text is emoji. API filters stored/native independently; CoreCaptureAll overwrites only IDs actually returned by native predicate. This is a confirmed existing logical-view gap, not missing GUID syntax. Priority root fix must establish live authority/absence across predicates while preserving pure stored holons, owner scope, completeness limits, provenance and performance; no unbounded source scan, silent fallback or record rewrite. Retain accepted9contract/grouped+3row cases; retry only affected/new regression cases. Image44653298584 succeeded exact8017ee4c digestsha256:948d104088b7bd3261bd7235a5cb0c17e51be8396bc75d1c7632e9a2d574aeb6; package44658363693/782byte14593634007 exact2JSON confirms same source/digest/teachingtag/5assemblies/sourcehash/runtimegraph/ownedContainerAbsent/dataWrites0, apiQualified=false/liveDeployment=false. Package success is not full query acceptance. No new Development push yet, currentDevb8 remains live. GUID predicate support, cache-boundary fix, hosted/free-model and full61/168 EF plus4/13 extension programme remain open. No sharedAtlas/PIPER/DNA/worker/staging/prod/local generated runtime/env/build/DB/image/archive/results changes. Artifact/log verification RAM only; temp source indexes dispose.


Evidence: https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44657797476

Owned fixture: one native emoji row has a materialized snapshot whose textAlias is deliberately stale zz; a pure important xy holon and pure emoji holon remain independent. The first existing String equality HTTP query must return no zz rows, but each real MongoDB/SQLite/Neo4j provider returns one. Assertion is MappedHttp.cs190; this is not the earlier faulty row-response mapping-ID assertion. Cleanup executes in fixture finally paths; no PIPER/Atlas data is used.

Relevant source: CqlProjectionV1Endpoint.ExecuteProjectionAsync executes the materialized filtered projection, then CompleteVirtualProjectionAsync runs the native mapping with the same filters. Core Queries/CosmicMixedProjectionResults.CaptureAll inserts all filtered materialized rows and replaces only IDs in the filtered native result. A native row excluded by its current value cannot displace its stale cached hit. This affects the shared capture owner used for rows, counts and grouped results; accepted intersection-only reconciliation does not prove predicate-boundary correctness.

Root-fix constraints:
- Treat a materialized cache of the mapped native identity according to the declared source authority before logical filtering, ordering, paging or aggregation.
- Preserve independent fully stored holons and all actor/app permission proofs.
- Distinguish a proven native nonmatch/deletion from an unavailable/incomplete native read; do not silently serve a stale or partial answer.
- Preserve bounded payload/read costs, cancellation and usage attempt accounting. Do not scan the entire native source or perform an unbounded per-record lookup.
- Preserve existing cached data; no migration/backfill or user-data write is authorized by this regression.

Snapshot metadata presently stores oasis.virtual.snapshot as a dictionary with mappingId, mappingVersion, provider, fetchedAt and optional applicationId. It does not store native key or source connection identity. Stable native IDs depend on actor/provider/source/native key/connection identity, not mapping ID. Account for existing snapshots and cross-mapping identity before choosing a minimal proof protocol. Do not merely filter result strings or drop all metadata-bearing holons.

Next: implement the authority/identity proof in its owning query layer and qualify these same3 failed cases, preserving previous accepted cohorts. GUID native predicate support is a separate open feature.
