# ENS public CQL source — next interface review

2 November 2026. Native protocol and local CQL console qualified. Hosted functional HTTP acceptance passed; card/publication and browser proof
remain open. Counts58integrated/54hosted. Earlier no-probe statements below are historical.

Existing provider: Providers/Network/NextGenSoftware.OASIS.API.Providers.ENSOASIS/ENSOASIS.cs.
It reads a hardcoded ENS Subgraph URL, https://api.thegraph.com/subgraphs/name/ensdomains/ens,
and projects names/owners into avatar/holon metadata. Current public availability,
normalization and response contracts have not been verified in this milestone.
Legacy code remains unchanged; document its gaps rather than repairing it.

Next verify official current ENS normalization/namehash/registry/resolver read
contracts and public-mainnet RPC eligibility, using existing cached runtime/RPC
helpers. Propose an explicit bounded three-name read catalogue and filtered pair,
with precise resolver/address/snapshot provenance and unresolved records modeled
explicitly. Do not infer wallet ownership, full ENS support or independent
consensus from a returned resolution. Define supported name forms and offchain
resolution behavior before implementation; no silent gateway/endpoint fallback.

Before any native call, retain a single exclusive intent with exact endpoint,
network/selector bounds and request budgets. Use the production guarded transport,
no paid key/account/install/new node, no transactions or source writes. Only after
native qualification implement the query-only profile, controlled tests, spaced
C# walkthrough, HTTP/Mongo/privacy/replicas and original live acceptance/card.

## Verified protocol decision — 2 November 2026

Official sources reviewed (not native execution evidence):

