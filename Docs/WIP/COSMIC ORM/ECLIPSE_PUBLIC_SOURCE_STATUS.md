# Eclipse public source — interface review

2 November 2026. Native protocol and actual local CQL console qualified; HTTP/hosted/card
remain red. One retained source owner/config allocated. Earlier no-probe proposals below are
historical. ENS scoped hosted milestone is complete; its records stay frozen.

Existing provider: Providers/Network/NextGenSoftware.OASIS.API.Providers.EclipseOASIS/EclipseOASIS.cs.
Its legacy adapter uses Solana-style RPC but has no generic CQL implementation.
Observed legacy gaps: getSlot activation does not establish network identity;
account helper returns null for HTTP failure and avatar mapping can substitute
zero/false/empty values; transaction loads/signature scans are different scopes
from bounded public account metadata. Legacy code unchanged; issues docs-only.

## Verified official contract constraints

- [RPC and explorers](https://docs.eclipse.xyz/developers/rpc-and-block-explorers)
  lists https://mainnetbeta-rpc.eclipse.xyz as free testing RPC, rate limited to
  3requests/second, with potential temporary IP bans above that rate. Any native
  gate must respect this limit; no automatic endpoint/proxy switching or retries.
- [Differences from Solana](https://docs.eclipse.xyz/developers/differences-between-eclipse-and-solana.md)
  says native currency is ETH with9decimals. A Solana-compatible wire balance is
  not SOL and is not Ethereum18decimal wei. Preserve unsigned integer text and
  explicit currency/decimals; never copy Solana labels into this profile.
- The same page cautions that Solana program addresses must actually exist on
  Eclipse. Reusing an address string alone is not a valid three-record proof.

## Proposed bounded MVP, still unverified

Reuse cached Solnet/public-key primitives, guarded HTTP and existing runtime;
review SolanaPublicAccountMetadataSource for shareable protocol responsibilities
without changing its completed catalogue or ownership. No new node/environment.

Before any native request record one exclusive exact endpoint/method/budget intent.
Qualify network-reported genesis identity, finalized account context and three
explicit configured account metadata records. Prefer one bounded getMultipleAccounts
request for shared context, excluding account data with an explicit data slice.
Do not invent genesis hash or assume Solana mainnet identity. Current official
sources reviewed here do not establish an independent Eclipse genesis hash.

Potential catalogue: system program and documented sysvars, but exact keys and
existence must be verified before adopting a catalogue/source owner. Null accounts
must be explicit absence or an error according to the declared contract; not zero
balance or a successful record. No source owner/config/admission before native gate.

Initial CQL scope would be configured EclipseAccountAddress equality/IN and paging,
public flat holons with endpoint/network/context/currency provenance, then C#3/2,
HTTP/Mongo5/4/privacy and original hosted/replica acceptance. Native transactions,
account-data download, history scans, Ethereum settlement/Celestia availability
verification and full ORM remain separate red scopes. No counts changed.

## Native qualification passed — original bounded intent

Production `EclipsePublicAccountReader` uses cached Solnet.Wallet6.1.0 for
canonical32byte base58 identity. Twelve controlled tests passed0failed/0skipped:
exact UInt64 values, shared finalized context, payload exclusion, absent/incomplete
records, invalid owner/genesis/envelopes, duplicate fields, overflow/fractions,
invalid catalogue/cancellation and measured request spacing. Calls serialize
through a process-wide semaphore at no more than1request/second, with cancellation
and a30second aggregate deadline. This is not a distributed multi-host rate cap.

One native test passed with two guarded JSON-RPC requests to the documented
free endpoint: getGenesisHash, then getMultipleAccounts for system program, clock
sysvar and rent sysvar. Explicit finalized commitment/base64/dataSlice(0,0),
15second client timeout,256KiB/response and512KiB/aggregate bounds. Three
non-null records shared slot `199439130`. Returned genesis
`EAQLJCV2mh23BsK2P9oYpV5CHVLDNHTxYss3URrNmg3s` is an endpoint-reported identity
now retained for future source pinning; not independent network/consensus proof.

Balances remain exact native integer text (ETH,9decimals), not SOL/18decimalwei.
RentEpoch UInt64Max retained exactly. Account data was empty as requested;
no wallet, transactions, source writes, source owner, node or database created.
Original keys/order/intent and ACK: LocalState/COSMIC/EclipsePublicQualificationCurrent.
Preserve; do not replay the qualifier or replace the intent for green.

Raw logs/TRX/native projection: TestResults/EclipsePublicSourceCurrent.
Protocol TRX SHA256 `e6b3f3fe157eaa17d674f16a3ff92c06ff9b3d6e0b69748bc089fc189dbe025f`;
native TRX SHA256 `3c4636d321d168f33321accfce3237b4b8750eb0d2ecfede9d7e4c1cd4fced1d`.
Existing Solana/ENS implementations/catalogues/ownership unchanged.

Next query-only adapter: mandatory retained genesis pin, explicit configured
EclipseAccountAddress equality/IN+paging, metadata currency/decimals/context
provenance and endpoint-specific stable IDs. Then source controls, spaced actual
C#3/2, HTTP/Mongo/privacy and original hosted/replica/card gates. Counts unchanged
58integrated/54hosted. Legacy gaps remain documentation-only.

## Query-only adapter controls passed

`EclipsePublicAccountMetadataSource` now implements generic CQL storage/read-only
activation over the qualified reader. Exact configured EclipseAccountAddress
equality/IN+paging,1–8keys/max44characters; mandatory canonical genesisHash in
exact schema1/rpcUrl/addresses/genesisHash/sourceOwner config. Unknown/duplicate
fields, invalid keys, empty owner and credential URLs fail. Factory uses guarded
HTTP/cookies disabled. No source owner/config allocated yet.

Projection preserves exact balance/rentEpoch/space/slot integer text, ETH/9decimal
labels, native owner/executable, finalized commitment and node-reported policy.
Stable public IDs include owner, endpoint, genesis and address. Every query checks
the retained genesis pin; selected records validate before paging, including
Take(0). Unsupported content filters/native writes fail, no partial rows.

29controls pass0failed/0skipped:17adapter/configuration/capability cases plus
12reader controls rerun after correcting their shared mock to return requested
cardinality. The first adapter build stopped at a generated namespace typo before
tests; corrected, original build log preserved. No native qualification replay.
Evidence TestResults/EclipsePublicSourceCurrent/adapter-and-protocol-controls.trx,
SHA256 1a40cf9e8527ab3ad11c4a21a636bd6caab006bc6eaf445925eaa8648387d4c9.

Next actual spaced C# manager/deferred-LINQ3/2 walkthrough with one retained
source owner/config; then HTTP/Mongo/privacy/replicas/hosted/card gates. Counts
remain58integrated/54hosted until actual integration is proved.

## Actual C# console integration passed

`Examples/CosmicCqlDemo/EclipsePublicWalkthrough.cs` and --eclipse-public use the
production adapter and shared HolonManager/deferred-LINQ walkthrough. Full demo
build0errors; final consumer-only copy build0errors after making the balance
explanation readable. One actual frozen-binary invocation passed3full/2filtered
records, with retained genesis checked on each read and process-wide rate admission.
No wallet, source writes, Mongo copies or native qualification replay.

Retained source/owner/config/console intent/ACK:
LocalState/COSMIC/HostedEclipsePublicSourceCurrent. Source SHA256
`4adf2a556b32499ea45c3577c90c4304546f94a319b4a055882a8947a20afccf`, binary SHA256 `f3e9c61fbaf99f3bc955eef234183957c6fcf73ba24a2eb49ff769fec3a3e15e`.
Raw console logs and build output summaries: TestResults/EclipsePublicSourceCurrent.
Do not rerun completed acceptance or replace owner/config to reproduce green.
[Developer guide](../../Devs/ECLIPSE_PUBLIC_ACCOUNT_CQL_GUIDE.md) explains exact
query scope, native ETH9decimal precision, rate policy and trust limitations.

59integrated query types now include52bounded public/source profiles; hosted
remains54/browser5verified49open. Next HTTP compiler/startup registration,
query-only tariff and API controls, then local native/Mongo/privacy fixture and
original hosted/save/replica/card acceptance. Deep ORM/consensus/settlement
verification remains outside this bounded read profile.

## HTTP admission controls passed

Explicit CQL_ECLIPSE_PUBLIC_SOURCE_JSON startup profile/project reference and both
HTTP compiler source lists now include Eclipse. Core query-only tariff permission
added; signed/native writes still rejected. Core task SHA 5375672c1e8e9411f017dc0fc766622fd0c08ecb pushed and
ls-remote verified. Parent manifest/gitlink advance together; Development/live
settings remain unchanged.

Core1/API3targeted tests pass0failed/0skipped; WEB4 compiled. Four native3/2
and Mongo paired presets plan using shipping capabilities, independent permission/
tariff required; missing Mongo explicitly disables pairs. Existing128character
demo-copy key boundary retained; Eclipse keys max44fit.

Publisher9pure controls pass including exact retained genesis rejection,
zero-cost admission, unchanged DNA/other settings and matching local proof
requirements. No actual hosted candidate prepared yet: local HTTP/Mongo proof
is required first. These scripts create no resource during pure tests.

Evidence TestResults/EclipsePublicSourceCurrent (SHA256):

- http-admission-core.trx: 62c10844615d7a9aa744ebabcc3c22bcff989ddc60738378c1597943416734fb
- http-admission-api.trx: 7d11ab3c1e337efd456dd65f37980f008fc4ce3c17c4bd04dff866a1c004f799

Next owned local native HTTP/Mongo3/2+5/4/privacy/cleanup; then actual protected
candidate/four-preset review and original hosted acceptance. Roadmaps/status
summaries reconciled to59integrated/54hosted; no new live admission/card yet.

## Local HTTP/Mongo and protected candidate passed

Original CqlEclipseMongoNativeTests passed native3/2, Mongo5/4, foreign-private0/
public3 with two SDK-owned version1 copies and9query receipts. Physical identities,
source visibility/genesis/currency/decimals and private snapshot metadata checked.
Controlled bearer/admission is fixture infrastructure, not production billing or
replication proof. Source config unchanged. Success-only cleanup checked exact
marker/two-holon count and dropped cosmic_cql_eclipse_http_demo; absence ACK
preserved, HTTP stopped and owned Mongo stopped/port27029absent. Shared data kept.

Retained fixture: LocalState/COSMIC/EclipseMongoHttpCurrent. Source-bound local
ACK: HostedEclipsePublicSourceCurrent/web4-local-qualified.acknowledged.json.
Protected before/candidate: HostedSourceRolloutCurrent/eclipse-public-*-variables.dpapi
(prepared once). Public review TestResults/EclipsePublicSourceCurrent/prepared-review.json
and actual-preset-review.json. Exactly3changed values, DNA/all unrelated settings
unchanged, free read-only tariff. No live mutation during preparation.

One actual-candidate planner test passed all4native/Mongo presets. Publisher9pure
controls already pass. Evidence under TestResults/EclipsePublicSourceCurrent:

- mongo-http-native.trx: fc6470ea6a5aaba697c2df458d56cebf7a7e599b598d8e09f98d9846c495bf9c
- prepared-presets.trx: 43aa377d24aa2278fad0f737a593a846836556f8d22306470250f32d088757bb

Next publish verified Core/parent code, observe exact code job, apply existing
candidate once, then one settings activation and original hosted acceptance.
No new native fixture/cohort/owner to reproduce green. Counts59integrated/54hosted.

## Development code publication checkpoint

Parentdbc5c6f75da35c03a482fab58b3c6006ad494df2/Core5375672c1e8e9411f017dc0fc766622fd0c08ecb
fast-forward pushed to Development after fetching/merging current Development
(already up to date); both remote SHAs verified. Manifest/sevenDockerfiles/
gitlinks pass. No production/master promotion. Exact code job
7c15337e-d4b6-438b-945d-d5eab1a946a6 is QUEUED. Original code publication
intent/observation retained under LocalState/COSMIC/HostedEclipseReleaseCurrent.
Observe this same job until terminal; do not redeploy because observation expires.
No live Eclipse settings applied, hosted cohort or card yet. Existing protected
candidate/source owner remain unchanged.

## Offline original-cohort audit ready

Scripts/Sandbox/Audit-HostedEclipseAcceptance.py validates original public owner/
endpoint/genesis/native key, shared finalized slot, ETH9decimal labels/exactUInt64
text/no account data, source3/2, private version1 snapshots, Mongo5/4, reuse0new/
2existing, exact SQLite/Neo4j projections, foreign visibility and two matching
normal API save-operation ACKs/no pending intents. It makes no requests or saves;
final acceptance ACK is exclusively written/fsynced only after actual proof passes.

Two controlled methods pass including12negative mutation subcases. Fixtures use
the real configured Eclipse public keys/genesis and canonical privacy wrappers.
Evidence TestResults/EclipsePublicSourceCurrent/acceptance-audit-controls-final.log.
This is checker readiness, not hosted acceptance. The same code deployment
7c15337e-d4b6-438b-945d-d5eab1a946a6 was re-observed QUEUED; source/candidate
remain unchanged, no hosted cohort/settings application yet.

## Exact code deployment FAILED — settings never applied

Original job7c15337e-d4b6-438b-945d-d5eab1a946a6 is terminal FAILED/stopped
at2026-11-02T16:02:42.571Z. Live Eclipse settings were never applied; no activation,
new hosted cohort or card. Existing protected candidate/source owner unchanged.
Do not restart/redeploy or change code without diagnosing the failed invariant.

CLI filtered/unfiltered build-log reads returned0bytes. Direct authenticated
Railway API buildLogs over an explicit window also returned0records. Deployment
diagnosis is null. Actual deployment events show SNAPSHOT_CODE completed and
BUILD_IMAGE failed with only the generic image-build error; no compiler/package
error or infrastructure cause established. This is a diagnostic evidence gap,
not proof that source code or Railway infrastructure caused the failure.

Protected raw metadata/events/log observations: LocalState/COSMIC/
HostedEclipseReleaseCurrent/code-failure-*.dpapi. Safe compact reports:
TestResults/EclipsePublicSourceCurrent/code-failure-summary.json,
code-failure-api-log-summary.json and code-failure-events-summary.json. Existing
Railway CLI api capability used with existing authentication; no credential
values printed, no signup/new CLI/environment. Public status page was reviewed
but supplied no correlated build incident; do not attribute this failure to it.

Next seek specific failure evidence via the same deployment events/build-log API
(or reproduce an identified packaging invariant against the exact release). No
speculative shim, source fallback, paid hosting or blind replacement job. Akash
pure reader controls progressed independently; whole goal remains active.

Exact-release source-closure check also passed: Eclipse/API project references
and both shipping source files are tracked with exact path case at dbc5c6f75.
Docker command is unavailable locally; no image reproduction/install was attempted.
This rules out only that specific missing-source/reference hypothesis, not the
image-build failure. Report TestResults/EclipsePublicSourceCurrent/exact-release-source-closure.json.

## Later same-job read-only diagnostic check

At2026-11-02T16:51:38.931622Z the exact original job remained FAILED/stopped,
with diagnosis null,0build-log records and the same2events. No redeploy, settings
application, source probe or replacement cohort. Protected response:
LocalState/COSMIC/HostedEclipseReleaseCurrent/code-failure-later-recheck.dpapi;
SHA256625d4f5bd34f2f49bf0e0c5c8add4841b0c97f44284a660b3c1e85822c0731ad.
Compact observation: TestResults/EclipsePublicSourceCurrent/code-failure-later-recheck.json.
No new compiler/packaging cause established. Repeated empty diagnostics do not
justify speculative changes or a blind rebuild during disk recovery.

2November further read-only diagnostics: actual Railway schema exposes no
separate public build-detail query beyond the existing buildLogs/events path.
DeploymentSnapshot contains variable values, so none were requested. Saved
protected metadata inspected only for configuration shape and known build fields:
DOCKERFILE builder, Docker/Dockerfile.web4, null rootDirectory, exactdbc5c6f75SHA.
This rules out that configured-builder/path mismatch, not the failed image cause.
Safe reports TestResults/EclipsePublicSourceCurrent/diagnostic-*-schema.json,
failure-metadata-shape.json and failure-build-settings.json. No secret values
printed, new environment, retry or change to the failed job. A future independently
verified Akash feature publication is a new scoped code release, not a claim to
have diagnosed or corrected this preserved failure.

## Eclipse baseline continuation prepared and verified

Read-only preflight proves original Eclipse before equals acknowledged Akash
before, and live equals acknowledged Akash after. Exactly3changed keys are the
accepted Akash source/examples/admission. New Reconcile-HostedEclipseBaseline.py
uses existing strict publishers, validates all3original mutation ACK hashes and
the original accepted Akash cohort/source, and refuses any other drift.

Two controlled methods pass, including14negative mutation cases. Controls cover
original baseline/candidate divergence, altered Akash candidate, extra/removed/
DNA/permission live values, either source owner changed, paid tariff, missing/
wrong-hash/unverified/deployment-triggered acknowledgements. No network or real
fixture in tests. Existing source/native/console/local controls are not rerun.

Distinct fixed protected prefix eclipse-public-after-akash:

- before-variables.dpapi191254bytes; candidate-variables.dpapi193894bytes.
- lineage.json940bytes binds four original protected-file hashes and accepted
  Akash/source hashes. All original before/candidates/owners/qualifications remain.
- New Eclipse source and four preset strings equal the original tested candidate;
  only admission is recomposed with accepted Akash retained. DNA/unrelated data
  unchanged, free tariff. No native request, .NET build, apply or deployment yet.

Raw controls/preflight/public-only review in TestResults/EclipsePublicSourceCurrent:
reconciliation-controls.log, reconciliation-preflight.json, reconciled-review.json.
Next guarded exact105329a3fapplication through the existing mutation owner using
this distinct prefix, after lineage re-verification. Never overwrite/replay the
old prefix or retry the original failed code job. One subsequent activation and
one original hosted cohort will have independent durable journals.

## First Eclipse settings applied; one activation observed

Reconciliation controls2methods/14negative cases passed; log SHA256
d876ba5c350b616c0f9b2059b6145ecb04fe15d921230c0a4fa56b64afedd0e8.
New fixed prefix/source/lineage reverified before first apply. Clean exact code
checkout105329a3f/expected-release guard used; task branch restored afterward.
Shared mutation owner applied3values once, each original per-key ACK observed,
and final candidate equality verified. All4old protected input hashes and source
hash remain unchanged; Akash and unrelated settings retained. No native source
write, new owner, code publication or local rebuild.

HostedEclipseReleaseCurrent/after-akash-settings.intent/acknowledged.json binds
new protected input hashes and original lineage. One after-akash-activation.intent
and protected CLI receipt; exact activationfcaa8d3a-7964-46f7-9964-82696ee9f8d1
observedBUILDING for105329a3fc84e8505a6779d3291de806858c23c2. Observe the same job,
never resubmit because of a timeout. Compact latest observation:
TestResults/EclipsePublicSourceCurrent/after-akash-activation-observation.json.

No original Eclipse hosted cohort yet. Only after activationSUCCESS run the
normal visitor/native/Mongo/reuse/privacy/replica gate once. Prepared frontend
Eclipse card is unpublished until those checks pass; no browser proof claimed.
Current55hosted functional proofs;56stored query tariffs pending activation.
