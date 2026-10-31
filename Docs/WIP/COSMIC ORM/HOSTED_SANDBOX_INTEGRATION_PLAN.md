# Hosted sandbox integration — active priority

31 October2026. User clarified that all demonstrated providers must run through
the hosted sandbox, like MongoDB/SQLite/Neo4j. Local launchers are development
evidence, not completion of hosted demo coverage. Pause additional local catalogue
expansion (including Milvus) until hosted integration makes progress.

## Current truth

-3 providers verified through deployed authenticated API: MongoDB, SQLite, Neo4j.
-41 additional providers have scoped native/local or temporary hosted SDK/HTTP
 demo evidence and public catalogue links. They are not41 hosted sandbox providers.
- Public headings39 are neither provider count nor hosted coverage count.
- Compiler read-profile admission additionally names Cosmos DB/IPFS/SOLID/
 ActivityPub, but source/config/policy/deployment/live acceptance is not proven by
 that list. Federation and public selector still admit only the original three.
- Azure account remains pending/free-only; no paid signup or assumed credentials.

## Required work

1. Inspect deployed provider registrations/configuration and available persistent
 hosted runtimes. Do not point public services at developer loopback addresses or
 forward browser JWTs to third-party providers. No unapproved paid resources.
2. Enrol actual configured sources through host-owned provider/read-policy registry.
 Discovery exposes only configured/eligible profiles and supported query presets.
 Fix hard-coded admission at its owning boundary; do not enable every enum blindly.
3. Retain per-avatar visibility, authoritative catalogue ownership, response budgets,
 native error propagation and capability-aware rejection. An arbitrary submitted
 ID is not an authorization grant. No local proxy/fixture rows disguised as hosted.
4. Use the existing CQL tab's query/request/result flow for newly enrolled providers;
 show service location and native/emulator status. Keep optional local teaching
 launchers separate. Unsupported metadata/text/etc queries must reject honestly.
5. For each provider verify deployed health/config identity, authenticated full and
 filtered multi-record query, Mongo federation, provenance, tenant isolation,
 capability rejection, native-source observation and cleanup/retention contract.
 Keep evidence counts/hashes/paths and never mark the provider hosted-green from
 compiler tests, a catalogue card or local-only acceptance.
6. Prioritise the requested demo providers (Oracle, IPFS, SOLID, ActivityPub,
 Solana, AWS/Azure as available) before remaining breadth; preserve existing
 Mongo/SQLite/Neo behaviour. Batch verified deployment once/twice daily.

HyperDrive failure matrix/full ORM parity remain open. Hosted read/query evidence
does not prove replication/CAS/transactions or100% uptime. Deep LINQ remains paused.

Exact next step: inspect deployed eligible sources and existing server-side CQL
read-access policy/registration, then implement and verify the first additional
provider in the hosted request/result flow. No further local-only card milestone
is a substitute for this requirement.

## First registration fix verified —31 October2026

ActivityPub was absent from the BootLoader registration switch. It now uses the same provider configuration factory as tests, with a valid instance-origin DNA default, explicit EnableCqlStatusKeySets and optional AccessTokenEnvironmentVariable. Overrides cannot rebind credentials to another origin; missing configured tokens reject, never downgrade to anonymous. Construction does no I/O and does not change the node default. DNA/provider/BootLoader builds and10 host configuration tests pass. TRX TestResults/HostedCqlIntegrationCurrent/activitypub-host-configuration.trx SHA256914658BF4790127FDE4F5E2705DA3053236CC05039D18CA79F64C7D496CCD431. Not deployed; no fourth hosted provider claimed.

Next root cause: current ordinary-provider admission requires write tariffs for every ordinary provider whenever write routes exist. Query-only social sources need explicit read-only enrollment and an enforced write prohibition, not fabricated write grants. Then enrol actual hosted source/discovery/presets and verify authenticated live queries/Mongo pairing. Existing local launch links remain.

## Query-only admission verified —31 October2026

Server configuration now supports explicit QueryOnlyProviders separate from ordinary writable storage. Both preflight and direct physical dispatch prohibit non-CQL calls before provider/outbox I/O, including SDK policies with optional storage pricing disabled. Explicit query tariff required; duplicate/overlapping/native/ambiguous grants rejected and caller array mutation cannot change captured permission. Existing ordinary storage unchanged.12 new controls pass and52 existing configured-admission regressions pass (64total/no skips). TestResults/HostedCqlIntegrationCurrent/query-only-configured-regression.trx SHA25637BB48BF44B86578BED456EDD9F50817E458130964867FBEE9F981B9687D9C76. Initial12-case run had1 invalid test fixture (leaf reserved4 attempts instead of1); corrected fixture, original failed TRX preserved. No production contract weakened. Next is actual hosted source activation/discovery, then presets/federation and deployed/browser verification. Still only3 hosted providers verified. Guide Docs/Devs/CQL_QUERY_ONLY_ADMISSION.md.

## Hosted registration/discovery and read-only activation verified

Background enrollment now uses only the admission factory's captured query-only providers, leaves default/routing lists unchanged, and requires explicit ICosmicReadOnlyQueryActivation (no generic provisioning activation fallback). ActivityPub verifies one cancellable/1MiB-bounded native instance GET. Hosted deadline30s awaits the owning request. Authenticated GET /api/data/query-cql/providers describes configured tariff-backed sources; query-only readiness requires completed verification, not just an active flag. Native failures are explicit/redacted and never switch sources. Missing factory exposes no fabricated sources. Native feature descriptions are not full API/query parity guarantees; protocol federation remains unavailable until integrated.

100 affected controls pass/no skips:64 Core admission/snapshot,21 API discovery/policy/sandbox and15 ActivityPub activation/configuration. Paths TestResults/HostedCqlIntegrationCurrent: hosted-source-core-regression.trx SHA25615C858164C7158B10FD6A529F69C138D3B5D7CA5DE67A6360EA1746422B42BF6; hosted-source-discovery-verified.trx SHA256621F150A12876B1447A1C30C732C4EEAE89F4D285B81D9DFE9381CE11F127E28; activitypub-read-only-host-activation.trx SHA256694715C916E354B886C3A9237BCA12FEF5E29E1422D8DD4D0E6BC38ADCDC445B. Injected/in-process controls, not deployed native acceptance. API producer build passes. Earlier missing ILogger import build failure preserved/corrected. Hosted verified count3 unchanged.

Next concrete gap: HTTP filter operators lack In membership, while native protocol multi-record profiles require one membership AST; OR of point predicates is not equivalent admission. Implement bounded typed In without alternate native reads, then public source presets/browser discovery and live deployment. Keep original local launcher links. Hosted Azure remains pending/free-only.
