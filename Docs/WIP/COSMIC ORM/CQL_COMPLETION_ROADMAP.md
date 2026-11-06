# CQL completion roadmap

Updated 5 November 2026. Full programme remains incomplete.
[CURRENT_STATUS](CURRENT_STATUS.md) owns deployment state and the exact next step.

Continue the full CQL, COSMIC ORM and HyperDrive roadmap. Prioritize the live
Development WEB4 demo. When a gate needs owner access or retained evidence,
keep that gate open and advance the next independent provider or engineering
scope. Verify the remaining browser, native, ORM and HyperDrive gates separately.
The post-demo [virtual/hybrid import design](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md)
tracks the proposal, one collection mapping, federation, explicit snapshot saves
and opt-in caching; [architecture](CQL_ARCHITECTURE.md) defines its access boundary.
After the live WEB4 demo and the CQL, COSMIC ORM and HyperDrive MVP,
complete every provider in the [provider inventory](PROVIDER_CQL_AND_SANDBOX_STATUS.md).
Each provider must implement its applicable shipping contracts through real
SDKs, clients or APIs. Close placeholders, TODOs and unimplemented shipping
paths, and verify behavior against native services with evidence for every
applicable capability. Keep scoped MVP acceptance separate from full provider
completion. Preserve existing owner deferrals as explicit open gates.
Extend the [EF Core, EF6 and NHibernate parity checklist](CQL_PARITY_CHECKLIST.md)
from SQLite, MongoDB and Neo4j to every provider after the MVP. Verify each
applicable feature's observable behavior on its native provider. Record the
reason and evidence for any inapplicable feature and keep unverified cells open.


The 4 November published launcher audit found all 37 referenced scripts in the
task checkout. Runtime qualification remains separate.

1. Complete the [LocalFile Development rollout](LOCALFILE_CQL_MVP.md) using the
   qualified image and verified persistent mount, then run the hosted demo gates.
   Nostr source recovery is already deployed. The [operator recovery gate](CQL_SOURCE_RECOVERY.md)
   remains open for the unavailable source.
   Recover the published launcher runtime/configuration gaps and their reviewed
   source dependencies from retained sandbox-integration pin 237403a75. DuckDB
   now has a passing retained native and Mongo-paired replay on the recovery
   laptop. Its Development file placement and activation remain open.
   ThreeFold's hosted audit and card/static assets now pass at website2170b67;
   preserve its original cohort/settings/ACKs. Do not replay accepted gates.
2. Preserve the accepted hosted and local scopes in the [provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md).
   ClickHouse is the latest accepted hosted addition; its sandbox card, guide/C#
   links and exact public static bytes are verified. Counts are demo scopes,
   not full provider or ORM certification. Meilisearch adds the new Windows-local
   profile: native/restart, manager/Mongo/HTTP and browser verified, with an honest
   local-only WEB4 card. Hosted Meili enrollment remains open. Solr now has
   native3/2/0, retained restart, manager/HTTP and local browser5/4 proof, adding
   one local profile. Its local WEB4 card72e29ba and pinned guide/C#links are
   published and browser-verified; hosted Solr remains open. ArcadeDB adds native/
   restart, manager/Mongo/HTTP and local browser5/4 proof; its local WEB4 card
   and pinned source links8123df9 are published/browser-verified. Hosted enrollment
   remains open. OpenSearch adds native/restart, manager/Mongo/HTTP and local
   browser5/4 proof; its local WEB4 card/source links5073f06 are published and
   browser-verified. Hosted/full-provider gates stay open.
3. Keep filtered native queries and Mongo federated presets for every admitted
   provider. Expand remaining red scopes with explicit capabilities/consistency,
   controlled tests, actual native manager/LINQ and HTTP proof, privacy/replicas,
   honest labels and usable sandbox/local demo links.
4. Complete 32 remaining browser gates. Twenty-eight flows have browser evidence,
   including the twenty-three recovery-session additions documented in
   [browser evidence](RECOVERY_BROWSER_EVIDENCE_2026-11-05.md).
   Static assets and HTTP success do not prove browser interaction.
5. Preserve the original MongoDB/SQLite/Neo4j investor subset and 50/82 green
   parity rows each. Work only red cells or reproduced bugs; do not re-harden
   completed providers or federation cursors.
6. Resume paused deep selected-stage LINQ, unrestricted POCO mapping and
   migrations tooling after demo priorities; these remain part of the full goal.
7. Complete full ORM/HyperDrive V2 acceptance for ownership/conditional writes,
   children/depth, tracking/deltas/history, recovery, migration and placement.
   Read-only projections do not establish owned writes or distributed snapshots.

Hosted Azure Cosmos, Oracle and PostgreSQL remain deferred. Legacy gaps are
reported only. Preserve accepted owners/catalogues/intents/ACKs; no cohort replay.

[Parity checklist](CQL_PARITY_CHECKLIST.md) ·
[Provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) ·
[Backlog](AFTER_DEMO_BACKLOG.md) ·
[Earlier roadmap](Archive/CQL_COMPLETION_ROADMAP_before_hosted_reconciliation_2026-11-02.md).
