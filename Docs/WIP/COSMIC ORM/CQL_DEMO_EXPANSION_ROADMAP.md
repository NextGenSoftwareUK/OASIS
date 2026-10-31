# Hosted CQL demo expansion

Updated 31 October 2026. User priority: broader hosted demo before full EF/NHibernate parity.
The original full roadmap remains active; defer depth the demo does not need.
Current release state/counts: [CURRENT_STATUS](CURRENT_STATUS.md). Detailed dated checkpoints below are historical.

## Priority and recommendations

| Order | Provider | Evidence and next work |
|---|---|---|
| Baseline | MongoDB, SQLite, Neo4j | Existing 12-row demo set is green. Use the proven common query subset; unfinished Q01f parity must not delay expansion. |
| 1: cloud | **Azure Cosmos DB for NoSQL** | Hosted Azure explicitly deferred by David: free only, no existing subscription. Existing local/emulator/native CQL proofs remain separate; do not re-ask signup/payment. [Earlier native evidence](CQL_COSMOS_NATIVE_ADMISSION.md). |
| Cloud breadth | **Public AWS S3 metadata** | Hosted native3/2, Mongo5/4, repeat0writes, isolation and exact SQLite/Neo4j copies verified through HTTP. Browser gate open. This is anonymous object HEAD metadata, not owned encrypted S3/DynamoDB acceptance. |
| Next free cloud candidate | **Google Cloud Storage public metadata** | Anonymous source now implements common CQL interfaces;17controls +1native3/2case pass. Cached Google.Cloud.Storage.V1 4.11.0 reused, no body downloads/account. Not hosted sandbox acceptance. WEB4 read registration/config/admission/discovery now implemented (11JSON/22API-schema/77Core controls pass). Native HTTP Google3/2+Mongo5/4and tenant isolation now pass.48client preparation controls and3configuration tests pass; fourpresets staged, no live settings changed. Next scheduled application/config rollout and hosted/browser gates; keep separate from frozen release batch. |
| Deferred hosting | **Oracle Database** | Read-only activation, provider-scoped catalogue/discovery and local native HTTP3/2+Mongo5/4 verified. SDK seeds2private copies; production billing/login/JS REST setup/replica/browser proof remain separate. David has no Oracle account and explicitly deferred hosting. |
| 2: chain | **Solana public account metadata** | Hosted native3/2, Mongo5/4, repeat0writes, isolation and exact SQLite/Neo4j copies verified through HTTP; verbatim C# LINQ sample passes mainnet3/2. Browser pending. Read-only configured accounts; original memo/history recovery and full ORM remain separate. |
| 3 | **IPFS** | Hosted HTTP/browser native3/2, Mongo5/4, private preparation/reuse/isolation and exact SQLite/Neo4j copies verified. Bounded configured CIDs on isolated Kubo; no general search or public-network resilience claim. [Earlier evidence](CQL_IPFS_DEMO_ADMISSION.md). |
| 4 | **SOLID** | Hosted HTTP native3/2, Mongo5/4, private preparation/reuse/isolation and exact SQLite/Neo4j copies verified. Browser gate open. Bounded configured encrypted resources, not general Pod search. |
| 5 | **ActivityPub** | Hosted HTTP/browser native3/2, Mongo5/4, private preparation/reuse/isolation and exact SQLite/Neo4j copies verified. Read-only configured statuses; no posting/authenticated mutation/general account search acceptance inferred. [Earlier profile](CQL_ACTIVITYPUB_NATIVE_PROFILE.md). |

Repository evidence: [provider matrix](../HyperDrive-Providers/PROVIDER_MATRIX.md),
[Solana submission work](../HyperDrive-Providers/SOLANA_SUBMISSION_CONCURRENCY.md),
[SOLID audit](../HyperDrive-Providers/SOLID_PROVIDER_IMPLEMENTATION_AUDIT.md).
Storage conformance is not CQL conformance; emulator/local proof is not hosted-cloud proof.
Refresh published provider evidence before implementation because that workstream can advance separately.

