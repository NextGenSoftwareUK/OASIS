# Blockscout public address CQL — native qualification

2 November 2026. Native and local CQL/C# verified; HTTP/hosted/card remain red.
Counts57integrated/52hosted/5browser47open. Original read timeout cause unresolved.

## Contract and native evidence

Existing provider default is https://eth.blockscout.com/api/v2/ with optional
API key. Legacy adapter remains unchanged. Its null balance default must not be
copied into a new read profile: unknown balance is not zero or indexed-account
existence. Official current source explicitly returns200for valid unindexed
addresses as well. The public explorer documentation URL /api-docs returned404
in web inspection; that was not a native address API failure.

[Official address controller](https://github.com/blockscout/blockscout/blob/master/apps/block_scout_web/lib/block_scout_web/controllers/api/v2/address_controller.ex)
· [Official documentation index](https://docs.blockscout.com/llms.txt).
Current Pro endpoints use a separate authenticated interface; no Pro account,
key, payment or endpoint substitution was used for this qualification.

One exclusive native qualification used the production guarded .NET transport,
no authorization/cookies/user-agent override and three bounded anonymous GETs.
All returned200with matching address hashes, boolean contract flags and exact
string balances. Original observations include an individually reported balance
update block number; the three records are not a shared finalized snapshot.
Six controlled cases cover exact/nullable balance, wrong address, numeric and
noncanonical balance, and missing identity. One explicit native test passes.
Native tests skip without their explicit environment gate; skipped tests are not
native proof. No source writes/accounts/transactions were created.

Original addresses:

- 0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2
- 0x6b175474e89094c44da98b954eedeac495271d0f
- 0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48

Proving test: OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/BlockscoutPublicNativeQualificationTests.cs.
Intent/outcome: LocalState/COSMIC/BlockscoutPublicQualificationCurrent.
Preserve original responses and consumed intent; no replay/requalification.

## Next red scope after Celestia publication

Implement an explicit query-only explorer-address source with equality/membership
and paging over the original three addresses. Retain exact balances as strings,
model nulls as absence, preserve per-record update heights and indexed names,
and exclude private/watchlist/tag fields from holon projections. No independent
consensus/finality or canonical provider writes are established by this data.
Require capability/error/precision/budget controls, actual C# walkthrough,
HTTP/Mongo/privacy/replicas and original hosted acceptance before a public card.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `LocalState/COSMIC/BlockscoutPublicQualificationCurrent/qualification.intent.json` | 332 | `53bac5cca7e8985a5ed26e4a433dfe2157e09b514ff238fe4c698cd02621b314` |
| `LocalState/COSMIC/BlockscoutPublicQualificationCurrent/qualification.observed.json` | 618 | `95033c17eda549a0f4d1265c712116e33a23abe8e9592beb0fefb5b780481f3f` |
| `TestResults/BlockscoutPublicSourceCurrent/qualification-controls.trx` | 12113 | `138b9d390e65c18514204588624438b24476b041d3db8b1129cf493a5f07b47c` |
| `TestResults/BlockscoutPublicSourceCurrent/native-qualification.trx` | 3351 | `f8e0bb3c054565cb3e3b725f6623b3a0b7e67db18bd3cf2d6224fce02dc92dfb` |
| `TestResults/BlockscoutPublicSourceCurrent/address-0.json` | 1169 | `2283dd252446126841e786cf07185cae34ce8fa9715cfc396a8ba5acf40f6dff` |
| `TestResults/BlockscoutPublicSourceCurrent/address-1.json` | 1144 | `1971218ecb5f6ee19be260c4ceac7fa7212de7bdef26b2e88925d571ea8f7ac1` |
| `TestResults/BlockscoutPublicSourceCurrent/address-2.json` | 1283 | `dad217c59852abdbf34cf49376acd83ce1402f61b631b14c808f375072b7fe72` |


## Local CQL/C# milestone verified

BlockscoutPublicAddressMetadataSource implements shared CQL/read-only activation
contracts, key BlockscoutAddress, at most8canonical addresses.42source controls
pass: equality/membership/paging; exact78-digit balances/uint64update blocks; null
absence; malformed identity/quantity/name/RPC fields; cancellation/budgets/unknown
keys/inactive source; write refusal; exclusion of private/watchlist/user/token
fields; and same-owner cross-explorer ID separation. Complete selection precedes
paging; late failure yields no partial rows. Legacy adapter unchanged.

Actual spaced/commented C# manager/deferred-LINQ demo returns3full/2filtered.
One original owner/candidate/published source/intent/ACK retained below
LocalState/COSMIC/HostedBlockscoutPublicSourceCurrent. Do not recreate the owner
or replay qualification/completion. Build passes/0errors.

### Preserved failures and explicit completion

Initial console preflight stopped before state/native requests: PowerShell array
Address method shadowed member-enumerated address. Fixed explicit per-record
property selection; preflight report retained. Original console then failed
activation on a15-second HttpClient read timeout while awaiting HTTP response.
No CQL rows/source writes completed. Native connection guard/refusal and parsing
were not the observed failure; endpoint/transport cause remains unresolved.

One separate evidence-bound read-only completion reused the exact candidate hash,
owner,3addresses, endpoint, binary and unchanged timeouts; original failure log
hash/exit/intent preserved. No source writes, retries loop, new source owner,
endpoint switch or deadline extension. Completed CLI3/2; immutable ACK explicitly
marks originalConsoleFailurePreserved/readOnlyCompletion/reliabilityCertifiedfalse.
Functional success does not certify endpoint reliability.

[Configuration, spaced C# usage and metadata guide](../../Devs/BLOCKSCOUT_PUBLIC_ADDRESS_CQL_GUIDE.md).
Next HTTP compiler/read activation/query-only admission/presets, local native3/2+
Mongo5/4/privacy, then original hosted/replica acceptance/card. Hosted stays52.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `TestResults/BlockscoutPublicSourceCurrent/source-controls.trx` | 71401 | `77bcc53241ce5e07e33496631e4d7425efce9aa70f99732cff348906e58ee148` |
| `TestResults/BlockscoutPublicSourceCurrent/console-preflight-initial.json` | 286 | `8a473d0c4d2d5501df83b38ddf0f3bca261c4a62750d9437d5514b2c61585e7c` |
| `TestResults/BlockscoutPublicSourceCurrent/console-qualification.log` | 4532 | `a34fd2e49ff04aa552a113d18f16a24adf24a91168f432ad223e3ba257246a30` |
| `TestResults/BlockscoutPublicSourceCurrent/console-readonly-completion.log` | 1552 | `052ef207f618ef8a8f891bcc3d2ea3d9c053ce8a567032d55507de7210bbd676` |
| `LocalState/COSMIC/HostedBlockscoutPublicSourceCurrent/source.json` | 260 | `203a5934576a702390d8847abd97d59e01f7615b3ebeff4baf3903b3c32e4bf2` |
| `LocalState/COSMIC/HostedBlockscoutPublicSourceCurrent/native-qualified.acknowledged.json` | 465 | `1c268e60fb982161358345a08c5af122098c6f048169e21f04f649c6617579a4` |
| `Providers/Network/NextGenSoftware.OASIS.API.Providers.BlockscoutOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.BlockscoutOASIS.dll` | 72704 | `aa14de696c2eaf5f9f63f7baf0192834dedde1f99632511a028aef34bbf7dea9` |
| `Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll` | 917504 | `1ceaccc1f7c0f4cc018eaaad9235486e6e8b3efdf123c92f2feabf70cce42725` |


## Local HTTP/Mongo milestone verified

WEB4 build0errors, Core1/API3/publisher9/actual prepared presets1/client108
controls pass. Local nativeHTTP1test passes: native3/2, Mongo5/4, foreign-private0/
public3,9query receipts. Two SDK-owned version1 copies seeded through manager;
controlled bearer/billing fixtures are not production billing or replication proof.
Exact owned DB removed after marker/count verification, owned Mongo stopped and
27029absent. Original fixture intent/save ACKs/cleanup below
LocalState/COSMIC/BlockscoutMongoHttpCurrent retained; no replay/resave.

Core task55de8e9f735346aec00ddd0b2cbdfcaadbebbb80 pushed/remote verified;
parent gitlink/manifest advanced together. Protected three-key candidate prepared
once below HostedSourceRolloutCurrent/blockscout-public-*; four presets reviewed
and planned with shipping capabilities. No live application/deployment yet. Next
commit/integrate tested batch, exact code SUCCESS, one application/activation,
original hosted cohort and card. Original console timeout cause still unresolved;
functional local success is not reliability certification. Hosted count stays52.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `TestResults/BlockscoutPublicSourceCurrent/mongo-http-native.trx` | 3383 | `e3a13b0c23e362dc7fb9307680464f19ca213e1b26090a8648a49bb2cb885481` |
| `TestResults/BlockscoutPublicSourceCurrent/blockscout-mongo-native-summary.json` | 233 | `4a0321d03db212c8c213fa861e231bfce2c41a39ed2799855251c602f899c321` |
| `TestResults/BlockscoutPublicSourceCurrent/Http/core-tariff.trx` | 3348 | `178d525d7543b39d857aaf4693cbb1ec116b61b855c020ea092986dac2d66e4c` |
| `TestResults/BlockscoutPublicSourceCurrent/Http/api-admission.trx` | 6655 | `b08cbf5362ca3ba04a80228da44694ccffcf2d0853549606a74bce9fa9ddb3d6` |
| `TestResults/BlockscoutPublicSourceCurrent/Http/prepared-presets.trx` | 3344 | `0e6dd742d60504149257da219cee0786e5eeeafdf21e9d1bc8a4f548156f648d` |
| `TestResults/BlockscoutPublicSourceCurrent/prepared-review.json` | 4299 | `412c9e68680bc9aa0cc9889b93a6be3829d75b48e2a5ff44fff2b93cc18ced93` |
| `TestResults/BlockscoutPublicSourceCurrent/client-all-tests.log` | 10478 | `a4e4d45f7b4e95832365e7360d38b5c81f14b2ce409e73f211786558a9f17d02` |

Development publication: parent669917da125c92880289c3cf6729dbe878b8819d and
Core55de8e9f735346aec00ddd0b2cbdfcaadbebbb80 remote refs verified after ordinary
fast-forward pushes. Manifest/gitlinks gate passes. Exact code job
effaffdf-9626-4301-8a9f-efeda25ed97d last observed BUILDING; follow same job,
no restart. Candidate remains unapplied. Durable release intent/observation below
LocalState/COSMIC/HostedBlockscoutReleaseCurrent; hosted stays52.

Codeeffaffdf SUCCESS at exact669917da1. Applied three reviewed values once
with skip-deploys, full candidate/per-key hashes observed exactly. Activation
948c5921-e8cd-42d4-9349-4c5284a3672c last verified BUILDING at same commit.
Follow existing job; no reapply/redeploy/reprepare. Original hosted cohort has
not started; durable release/application ACKs and intents retained under
HostedBlockscoutReleaseCurrent/HostedSourceRolloutCurrent.


## Hosted startup failure and tested root correction

Original code/settings deployments succeeded, but optional source activation
returned upstream HTTP500. Safe event4831: activation/unclassified/other-http/500.
Discovery explicitly reports CQL_SOURCE_UNAVAILABLE. Original acceptance stopped
before native queries/private saves; one original owner session retained.
No card/hosted proof, source owner/config unchanged; no restart to get green.
Durable original native-failure.observed.json ties owner/source/failure/diagnostic
hashes and zero-save proof; do not replace the owner.

Root gap: one failed startup health read permanently latched the source unavailable.
Added explicit per-profile activation policy; only Blockscout opts in. At most
2awaited attempts/one5s delay inside the same30s deadline, typedHTTP500/502/503/504
only. Other profiles remain1attempt. No normal-query/write/signing/auth/rate-limit/
malformed-response/timeout retries; no endpoint switch. Failed attempts stay
observable through safe event4831 plus typed recovery event4832. Exhaustion stays
unavailable, stop cancellation prevents the next native operation.

18recovery/profile checks pass; final DI-backed recovery case rerun1/1 proves
actual registry construction/logging and delayed readiness. Initial new test
logger-class naming compile failure corrected; original log retained. No native
fixture replay. Next publish correction with existing settings unchanged, observe
exact new code SUCCESS, then same-owner discovery readiness and one explicit
--resume-unstarted-native continuation only if ready. Never reapply settings or
resubmit the original failed deployment. Hosted52/configured tariffs53.

Owned3,304,884byte OS crash dump dotnet.exe.6604.dmp was identified by the
original .NET Runtime stack/time; cleanup was rejected by automatic approval
review (blocked by policy). Dump preserved/inventoried; no alternate deletion
attempt or David chore. This does not block CQL work.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `TestResults/BlockscoutPublicSourceCurrent/hosted-acceptance.log` | 422 | `5e2c905b6dcacbcce01524254c54b797a1488d3a80e5dc7340e178979c3d7c5f` |
| `TestResults/BlockscoutPublicSourceCurrent/discovery-after-unready.json` | 265 | `74a3dc2d18399021bf94f11e57c4771a0398296c09c2ff0e0c297e26e1a48e60` |
| `TestResults/BlockscoutPublicSourceCurrent/hosted-activation-diagnostic.json` | 485 | `9e25fb8c801e457c461c138a1d25a6c834c676121179e4d42b9e3829c2bb35f5` |
| `LocalState/COSMIC/HostedQueryAcceptance-BlockscoutOASIS/native-failure.observed.json` | 733 | `fecdc22e8a43681b1efa8141ffb6b5e624320fe7d912c3b7e4b5c9419682ca29` |
| `TestResults/BlockscoutPublicSourceCurrent/Http/activation-recovery.trx` | 31405 | `f7b196f318b31add0ec9e8e3ef172688101cf5507cb37bd4b46db984c1112ae7` |
| `TestResults/BlockscoutPublicSourceCurrent/Http/activation-recovery-di.trx` | 3386 | `8a5229f5aef53ef8b91bc001ed158bdf562d07c4afa6e6a36db4c8481167efc0` |
| `TestResults/BlockscoutPublicSourceCurrent/owned-crash-preserved.json` | 590 | `d98ac65c30bdde3c14f1c4ffa5fc538ad6e8656aadda0e1767582e54ce5c241c` |

Correction21666342fb90599ad74f3d3825acf253d7e5d9d0 pushed to Development;
Core55de8e9 and all live variables unchanged/equal original reviewed candidate.
Exact correction job97b13d99-4192-45e8-b45e-47c5411d6bca last observed BUILDING.
Journal activation-recovery-code-publication.{intent,observed}.json below
HostedBlockscoutReleaseCurrent; original failed release/cohort preserved.
Follow same job; no restart/settings reapplication/new owner.


## Hosted functional HTTP acceptance complete

Corrected code21666342f/job97b13d99 SUCCESS, Core55de8e9 and all settings unchanged.
Original owner discovery ready/four presets; explicit --resume-unstarted-native
continued the same zero-save cohort, not a replacement avatar. Native3/2, Mongo5/4,
reuse0new/2existing, foreign-private0/public3 and exact SQLite/Neo4j2/2 pass.
Only two original normal API version1 saves/operation ACKs, pending0. Independent
source-owner/key/nullability/field-allowlist/privacy/save-operation/exact-replica
audit wrote immutable acceptance ACK under HostedQueryAcceptance-BlockscoutOASIS.
Original startup500/console timeout/source config/owner/failure evidence preserved.

Recovery policy has18controlled/profile passes plus final production-DI1/1.
Corrected boot had no matching retry event in bounded operator observation;
live readiness is not live induced-failure recovery or reliability proof.
Hosted functional53/query tariffs53, browser5verified/48open. Public card next.
No native writes, independent consensus, full payload copying or full ORM claim.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `TestResults\HostedCqlIntegrationCurrent\BlockscoutOASIS-native-http.json` | 15318 | `4a74ac8d3ec4f15a6ecafeb73c322b2270a6ac2cc1758b9ff6ffee3f1ed2d3c8` |
| `TestResults\HostedCqlIntegrationCurrent\BlockscoutOASIS-client-http-flow.json` | 39982 | `6ac3d0ec2f5c6c6a9e70d77fe5f71b09bb6dfab2eae27e25a1658a15c4d702ef` |
| `TestResults\HostedCqlIntegrationCurrent\BlockscoutOASIS-isolation-http.json` | 6108 | `5f3b8658f5a166c8636329ff28af751abace3266e06ab63d7560f43f28bc2073` |


## Public sandbox publication verified

Website task/main2983b1cceec7aed26b2fb52c1995f3dc136daaae verified remotely.
Blockscout card includes native/filter/Mongo instructions, exact nullable-balance/
per-record-block/privacy/trust limits and pinned guide+C#links. HTML parses/IDs
unique,108client controls retained/no new JS change. Three anonymous public GETs
match exact git blobs; observation TestResults/BlockscoutPublicSourceCurrent/
public-assets-observation-1.json, immutable site ACK under HostedBlockscoutReleaseCurrent.
Original source/cohort/failures/save ACKs frozen; no replay. Hosted53/browser5/open48.
Browser interaction/full native payload copying/reliability remain unverified.

| Public asset | Bytes | SHA256 |
|---|---|---|
| `web4-sandbox.html` | 187960 | `8b151b5cf9d1a454d40bfb22e239c802fdec87265b6712a97b6d263f78a41a53` |
| `cql-demo-preparation.js` | 10241 | `c1006abf6074616428f93e83e13774ba8a6198f35d8ed097d84323466a8b309a` |
| `cql-playground.js` | 21871 | `7cb6210b9adda1e83630fc966926772f8338c43163c24d1abd88c626ad1ad649` |

11 November UTC: Blockscout native public explorer address presets rendered 3 full and 2 filtered rows in the live Development WEB4 CQL Playground. The original-owner private paired browser check remains open.