- [ENSv2 readiness](https://docs.ens.domains/web/ensv2-readiness/):
  the upgradable Universal Resolver is the resolution entry point. The documented
  integration sentinel `ur.integration-tests.eth` must return
  `0x2222222222222222222222222222222222222222`; the old-route result
  `0x1111111111111111111111111111111111111111` must fail qualification.
- [Universal Resolver](https://docs.ens.domains/resolvers/universal/):
  mainnet proxy `0xeeeeeeee14d718c2b47d9923deab1335e144eeee`,
  `resolve(bytes name, bytes data)` returns `(bytes result, address resolver)`.
  DNS-encode the normalized name; inner record call is `addr(bytes32 node)`.
  Never substitute direct registry/per-name resolver lookup when this fails.
- [ENSIP-15](https://docs.ens.domains/ensip/15/): normalization includes Unicode
  and emoji rules; lowercasing or UTS-46 alone is not general ENS normalization.

Qualification catalogue: exactly `nick.eth`, `vitalik.eth`,
`ur.integration-tests.eth`; filtered pair: `nick.eth`, `vitalik.eth`.
These are explicit pre-normalized ASCII fixtures, not an arbitrary-name API or
suffix-based ENS detector. The initial profile must reject names outside its
configured catalogue and must not advertise full Unicode normalization.

Reuse the existing anonymous Ethereum RPC endpoint
`https://ethereum-rpc.publicnode.com/`, SDK10.0.401 and cached Nethereum7.0.0
ABI/Keccak dependencies; do not modify the completed Ethereum source profile.
Before native execution, exclusively persist the exact qualification intent.
Bound the sequence to chain identity, finalized block observation, proxy-code
check and three resolution calls at that same canonical block hash. Record the
actual selector, block hash, resolver, raw-response hashes and resolved address.
Reject malformed ABI offsets/lengths/padding, wrong chain, missing proxy code,
wrong sentinel, zero/empty records and failed RPC calls; never turn failures into
successful unresolved rows. All-or-error three-record qualification.

CCIP Read is a separate protocol requirement, not a transport fallback. Until
its guarded gateway/callback verification is implemented and tested, surface
`OffchainLookup` explicitly as unsupported for this bounded direct-resolution
profile. Do not follow arbitrary gateway URLs or replace the Universal Resolver.
General CCIP/wildcard/Unicode/multicoin/text/reverse/ownership/history support
remains red and must be disclosed in the eventual demo guide.

Existing Ethereum test host already references the Ethereum provider and its
cached ABI/Keccak dependency graph. Use it for codec/protocol qualification
checks; do not create another environment. No native call or installation was
made during this review. Next: implement bounded DNS/namehash/ABI codec and
controlled rejection tests, then record the native intent and execute once.

## Codec milestone verified

Production `EnsUniversalResolverCodec.cs` lives in the ENS provider project;
the reused Ethereum test host references that actual project. Cached
Nethereum.Util7.0.0 supplies Keccak, avoiding a second hashing implementation.
The codec deliberately accepts bounded lowercase ASCII labels, encodes DNS and
namehash, constructs Universal Resolver/addr ABI, and rejects malformed offsets,
lengths, padding, empty addresses, truncation and trailing bytes.

19 controlled tests passed,0failed/0skipped. Tests include the official DNS
example, known eth namehash, dynamic-tail layout and invalid-name/ABI cases.
Evidence: `TestResults/EnsPublicSourceCurrent/codec.trx`, SHA256 `83fc71e2456cfc92c51e2d2eb7978a51ba12bbae0cc82cfcf84b009e25808ac1`.
Build log is adjacent `codec-build-test.log`; raw output remains outside Docs.
No native ENS call has run and no source/admission/card is counted.

Next implement the guarded bounded qualification runner using this codec;
freeze an exclusive three-name intent before its first native request.

## Native protocol qualification passed — original intent, no replay

The production `EnsPublicResolutionReader` is consumed by the actual existing
Ethereum test host.10controlled protocol tests passed/0failed/0skipped:
canonical block selectors, wrong chain/block/code/sentinel, RPC envelope/errors,
oversized bodies and invalid catalogues.19codec tests passed at prior milestone.

One guarded native test passed with the original three names; six JSON-RPC
reads, no transactions, gateways, endpoint changes or retries. It checked chain1,
finalized block and proxy code, then resolved all names at the same EIP-1898
canonical block hash `0x9e3f030475ded631905d3511551fab452473203e7a5c19aa761b95762caa3443`
(number `0x191baa9`). The official integration sentinel returned
the required0x222 address. These are endpoint-reported finalized observations,
not an independent consensus or ownership verification.

Durable intent and ACK: `LocalState/COSMIC/EnsPublicQualificationCurrent/`
(`qualification.intent.json`, `qualification.acknowledged.json`). ACK SHA256
`6e158defa01ba4a81c07f272f2bc4bb33fb650306a43c76ef2835ab571a77631`. Preserve; never replay the
native test or replace the intent to reproduce green. Raw native observations,
logs and TRX remain in `TestResults/EnsPublicSourceCurrent/`.

Next implement the query-only ENS catalogue adapter and its capability/projection
tests, then the spaced manager/LINQ3/2 walkthrough. Hosted enrolment, Mongo
private-copy/privacy/replica acceptance and website card remain unimplemented.
Native qualification alone does not increase the integrated/hosted counts.

## CQL adapter milestone verified

`EnsPublicNameMetadataSource` now implements the generic CQL storage and
read-only activation contracts over the production resolver reader.17controlled
tests pass/0failed/0skipped. Explicit `EnsName` equality/IN,1–8configured names
and captured offset/limit are supported. Unknown catalogue keys, inactive or
cancelled reads, over-budget pages, malformed/failed native reads, unsupported
content filters and native writes fail with real errors. Selected records are
fully validated before paging, including Take(0); no partial rows on failure.

Projected public holons retain configured owner, endpoint-specific stable IDs,
native name key, resolved ETH address, individual resolver, Universal Resolver,
shared query block hash/number, response hash and explicit node-reported policy.
No account ownership, consensus verification or unrestricted ENS support claimed.
Exact host JSON fields: schema1/rpcUrl/names/sourceOwner; duplicates, unknown
fields, unsupported names, empty owner and credential-bearing URLs rejected.
The factory uses the existing guarded HTTP transport with cookies disabled.

Evidence `TestResults/EnsPublicSourceCurrent/adapter-controls.trx`, SHA256
`9e57056303eadd77aef966373b93f6708ae3172f0460b136fe8a5763b0de2626`; adjacent adapter-build-test.log.
No additional native calls or source owner were allocated. Original protocol
qualification remains frozen. Next: spaced C# HolonManager/deferred-LINQ
walkthrough3/2, then HTTP/Mongo/privacy/replica and live integration gates.
Integrated57/hosted53 counts remain unchanged until that integration is proved.

## Actual C# console milestone passed

`Examples/CosmicCqlDemo/EnsPublicWalkthrough.cs` and `--ens-public` now use
the production ENS adapter and shared manager/deferred-LINQ walkthrough. The
actual demo project built0errors. One frozen-binary CLI invocation passed3full/
2filtered records, using the retained three names and one source owner. No
Mongo copy, wallet, transaction or source write. This is additional console
acceptance, not a replay of the immutable native protocol qualification.

Retained source/config/console intent/ACK:
`LocalState/COSMIC/HostedEnsPublicSourceCurrent/`. Source SHA256
`f16c146823743a77394d54e302b7d54b013c8a78a7779cc16266c76ed0f90455`, binary SHA256 `817309fa8169dce1f84d2907c55def125b377e7df60be4b485af4978d655e4d1`.
Raw console stdout/stderr/build log: `TestResults/EnsPublicSourceCurrent/`.
Runner `Scripts/TestHosts/Run-CosmicEnsPublicConsole.py` refuses an existing
source state and preserves failures rather than replacing identities. Do not
rerun a completed acceptance invocation to reproduce green.

Developer guide: [ENS public-name CQL](../../Devs/ENS_PUBLIC_NAME_CQL_GUIDE.md).
58integrated types now include51bounded public/source profiles; hosted remains53
and browser5verified/48open. Next implement HTTP compiler/startup registration,
query-only tariff and controlled API tests, then the retained native/Mongo/privacy
fixture and original hosted/private-copy/replica acceptance before website card.

## HTTP admission controls passed

WEB4 now explicitly registers CQL_ENS_PUBLIC_SOURCE_JSON as a query-only read
profile; both HTTP compiler provider lists include ENS. No retry policy is
added to this source. Core tariff permission includes ENS reads, not writes.
Core task SHA 13add812baa3d6ffea4f5cd49018e5e3fb959d4d is pushed and verified with ls-remote. Parent manifest
and gitlink advance together; Development/live settings remain unchanged.

Core1/API3 targeted tests passed0failed/0skipped; WEB4 compiled successfully.
API controls prove 3/2 native and paired presets plan, independent permission/
tariff is required, and unavailable Mongo leaves pairs explicitly unavailable.
The existing128character demo-copy key boundary is preserved (all demo names
fit); provider lookup accepts bounded names up to253characters.

Evidence under TestResults/EnsPublicSourceCurrent (SHA256):

- http-admission-core.trx: 359324a9888873eb16bb2450b2968a8b01c09feb2499efb0f54827a647079212
- http-admission-api.trx: e542ee2a37f66ac70421c3d04d150e16ff3c5d7e5833e95f26db83d7808b32f5

Next: owned local HTTP/Mongo fixture3/2+5/4, privacy and cleanup proof;
then reviewed hosting configuration/publication and original hosted acceptance.
No live ENS admission/card, source writes or new hosting resources yet.

## Local native HTTP/Mongo acceptance passed

One original `CqlEnsMongoNativeTests` fixture passed with real ENS RPC/Mongo
through shipping CQL endpoints over loopback HTTP. Native3/2; federation5/4;
foreign-private0/public3. Every selected physical identity, source/provenance,
visibility and private-copy version1 was checked, not only counts. Two SDK-owned
private Mongo saves/9query receipts; no production billing or replication claim.

Evidence: `TestResults/EnsPublicSourceCurrent/mongo-http-native.trx`, SHA256
`31f64094873231150880d789921ea519efd4332f17cffae1659cc1ef23290efc`.
Summary/log/service output stays beside it. Durable original fixture identity,
save ACKs and cleanup: `LocalState/COSMIC/EnsMongoHttpCurrent/`. Existing source
config SHA256 `f16c146823743a77394d54e302b7d54b013c8a78a7779cc16266c76ed0f90455` unchanged;
`HostedEnsPublicSourceCurrent/web4-local-qualified.acknowledged.json` frozen.

Success-only cleanup validated the exact fixture marker and two-record count,
stopped HTTP consumers, dropped only `cosmic_cql_ens_http_demo`, confirmed its
absence, and stopped the owned Mongo process. Port27029has0listeners. Existing
shared mongo-data/runtime retained. Do not replay the fixture or recreate its
owner/config/ACKs. Controlled bearer/admission is test infrastructure; live signup,
normal API saves and exact SQLite/Neo4j replicas need separate hosted acceptance.

Next prepare a protected hosted candidate from current live settings, changing
only ENS source/admission/examples; preserve unrelated and concurrent settings.
Test that candidate/publisher and actual four presets before any deployment.
Hosted counts remain53; ENS card/browser still unverified.

## Hosted candidate controls passed

Protected before/candidate snapshots exist once at
LocalState/COSMIC/HostedSourceRolloutCurrent/ens-public-*-variables.dpapi.
Publisher9pure tests and actual four-preset planner1test passed0failed. The
three changed keys are CQL_ENS_PUBLIC_SOURCE_JSON, PREPARED_STORAGE_ADMISSION_JSON
and CQL_HOSTED_QUERY_EXAMPLES__ENSOASIS. All unrelated variables/DNA preserved;
zero-cost IPFS tariff cloned only for explicit ENS query permission. Encryption
requires EnsName explicitly queryable if enabled. No native-write admission.

Public-only review and test output: TestResults/EnsPublicSourceCurrent/
prepared-review.json, actual-preset-review.json, publisher-controls.log and
prepared-presets.trx. Protected snapshots must never be printed or committed.
No live settings changed during preparation. Next publish verified Core/parent
code, observe that exact code job, then apply the existing candidate once and
observe one settings activation before original-owner hosted acceptance.

## Development code publication checkpoint

Parent a20f35f9a85af3807f42e1273d7cc6e0fabb0934 and Core
13add812baa3d6ffea4f5cd49018e5e3fb959d4d fast-forwarded to Development,
verified using ls-remote. Manifest/seven Dockerfiles/gitlinks pass. No master
promotion. Original publication intent/observation retained at
LocalState/COSMIC/HostedEnsReleaseCurrent. Exact code job
dcf95c97-dd85-4b82-8f36-b65b8a257ef4 is QUEUED at this checkpoint;
continue observing this same job, never deploy again because observation expires.
No live ENS settings applied and no hosted cohort created yet. Existing protected
candidate/source/configuration remains unchanged.

Deployment re-observation: the SAME code job remains QUEUED (specific CLI
job/commit verified again). No restart, replacement job, settings application or
hosted session created. A publisher negative-test fixture contained duplicate
exitCode literals and a misleading inherited policy-test name; corrected to one
explicit failed-console code and descriptive test name. All9pure tests pass
again; production publisher/candidate unchanged. Latest observation and corrected
test log remain TestResults/EnsPublicSourceCurrent.

## Offline hosted audit ready while the exact build runs

Scripts/Sandbox/Audit-HostedEnsAcceptance.py audits original source/owner/native
3/2, one block per query, sentinel, private version1 snapshots, federation5/4,
repeat0new/2existing, exact SQLite/Neo4j projections, privacy and two normal API
save-operation ACKs with no pending intents. It reads local captured evidence
only and exclusively writes the final acceptance ACK after all checks pass;
never sends network requests, creates an avatar or saves a holon.

Two controlled test methods pass, including nine rejected mutation subcases
(sentinel/ownership/blocks/replica/version/privacy/operation/pending/resave).
Evidence TestResults/EnsPublicSourceCurrent/acceptance-audit-controls.log.
No claim of live acceptance until the actual retained cohort passes.
The same dcf95c97 code deployment remains BUILDING at this checkpoint.

## Code success and settings activation

Exact codedcf95c97 SUCCESS at a20f35f9a85af3807f42e1273d7cc6e0fabb0934.
Code success ACK retained. Checked out that exact deployed revision for publisher
application, then restored the task branch. The existing protected candidate was
applied once: all3per-key hashes/ACKs and complete variable dictionary match;
DNA/unrelated settings unchanged. No extra resource or paid hosting.

One redeploy of the same existing artifact was requested after exclusive
activation.intent.json; raw CLI receipt DPAPI-protected. Exact activation
1f852f42-b2db-4be4-b179-36f88db6e16a is BUILDING. Preserve journal
LocalState/COSMIC/HostedEnsReleaseCurrent and observe this same job; do not
reapply settings or restart. No hosted test cohort/card yet. Next success/readiness,
then original normal-visitor acceptance once and offline audit.

## Hosted functional HTTP acceptance passed

Settings activation1f852f42 SUCCESS at exact a20f35f9a release; all4ENS
discovery presets available and federation ready. Original normal-visitor cohort
completed native3/2+Mongo5/4; repeat0new/2existing, foreign-private0/public3;
exact SQLite/Neo4j2/2 projections. Two normal API private version1 saves with
matching operation ACKs; no pending intents, no continuations or repeated saves.

Offline audit initially misread foreignPrivateIds as a list; actual API wrapper
reported isErrorfalse/count0/rows[]. Fixed checker and controlled fixture to use
the canonical wrapper. Original failure preserved at
TestResults/EnsPublicSourceCurrent/audit-initial-failure.json. Corrected two test
methods/9negative subcases pass; offline re-audit passed without native requests
or saves. This checker failure does not indicate a data-privacy failure.

Frozen original cohort/source/save ACKs/final audit:
LocalState/COSMIC/HostedQueryAcceptance-ENSOASIS/; source/config unchanged in
HostedEnsPublicSourceCurrent. Raw native/client-flow/isolation proofs:
TestResults/HostedCqlIntegrationCurrent/ENSOASIS-*.json. No browser interaction or
reliability certification; no source writes/ownership/independent consensus claim.
58integrated/54hosted functional types; browser5verified/49open.
Next publish the ENS website card with pinned guide/C#links, verify public asset
bytes, retain browser gate separately, then proceed to next red Eclipse review.