Official references: [Cosmos query overview](https://learn.microsoft.com/en-us/cosmos-db/query/overview),
[Solana account-query RPC](https://solana.com/docs/rpc/http/getprogramaccounts).
Cosmos supports SQL-style selection/filtering/ordering; Solana RPC account filters are narrower.
Neither reference proves the current OASIS adapter already supports CQL.

## Next three EVM demo scopes

| Scope | Native CQL | C# / CLI | HTTP + Mongo / isolation | Hosted / browser |
|---|---|---|---|---|
| Base public balances | ✅3/2 | ✅3/2 | ✅3/2 +5/4,isolation | ❌ pending |
| Avalanche public balances | ✅3/2 | ✅3/2 | ✅3/2 +5/4,isolation | ❌ pending |
| BNB Chain public balances | ✅3/2 | ✅3/2 | ✅3/2 +5/4,isolation | ❌ pending |
| Optimism public balances | ✅3/2 | ✅3/2 | ✅3/2 +5/4,isolation | ❌ pending |
| Linea public balances | ✅3/2 | ✅3/2 | ❌ next | ❌ pending |
| Scroll public balances | ✅3/2 | ✅3/2 | ❌ next | ❌ pending |
| Rootstock public balances | ❌ finalizedheaderHTTP400 | ❌ | ❌ | ❌ |

[Proof, exact limits and next steps](EVM_NEXT_THREE_SOURCE_STATUS.md). These reuse the unchanged EVM read engine and existing providers; source projection proof is not full ORM or signed storage parity. Work only on the red cells.

## Bounded common demo acceptance

1. One readable C# sample and one API demonstration, using synthetic holons and explicit provider scope.
2. Save/load, edit/save an acknowledged revision, inspect its delta, and query the selected backend.
3. Start with proven common shapes: typed/metadata equality and range filters, ordering, bounded paging, simple projection and Count/Any where admitted. Freeze the exact cases first; arbitrary LINQ is not required everywhere.
4. Show provider, execution mode, authoritative record/revision, consistency/finality and any index watermark/lag.
5. Prove the selected backend supplied the data. Do not silently present a Mongo copy as native chain querying.
6. Reject unsupported operators during planning. No hidden full-store download, managed filtering, source substitution or implicit retries.
7. Preserve actor visibility and existing encryption/reserved-metadata policy. Verify the child/depth and conditional-write semantics actually demonstrated; unsupported conditional writes remain explicit.
8. Test success and failure, record hashes/counts in reports, keep raw evidence in TestResults, and clean only exact owned fixtures.
9. A cloud claim needs a hosted endpoint. Label chain local-validator/devnet/mainnet accurately; real-money transactions and new paid infrastructure are not implementation prerequisites.

## Different backends, honest execution

Use native supported query shapes first. A persistent query index may be a deliberately designed,
independently admitted capability for a chain or protocol, with source references and finality/lag
semantics. It must not be an automatic fallback when native translation fails. Design and disclose
that execution mode before enrolling richer shapes.

IPFS deletion must describe the actual catalog/tombstone/unpin behavior, not global erasure of copies.
SOLID preserves Pod ownership/access; ActivityPub preserves actor/activity semantics.
Distinguish replicas from distinct holons and do not promise one snapshot across providers.
HyperDrive automatic failover/replication needs its own verified demo gate; individual CQL support
does not establish global failover or 100% uptime.

## Next actions

- SQLite projection checkpoint is committed; unfinished parity-only depth remains deferred.
- Cosmos canonical and string/Boolean metadata rows are enrolled/native-proven; native Count/LongCount/Any and projection also pass; conditional Save/durable deltas are explicitly unsupported with pre-activation rejection tests; next implement numeric semantics and remaining lifecycle/HTTP/hosted acceptance; shared manager/LINQ row demo and canonical Version/Id index profile now pass.
- Solana declared-range source/index SDK, LINQ, rows/projection/cardinality HTTP and interactive launcher now pass. Original-history recovery remains red; no replacement seed is claimed as recovery. Next: common demo metadata/lifecycle capabilities, ongoing index ingestion, hosted-cloud and production auth/durable settlement; preserve all narrower point-profile limits.
- Keep the full parity checklist strict and update CURRENT_STATUS each milestone. Unfinished Q01f depth remains deferred, not complete.

Environment update: user explicitly authorized creating missing environments.
Fresh persistent IPFS and isolated SOLID demo identities now pass native readiness
using cached runtimes; [commands/evidence](CQL_DEMO_ENVIRONMENTS.md). Active next
IPFS V2 storage baseline/CQL admission, then SOLID; old-data recovery stays open.

Unified demo launcher now selects SQLite coffee or IPFS/SOLID/ActivityPub point hosts. ActivityPub nativeinteractivegate1pass against existingpublicstatus, no posting/authentication. See [commands/evidence/limits](CQL_INTERACTIVE_POINT_DEMO.md). Fourproviders inlauncher doesnotcertify full8provider/commonquerydemo orfullparity.

Live Solana declared-range SDK/index/manager/LINQ and shared C# walkthrough now
pass against the retained validator with new coverage3073; original recovery stays
red. [Native command and limits](CQL_SOLANA_DEMO_ADMISSION.md#live-declared-range-sdk-demo-new-range-not-original-recovery).
Live-source shipping HTTP now passes27requests against the same seed/index and
validator. Interactive hub now adds Solana on5089 with native leave-running/shutdown acceptance. Next: ongoing index ingestion, production authentication/durable billing and common-demo capability gaps.

SolanaMetadata now explicitly selects the separate schema3 acknowledged fixture/index: live SDK/HTTP42requests and read-only/interactive reuse pass, including nested string/exactInt64 filters and pre-reservation private-root rejection. It shares the retained validator and preserves schema2/original history. Next selected metadata and remaining red demo capabilities; production identity/durable settlement remain open. See CQL_INDEXED_METADATA.md.

Selected schema3 metadata is now native SDK/HTTP/demo proven (50requests plus
interactive selection), with no new seed/source writes. Next demo integration:
reuse Run-CosmicCqlV1Demo.ps1 for the existing three-database group, then the retained
Cosmos DB CQL profile/runtime for a cloud teaching host. Current hub lacks these
entries; do not label the Cosmos blockchain runner as the Azure cloud runner.
This integration takes priority over additional parity-only Solana operators.

Databases hub native launch is restored after exact archived fixture reconciliation:681SDK queries/138walkthrough HTTP requests/30save checks pass, with cleanup. CosmosDB teaching-host/hub integration also passes native and interactive HTTP lifecycle; browser rendering and remaining red demo/parity gates follow. See CQL_DATABASE_DEMO_HUB.md and CURRENT_STATUS.md.

## Google public-source candidate evidence (31 October)

The existing Google canonical provider uses Firestore. Its preserved broader-branch native CQL/local Mongo demo is a different surface; do not duplicate or replace it. A separate anonymous Cloud Storage object-metadata profile can reuse the existing pinned SDK without a Google account/emulator or new installation, following the explicit read-source profile pattern already used by S3/Solana. No general bucket scan, source body download, credential path or owned-cloud write claim.

Verified3objects from official gcp-public-data-landsat scene LC08_L1GT_001003_20140812_20170420_01_T2, via one bounded prefix listing and3exact metadata requests. Candidate raw path TestResults/GcsPublicSourceCandidateCurrent/candidate-metadata.json1952B, SHA256b056c141917c9b2338485d6d29b4edf591f7a2bbd6c503e1767a67b8a328c909. No cloud/session/config mutations or durable source owner created. [Official anonymous public-data access](https://docs.cloud.google.com/storage/docs/access-public-data), [public datasets](https://docs.cloud.google.com/storage/docs/public-datasets). Actual pinned4.11.0 XML docs locally confirm StorageClient.CreateUnauthenticated/GetObjectAsync/Dispose; implementation must compile and test against that version, not latest5.x documentation.

Next independent implementation should use a separate task branch before touching provider/Core source, retaining the verified Core6bbd015/current root release candidate. No new worktree/environment is needed. Hosted admission, query/visibility/discovery and actual native3/2+Mongo5/4 acceptance remain unimplemented for Google; do not increase the hosted-provider count from8.

### Google adapter milestone

Branch work/cosmic-cql-google-public, same assigned worktree; Core remains6bbd015 and frozen release branch79fe887e3 unchanged. GoogleCloudPublicMetadataSource public constructor is anonymous-only; borrowed SDK test injection is internal. Existing canonical Firestore provider/factory and boot DNA were not changed. Query contract: configured GCSObjectKey equality/In, max8keys/128characters, complete validation before page publication, visibility and deterministic NativeRecordHolonMapper identities; unsupported content queries fail planning. Native UInt64size and Int64generation values remain exact decimal strings; updated timestamp remains raw text. Metadata-only GetObjectAsync uses NoAcl projection, RetryOptions.Never and no UserProject.

17controlled cases pass/no skips: point/membership/page/missing/error/zero-page-error/budget/cancellation/unknown/inactive/malformed metadata, invalid catalogue and disposal/planning checks.1actual anonymous SDK/CQL case passes/no skips against the retained3official Landsat objects and2record subset. Source config LocalState/COSMIC/HostedGcsPublicSourceCurrent/source.json470B retains stable synthetic owner/keys; no cloud credentials or account. Public provider README now has spaced/commented C# usage. These tests do not claim WEB4 auth/billing/federation/replica/browser acceptance.

Evidence TestResults/GcsPublicSourceCandidateCurrent/provider-controls.trx SHA256CCD4567C1F937623F19DD788ADC6EFDF3A6653933B1A528B11A69F3CB21481F8; provider-native.trx SHA256706C3ACB3392A330E4ECA7A9F119F555A649758018A02DA465A028BEF74A8BAB. Cache-only restore rebuilt missing test assets from verified existing pins; no remote feed/new dependencies. Initial missing-assets and explicit-namespace compile failures retained separately, corrected build passes. Hosted provider count remains8.

### Google WEB4 profile milestone (task-only)

Explicit CQL_GCS_PUBLIC_SOURCE_JSON now registers GoogleCloudPublicMetadataSource through the existing isolated CqlReadSourceProfile registry; canonical Firestore DNA, credentials/default-provider routing and ordinary write tariffs are unchanged. Exact schema is {schema:1,bucket,keys,owner}, max8KiB UTF8/depth4, no unknown/decoded-duplicate fields, key/owner/type validation before anonymous client creation. Credentials, billing-project and endpoint fields are rejected.

CQL leaf/federation compiler accepts the explicit Google read profile and unordered bounded key selections. Independent host query permission and its query tariff remain required; a query tariff cannot admit a Google Save. The11-provider policy test reproduced the old fixed10tariff ceiling. Core now derives capture/overflow bounds from its single explicit supported unsigned-read set, preserving duplicate/unsupported/unpermitted/price checks. This avoids another provider-count mismatch as breadth grows.

Verification: shipping API/test builds pass;11JSON cases,22API/profile/discovery/schema cases and77affected Core admission cases pass/no skips. Discovery tests plan public3/2and Mongo pair presets, keep leaf examples available when Mongo is absent and disable those pairs explicitly. These are controlled profile/admission tests, not hosted authentication or actual Mongo data/federation proof. Previous17source controls/1real anonymous native3/2remain preserved and were not repeated.

Initial failures retained: Startup edit encoding corrected from original UTF8/BOM with only the intended registration hunk; first API run21pass/1fixed-cardinality failure; first Core run75pass/1staleSolana assertion. The latter test wrongly treated all Solana reads as signed storage despite the existing unsigned public profile. Separate root test-only commit73b8da142 preserves unsupported Ethereum refusal and explicitly verifies Solana read pricing does not permit signed saves. No production signature permission relaxed.

Core work/cosmic-cql-google-public44649b03974bed9bccb0e334e91ee0fb79abfb64 is pushed/ls-remote verified; current Google parent gitlink/manifest advance together. Frozen release Core6bb/root79fe887e3 unchanged. Evidence TestResults/GcsPublicSourceCandidateCurrent/profile-json-controls.trx SHA256738A6AC5969C1A3DDF2C1EE8DE8A02160A70CE9E6B67120DF8B600F82B82F78E; web4-profile-controls-corrected.trx SHA2567D49C40FE89253F750823DCCE4E4E4B7978A2FC4D92286803F7382D18DF099DA; core-admission-controls-corrected.trx SHA2568E55A72FE0513FEDCD1600980E5119C11F46A5510B2F27185DF1E581A37BEC3B. Raw failure reports/build logs retained beside them. No live variable/deployment/session/data mutation; hosted count remains8.

### Google + Mongo HTTP milestone (31 October, task-only)

`CqlGcsMongoNativeTests` now exercises the shipping leaf/federation endpoints over loopback Kestrel using the anonymous SDK Google source and retained Mongo8.3.11 replica set. Source metadata membership returns3/2; two SDK-owned private create-only snapshots produce5/4preserved copies. A second controlled actor sees0private Mongo rows and3public Google rows in federation. Every returned ID, selected provider, public flag and private owner is checked;9physical query receipts validated. Public source config unchanged; no cloud writes/body downloads.

Full test project build passes. Explicit native gate1passed/0failed/0skipped: `TestResults/GcsPublicSourceCandidateCurrent/google-mongo-http.trx`, SHA2567B1E50E6202709459F8BD5B3A0AFBC1F8F78E4CE0382ACEF0FDF2E596EA26180. Compact counts live beside it in google-mongo-native-summary.json. Run after a leased bounded build with `Scripts/Sandbox/Test-CqlReplicaBaselineRepair.ps1 -GcsMongoHttp`; the runner owns its lease and Mongo process, so do not nest another lease.

Durable intent records original operation IDs before writes; each save acknowledgement retained under `LocalState/COSMIC/GcsMongoHttpCurrent`. Success-only cleanup checks the nonce/owner and exactly2Holons, removes only cosmic_cql_gcs_http_demo and verifies database absence. Runner stopped Mongo; port27029has no listener. This retained cohort cannot be replayed automatically; the intent guard requires explicit reviewed fixture lifecycle before any future run.

Scope limits: controlled bearer and usage-authority mocks, not production login/billing; SDK snapshot setup, not browser/JS REST preparation; no HyperDrive replication acceptance. Hosted Google configuration, client preparation/presets, browser acceptance and deployment remain next. Hosted count stays8. Earlier profile/native-only milestone descriptions above are historical.

### Google demo composition and staged configuration

Web4Sitea5f1e78 adds a Google-specific preparation case; all48client controls pass. Existing discovery/preset/preparation code already supports the explicit provider+metadata-path recipe, so no parallel production branch was added. Four server-owned presets cover public3/2and Mongo-paired5/4; client preparation retains exactly the first two original keys, server-derived avatar IDs and create-only private saves.

Publish-HostedGooglePublicConfiguration.py reuses the journalled publisher and stages exactly3values: source profile, query-only admission/free tariff, and4presets. Two offline tests (including11rejection subcases) pass; canonical FirestoreDNA and unrelated routing/admission remain unchanged. Credentials/billing/catalogue drift/paid tariffs/existing enrollment are refused. A protected current-before/candidate snapshot was prepared successfully; no live variable mutation/deployment. Application rollout and hosted/browser/replica proof remain open. The [Google guide](../../Devs/GOOGLE_PUBLIC_METADATA_CQL_GUIDE.md) provides spaced C#/HTTP usage, configuration, runner commands and exact scope limits.

Google encrypted-snapshot prerequisite (31October): GCSObjectKey remains app-specific metadata. The publisher refuses DNA-encrypted enrollment unless AdditionalQueryableKeys explicitly grants it; it never rewrites canonical DNA or globally reserves public-source fields. Both encryption-switch cases tested. Current protected snapshot has encryption off, and candidate equality was rechecked without replacing state or touching hosting.3configuration tests pass; raw log `TestResults/GcsPublicSourceCandidateCurrent/configuration-controls.log` SHA25603E73C89988A862AD73D785B5D86827014F16867210004E62ED16B776B6F65C8. Existing hosted runner now accepts GoogleCloudOASIS --prepare; run only after matching deployment/config. Browser getTab still Transport closed; no Windows-input restart.

### Runnable Google C# demo

Examples/CosmicCqlDemo now accepts --google-public <existing-source.json> before SQLite fixture setup. GooglePublicWalkthrough activates the anonymous source, supplies its explicit adapter to an unbound HolonManager and runs the shared NativeKeySetWalkthrough via CosmicLinq for3keys then2. Canonical registry remains unused; no Google/Mongo writes or service setup. NativeKeySetWalkthrough also admits the explicit Google profile for its existing Mongo-pair API; the new console mode demonstrates only native leaf reads, not that C# pair method.

Actual command exits0and prints3full/2filtered records.1native GooglePublicCSharpDemoNative case passes/no skips, compiling the actual published walkthrough files. Full demo/test builds pass; example assets restored from existing cache only. TRX TestResults/GcsPublicSourceCandidateCurrent/console-sample-native.trx SHA2560EFDEEB893EDBEBD30A14EB21170D2E60630304E9B367D524034754D0C415E12; console-native.log SHA25670B992415A13BEA71B5A8C3604750FE6064BC1B578EDFD1CA34F1588F57BDD11. README/guide have spaced runnable commands and scope limits. Existing HTTP pairing proof remains separate; hosted Google still pending.

### Next chain breadth: Ethereum public metadata candidate

Readiness only, not a new CQL/hosted provider: `Scripts/Sandbox/Test-EthereumPublicMetadataCandidate.ps1` passes a real anonymous .NET HTTP probe against https://ethereum-rpc.publicnode.com. Requires eth_chainId0x1, resolves one finalized block and uses EIP-1898 blockHash/requireCanonical:true for every balance. Configured public addresses0x000…001/002/003 produce3native balance responses and a2address subset. No wallet/signature/transaction/account creation, chain scan, full transaction body or local node. These are native address-state observations, not persisted COSMIC holons or general queries.

Probe has4HTTPrequests/7RPCoperations, unique batch-ID checks, complete responses, capped256KiB buffering, canonical UInt256hex validation and exact BigInteger decimal strings. Final raw evidence `TestResults/EvmPublicSourceCandidateCurrent/native-metadata-probe.json`1171B SHA2567833E00E1A9E740B8654C910FFE0EF0A5F882A8F8A6C0045DC86B83FFDF7D7D6. Initial Python urllib attempts to PublicNode/Cloudflare/Llama returned403; PublicNode body code1010identified client-signature rejection. Existing .NET HttpClient succeeds; no endpoint hopping/fallback added to shipping provider code. A probe-only PowerShell async-return value polluted a batch after introducing capped buffering; suppressing that void-task output corrected the producer, then native3/2passed.

Next red scope: explicit anonymous Ethereum read adapter using common CQL interfaces, bounded native-address membership and chain/block provenance; controls/native3/2, WEB4 query-only admission/discovery, private Mongo5/4pairing/isolation, then hosted/browser acceptance. No new hosted count. Canonical Ethereum wallets/storage remain untouched; Polygon/Arbitrum can reuse a separately admitted chain descriptor only after individual native proof.

Primary protocol references: [Ethereum JSON-RPC](https://ethereum.org/developers/docs/apis/json-rpc/), [EIP-1898 block binding](https://eips.ethereum.org/EIPS/eip-1898), [PublicNode advertised endpoint](https://ethereum.publicnode.com/). Availability was independently observed above, not inferred from documentation.

### Ethereum public CQL adapter milestone

EthereumPublicAccountMetadataSource now implements common ICosmicQueryStorageProvider/read-only activation interfaces over KeyValueStorageProviderBase. Query shape: EthereumAccountAddress equality/In over configured1..8canonical lowercase addresses; query-scoped chainId+genesis verification and finalized canonical block-hash binding for each balance. UInt256balance/number exact decimal strings. Stable chain-scoped NativeRecordHolonMapper IDs; public active roots, no children. No wallet, transactions, managed scan, latest-block fallback or canonical provider changes. Host factory accepts exact bounded anonymous JSON; provider README has spaced C# example/limits.

18controlled cases pass/no skips, including precise maximumUInt256/>2^53block values, point/IN/page/budget, unknown/inactive/pre-cancel/cancel, wrong network/genesis/block/envelope, duplicate/oversize native JSON, late-row error and Take0error.1real .NET mainnet CQL native case passes3/2and exact address order/single blockhash per query. TRX under TestResults/EvmPublicSourceCandidateCurrent: adapter-controls.trx SHA256088C313792DC1574FE1F47FDA522175719750444CA779B3587AD867AEFB89CFD; adapter-native.trx SHA256616BA787F0032410C2617493F9EF97431979D1E82E64DCD2B24E36B13DA5E61C.

Legacy gap: existing Ethereum UnitTests target fails16compile errors from removed GetPlayersNearMe/GetHolonsNearMe/geoNFT/wallet/search request APIs and obsolete enum. Left untouched, original buildlog retained. New narrowly scoped EthereumOASIS.CqlTests source target reuses the same SDK/cache/provider with no new runtime/service; cache-only restore/full build pass. Stable nonsecret config278B at LocalState/COSMIC/HostedEthereumPublicSourceCurrent/source.json retained. No web4/Core permission grant or hosting mutation. Next: isolated query-only WEB4 registration/admission/discovery, Mongo HTTP5/4/isolation, C# demo and browser. Hosted provider count stays8.

### Ethereum WEB4 profile milestone (task only)

Startup now registers CQL_ETHEREUM_PUBLIC_SOURCE_JSON lazily through isolated CqlReadSourceProfile, matching other explicit read representations; canonical wallet DNA/registry remain unchanged. Compiler read-profile scope and unordered bounded membership admit Ethereum leaf/federation without expanding the three-store composition parity set. Private Core unsigned-query supported set adds Ethereum; independent host permission and exact query tariff still required. StorageAttemptTariff still refuses unsigned SaveHolon pricing for Ethereum. Existing unsupported-profile test now uses Polygon, which has no unsigned read adapter enrolled.

12-provider catalogue test covers independent missing permission/price, duplicate price and forbidden write tariff. Native3/2/Mongo pair presets plan; ready source leaf examples remain available when Mongo is absent and pairs disable. Full shipping API/test/Core-test builds pass.24affected API/profile/discovery/schema cases and78Core admission cases pass/no skips. TRX under TestResults/EvmPublicSourceCandidateCurrent: web4-admission.trx SHA2565F2C557979A7C6201BED15BFCC1E78107D8D2BCDBEDB999F0320ECCC0399D031; core-admission.trx SHA2569FD267B8CC8FD3BF224A1F2E2E31D0029DC3058CDCF6A8080D2F2117E61F236B. First Startup compile lacked the Services.Cql namespace; fixed to existing lazy factory/lifetime registration before acceptance, original log retained. No native-source green rerun or new live session/vars/deployment.

Core975c3750f676fb4724c74a08a2b240faa790550d pushed/remote verified on task branch; parent gitlink+manifest advance together. Frozen release Core6bb/root79fe887e3 remains unchanged and excludes Google/Ethereum. Next actual Ethereum/Mongo HTTP5/4private snapshot/isolation acceptance and source-specific local/demo presets. Hosted count stays8.

### Ethereum/Mongo HTTP and sandbox composition milestone

CqlEthereumMongoNativeTests passes1real native/no skips through shipping leaf/federation endpoints over loopback Kestrel: Ethereum3/2, two private SDK-owned Mongo snapshots, paired5/4, foreign-private0/public-pair3. Every selected physical ID/provider/public flag/private owner checked;9query receipts validated. Source catalogue unchanged; no chain write. Controlled bearer/usage mocks, not production auth/billing/replication/browser proof. Existing runner -EthereumMongoHttp owns Mongo/lease, stores raw reports in TestResults/EvmPublicSourceCandidateCurrent. Durable original intent plus2ACKs+cleaned confirmation retained (4files845B); nonce/owner/2Holon checks, database absence verified, service stopped. NativeTRXSHA256D7242B23BBB6F69FAE192EADCF568FBAF25DABD7FB7C08C5E73DAED785E73F03.

Frontend331258eadds Ethereum preparation coverage; all49client tests pass. Existing generic production recipe already handles the explicit address metadata key. First2selected source keys use Core-derived private IDs; repeat0writes/2existing, no chain Save. Four server presets prepared in protected individual candidate;3offline config tests pass (including encrypted-key preflight). Source/sourceOwner and canonical wallet DNA preserved, no cloud spend or signers.

For one scheduled Google+Ethereum release, use Publish-HostedGoogleEthereumConfiguration.py. It composes both strict candidate validators into4changed values: two source profiles plus shared admission/examples, preserving8presets and both query tariffs.3offline composition tests pass, including no before-object mutation, existing-enrollment refusal and both explicit encrypted query-key grants. Protected before/candidate prepared; no live apply/deployment. Individual candidates from the same previous snapshot must not be applied sequentially to overwrite shared lists. Combined candidate now owns the joint rollout.

Config logs TestResults/EvmPublicSourceCandidateCurrent/configuration-controls.log SHA256949F94D2C1F83530348F9E925BE1BB40690FC60F5338EDCCFC429BE2F8B88E84; combined-configuration-controls.log SHA256205831DAE99C7E8B2A1AB656024B3C6AC6FB9D7015C1BDDE48A7607A06027A63. Hosted source count stays8; next runnable Ethereum C# walkthrough and matching scheduled rollout/HTTP/browser gates.

### Runnable Ethereum C# demo milestone

Examples/CosmicCqlDemo now supports --ethereum-public <existing-source.json>, reusing the explicit public profile and shared NativeKeySetWalkthrough through HolonManager/CosmicLinq. Full3address selection and a2address subset print source IDs/keys, exact wei balance strings and RPC-reported finalized block hash. No wallet/transaction/Mongo fixture. The existing C# federation helper is not newly admitted for Ethereum by this console change; HTTP pairing remains its separate verified path.

Full demo/test builds pass after cache-only restore of the new project reference. Actual CLI exits0/3full/2filtered;1EthereumPublicCSharpDemoNative case passes/no skips, compiling the published example files directly. Evidence TestResults/EvmPublicSourceCandidateCurrent/console-sample-native.trx SHA25641B395CF2A1AD84855CA471D096F741004551153580B4981DDFD4943DEA68BE6; console-native.log SHA25619C516729C391AA000A634C6C2CDAB9A02B6EAACED5A13B36C23437E1E997E37. New official ETHEREUM_PUBLIC_METADATA_CQL_GUIDE.md has spaced CLI/C#/HTTP usage, complete nonsecret catalogue example, permissions/encryption/bounds and honest hosted limits. Next matching scheduled rollout then actual hosted/browser gates; no new live provider claimed.

Frontend public release9b65a88: Google/Ethereum CLI+guide/sample links published in their existing catalogue cards, keeping public read profiles separate from local owned-record/emulator examples.49client controls pass; exact public HTMLSHA2565be5c5181f92765a0bd56e3883d73e7ece59a2708a5d9ad0f2a1949adf0b1bab and both unchangedCQLscripts match git blobs. Raw boundedHTTPcomparison TestResults/HostedCqlIntegrationCurrent/frontend-google-ethereum-publication.json. No backend settings/deploy changed, hosted-provider count8and browser count5unchanged.

### Polygon native public CQL milestone

Free PublicNode mainnet probe verifies chain0x89, genesis0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b, RPC-reported finalized block and EIP1898canonical-hash3/2balance reads. No wallets/funding/transactions/localnode. Source https://polygon-bor-rpc.publicnode.com/; [operator endpoint](https://polygon-bor-rpc.publicnode.com/) / [Polygon chain ID](https://support.polygon.technology/support/solutions/articles/82000889434-how-to-add-polygon-mainnet-on-metamask-). Probe is not CQL acceptance; native proof below is separate.

Production read engine now lives once in Web3Core/src/EvmPublicAccountMetadataSource.cs. Fixed Ethereum/Polygon descriptors own chain/genesis/provider/name/address metadata; Ethereum keeps its public constructor/JSON schema, native source-name mapping and metadata names. This extraction is necessary for the red Polygon feature, not further Ethereum parity work; its affected regressions were rerun. No canonical signed-provider/legacy edits. PolygonPublicAccountMetadataSource supports exact PolygonAccountAddress equality/In, configured1..8lowercase addresses, chain+genesis+finalized canonicalhash verification and exact UInt256balance strings. Same bounded complete-source/error/privacy/page/read-only contract as Ethereum.

36family controls pass (18newPolygon+18affectedEthereum),3native cases pass (Polygon3/2, Ethereum3/2, Ethereum published manager/LINQ sample). Full shipping API/test build and3affected Ethereum/profile/schema tests pass. All zero skips. Family tests reuse existing EthereumCqlTests target/cache; no new environment. Initial wrapper enum/property-name ambiguity caused2compile errors, corrected with explicit enumalias before tests.

Evidence TestResults/PolygonPublicSourceCandidateCurrent: family-controls.trx SHA2564C7AE702942E1B95FBFA5D06671AB6AFE9674B91EE8C64D98990772E3817BEA7; family-native.trx SHA2562223B9D3BBE6FDF43071BEAFEB6317258BBF95AAF60437DE8B6EC7CD6C5059EE; shipping-profile.trx SHA256C4D0D0F10C9D2B0DCC94C848BB8F10CC8AC850D05AA9C9AA792C6301417ACB45. Stable sourceconfig282B retained under LocalState/COSMIC/HostedPolygonPublicSourceCurrent. Next Polygon WEB4 query-only permission/tariff/profile/discovery, actual Mongo HTTP5/4and isolation, local sample/presets, scheduled rollout/browser. Hosted source count remains8.

### Polygon WEB4/HTTP composition milestone

CQL_POLYGON_PUBLIC_SOURCE_JSON now registers an isolated read profile. Compiler leaf/federation native-address scopes and private Core unsigned query tariffs includePolygon, with independent permission/price and no signed-write grant.13-provider catalogue/preset/discovery controls pass. Permanent unresolved-provider rejection uses None rather than assuming a requested chain can never gain a public read profile. Full API/test/Core-test builds and26API/79Core affected cases pass/no skips.

CqlPolygonMongoNativeTests passes1native HTTP case: Polygon3/2, SDK-created private Mongo2, federation5/4, foreign-private0/public-pair3, exact physical IDs/provider/public/private ownership and9validated query receipts. Controlled bearer/usage mocks, not production billing/login/replica/browser proof. Stable source unchanged; owned DBcosmic_cql_polygon_http_demo removed after marker/2Holon checks and absence verification, Mongo27029stopped. Durable4files843B retained.

Raw evidence TestResults/PolygonPublicSourceCandidateCurrent: web4-admission.trx SHA256B1EBEECE02CC483CF00FE8683917D2F1E752A72491C6B5AE17418DE005D22444; core-admission.trx SHA256638A325FA1567599FAAF5EF6217EEE2375665EF15BB5389C73A6C466A747C786; polygon-mongo-http.trx SHA256D6779C0A1687C2CDAE6784AC637FEA2D887DFEF8742FFAF8ADA44DB907ECB99E.

Frontend task13703b7adds Polygon preparation case; all50client controls pass with no productionJSbranch.3Polygon config tests and4composition tests pass. The optional with-polygon publisher path composes Google/Ethereum/Polygon into5values/12presets, preserves canonical walletDNA and explicit encrypted-key grants. Protected before67,094B/candidate75,302B prepared; no live mutation/deployment. Prior2source candidates retained as historical preparation, not sequentially applied over one another. Configlogs hashes03D21374EBDCFACA43CC0220C6975F17D6D44EC4B0458D2E8FA972645AFF4B82 /D5EF5B2DE868B070F431BE21D6C9A4C3B096986CBF4C970CD43A32B494E6FF67.

Corea65b8271a22ff479f07c86d3a479b8e44784dbb0pushed/remote verified; parent gitlink+manifest advance together. Frozen Core6bb/release79feunchanged. Next runnable Polygon C# demo and catalogue links, then matching scheduled rollout/hosted HTTP/browser/replica gates. Hosted source count8/browser5unchanged; no full ORM/chain-write parity inferred.

### Runnable Polygon C# milestone

CosmicCqlDemo now accepts --polygon-public <existing-source.json> before local fixture setup. PolygonPublicWalkthrough uses the common NativeKeySetWalkthrough/HolonManager/CosmicLinq leaf path, reads3addresses then2, prints exact native-base-unit balances and RPC-reported finalized block hashes. No wallet, funding, source transaction or Mongo fixture. The C# federation helper is not broadened in this change; HTTP pairing remains separate verified proof.

Full demo/test builds pass with cache-only restore of the existing Polygon reference. ActualCLIexit0/3+2and1PolygonPublicCSharpDemoNative case pass/no skips, compiling the published sample files. TestResults/PolygonPublicSourceCandidateCurrent/console-sample-native.trx SHA256415958860A8A5851495D6C118F8EF6597EDF382CB5FAE40D8E9088970C016392; console-native.log SHA256F4FFD5C9397995BB7B18C1E93CA162FFDE32A18D47185795FF5F732A4AB52BD6. New POLYGON_PUBLIC_METADATA_CQL_GUIDE.md has complete nonsecret catalogue, commented CLI/C#/HTTP steps, fixed chain scope, encrypted-key policy and pending hosted limits. All local links resolve. Next catalogue link and matching scheduled rollout/hosted/browser gates; source count8/browser5unchanged.

### Arbitrum One native public CQL milestone

Initial PublicNode readiness failed because finalized balance state was unavailable, despite matching chain/genesis/finalized headers. Preserved diagnostics show the native historical-state error. Official https://arb1.arbitrum.io/rpc independently supplies the same finalized state, so the profile explicitly selects that endpoint. No automatic endpoint fallback or latest-state substitution. Chain 0xa4b1 and genesis 0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442 are pinned. [Primary chain info](https://docs.arbitrum.io/chain-info); native observations are separate evidence.

ArbitrumPublicAccountMetadataSource wraps the common EVM reader. Its trusted descriptor adds a fixed /rpc path; default Ethereum/Polygon descriptors remain root-only. All other strict JSON/URI/credential/quantity/complete-source/page/read-only contracts are shared. Native address membership yields exact ArbitrumBalanceWei strings and RPC-reported finalized block provenance. No wallet/node/funding/chain write.

54 family controls pass (18 new Arbitrum plus 36 affected Ethereum/Polygon); five native cases pass (three source 3/2 cases plus Ethereum/Polygon published C# demos). Shipping API/test build and five affected profile/schema cases pass. No skips. TestResults/ArbitrumPublicSourceCandidateCurrent: family-controls.trx SHA256A896FDDC580D122CD697F46B5BC06AD5869E292B778BE180F0E6D2FFC3891AD2; family-native.trx SHA256628C08B3A28D5AE9B8CEA3A7732EE898372480A76D4122FE399669EB2829BFEE; shipping-profile.trx SHA256DBCA8E28BF60F26A2CBA22E24A26D25E09C418AB67180D5E60F668F7DF55489A. Stable nonsecret source273B retained under LocalState/COSMIC/HostedArbitrumPublicSourceCurrent.

Next red scope: isolated WEB4 Arbitrum query-only registration/permission/tariff, actual Mongo HTTP 5/4 and isolation, presets/C# demo, scheduled hosted/browser acceptance. No new hosted provider claimed (8 HTTP/5 browser unchanged). RPC-reported finalized L2 state is not independent rollup settlement/L1 proof.

### Arbitrum WEB4/HTTP composition milestone

CQL_ARBITRUM_PUBLIC_SOURCE_JSON now registers an isolated read representation. Core unsigned-query price bounds include14supported profiles with independent permission/price; no signed-write grant. Compiler leaf/federation retains exact native unordered address capabilities. Full API/test/Core-test builds,28API and80Core admission cases pass/no skips.

Native CqlArbitrumMongoNativeTests passes1HTTP case: source3/2, private Mongo2 normal SDK create-only saves, paired5/4, foreign-private0/public-pair3, exact selected physical IDs/provider/visibility/owner,9query receipts. Controlled bearer/usage authority is not production login/billing/replica/browser proof. Sourceconfig unchanged, owned DBcosmic_cql_arbitrum_http_demo removed after marker/2Holon/absence checks; Mongo27029stopped. Durable4files845B retained.

Evidence TestResults/ArbitrumPublicSourceCandidateCurrent: web4-admission.trx SHA2565FD26A524C874921A1A395B57C5175B6A927A12E02E73D68555C01CFDA70BF5C; core-admission.trx SHA256C56EC9D34420B14A98DD80A295785D3282509FA3ABD32575B589119FDB99FF97; arbitrum-mongo-http.trx SHA256EB6E821E5984A00D76C54C73E0546D0DE352E977D2944CD099AEFE00AA53483E.

Frontendbd2d119adds Arbitrum preparation;51client controls pass with no productionJSbranch.3Arbitrum configuration and5composition cases pass. Optional with-arbitrum joint publisher composes Google/Ethereum/Polygon/Arbitrum,6changed values/16presets, preserving existing walletDNA and explicit encrypted query-key grants. Protected before67,094B/candidate77,782Bprepared; no live apply/deployment. Individual Arbitrum candidate not generated. Configlogs hashes956C2D4739791064D7CD4952BB4ED30C734416B5DCA12AB908D7804598A1E4A7 /5C56E747CEFA9BBD70A453233E60F0EF075537478746FF4EA64AB2B2DE56E926.

Coreb84ab80078350d67fdf3103a994aaf7bf03f3e05pushed/remoteverified; parent pin+gitlink advance together. Frozen Core6bb/root79feunchanged. Next runnable Arbitrum C# sample/catalogue links and scheduled matching rollout/hosted/browser/replica gates. Actual live counts8HTTP/5browser unchanged.

Deferred mixed-role admission gap: PricedStorageAdmissionFactory rejects native bindings whose provider is in its unsigned/query-only set (line67), and PreparedStorageExecution likewise requires disjoint signers/nonNative permissions. This protects current read-only demos but prevents one factory from simultaneously authorizing a provider's public unsigned query representation and canonical signed writes. Publishers correctly reject existing overlapping enrollment and preserve walletDNA; they do not remove signers. A later operation/representation-scoped admission model and native/HTTP tests are required. Do not claim this mixed-mode contract complete or work around it by silent source substitution. It is engineering work after the demo scope, not an owner setup task.

### Runnable Arbitrum C# milestone

CosmicCqlDemo now accepts --arbitrum-public <existing-source.json>. ArbitrumPublicWalkthrough uses shared NativeKeySetWalkthrough/HolonManager/CosmicLinq for3addresses then2, printing exact native balances and RPC-reported finalized hashes. Official fixed /rpc source, no wallet/funding/transaction/Mongo fixture. C# federation helper is not broadened; HTTP pairing remains separate proof.

Example/test builds pass after cache-only restore of the existing Arbitrum reference. ActualCLIexit0/3+2and1ArbitrumPublicCSharpDemoNative pass/no skips, compiling the published sample files. Raw evidence TestResults/ArbitrumPublicSourceCandidateCurrent/console-sample-native.trx SHA2562FA7897F6CB346BCF7892592827EC3BA21ECED462F6E09B4C68A227DE06D3B56; console-native.log SHA2567F3A6149EC19C2FDAD375B5818AD80211FAA19BE2FA616D0DA50A415D49DD890. New ARBITRUM_PUBLIC_METADATA_CQL_GUIDE.md provides complete nonsecret catalogue and spaced CLI/C#/HTTP steps, endpoint/finality/mixed-role scope limits. Local guide links verified. Next stage catalogue link beside Polygon, then scheduled matching rollout and hosted/browser/replica gates. Live counts8HTTP/5browser unchanged.
