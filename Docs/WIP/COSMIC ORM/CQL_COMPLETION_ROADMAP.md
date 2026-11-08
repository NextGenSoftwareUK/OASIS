# CQL completion roadmap

## User priority order updated 8 November 2026

Solana → Ethereum → Polygon → Arbitrum → NEAR → EOS/EOSIO → SOLID → IPFS →
ActivityPub → Cosmos → Polkadot → TON → TRON → Hedera/Hashgraph, then remaining
providers in popularity order. This matches the latest user-edited goal. OLID
means SOLID; ActibityPub means ActivityPub; HetraGraph means Hedera/Hashgraph
(HashgraphOASIS).

Solana, Ethereum, Polygon, Arbitrum, NEAR, Cosmos and Polkadot retain accepted
scoped live HTTP and browser evidence. IPFS and ActivityPub also retain live HTTP
and browser proofs. SOLID, TON, Hedera and EOS retain accepted live HTTP evidence;
their browser gates remain open. TRON has no accepted current live CQL admission: preserve rate-limit/denial
outcomes and the authenticated-reader draft/credential preflight; do not retry
failed unauthenticated endpoints or recreate its original cohort. Use the single
owner-action list for any missing account/access requirement, preserving existing
entries and deferrals. Do not rerun accepted native sources or resave copies just
to change queue order.

Focus engineering/demo usability on this chain order before adding further
providers. Existing MapLibre and four-provider plans/owners/settings remain
prepared and unchanged behind the chain priorities; no unverified rollout or
third8November Development push. GOMap WEB4 run42480693015 has completed success;
close out its already-running artifact/cleanup evidence without starting another
GOMap feature ahead of this queue. Remaining-provider popularity ranking must use
current adoption/use evidence when that queue is resumed, with dependencies and
owner deferrals explicit.


Updated 7 November 2026. Full programme remains incomplete.
[CURRENT_STATUS](CURRENT_STATUS.md) owns deployment state and the exact next step.

7 November checkpoint: [Dapr](DAPR_CQL_MVP.md) now passes scoped live native3/2,
Mongo5/4, snapshot reuse/privacy and SQLite/Neo4j2/2, with unchanged mounted
values/ETags. Coverage119local/92liveHTTP/28browser; Dapr browser gate remains open.
The original unadmitted failed generation is preserved separately from its fresh
acknowledged source. This checkpoint does not close the full provider/ORM programme.

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

Latest breadth milestone: six additional hosted readers (Miden, Espresso,
EigenLayer, Fhenix, Axelar and Ritual) and cards71-76 are accepted/published.
Coverage119local/91live HTTP/28functional browser. [Exact evidence and limits](SIX_PUBLIC_READERS_HOSTED_BATCH.md).
Dapr's next hosting gate requires Linux native sidecar and restricted backing
store proof; preserve its Windows-only qualification as a separate scope.

1. Continue the next eligible provider scope from
   [CURRENT_STATUS](CURRENT_STATUS.md) and the provider inventory. NATS scoped
   TLS/native/restart, hosted HTTP native3/2/Mongo5/4/privacy/replicas and public
   card64 are complete; [NATS evidence](NATS_JETSTREAM_CQL_MVP.md) preserves
   the original source and sealed cohort. Browser verification stays open.
   Preserve accepted LocalFile, Graph Studio, PouchDB, Waku and DuckDB sources
   and write schedules. GUN/OrbitDB teaching runtimes remain enabled.
   Nostr recovery, QuestDB public403, Chroma enrollment and remaining launcher
   runtime gaps stay open. Preserve all deferred owner decisions.
2. Preserve the accepted hosted and local scopes in the [provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md).
   ClickHouse has accepted hosted evidence; its sandbox card, guide/C#
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
4. Complete 46 remaining browser gates among the 74 HTTP-qualified profiles.
   Twenty-eight flows have browser evidence,
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
