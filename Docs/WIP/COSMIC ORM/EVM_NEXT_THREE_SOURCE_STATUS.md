# Base / Avalanche / BNB Chain public CQL milestone

31 October 2026. Three native adapters and published C# walkthroughs/CLI3/2 verified; HTTP/Mongo, hosted configuration and browser acceptance remain open. Work is limited to new demo scopes; existing green EVM code is unchanged and legacy classes were not edited.

| Provider | Native endpoint | Chain ID | Exact RPC path | Proof |
|---|---|---|---|---|
| BaseOASIS | https://mainnet.base.org/ |0x2105| / |18controls + native3/2 |
| AvalancheOASIS | https://api.avax.network/ext/bc/C/rpc |0xa86a| /ext/bc/C/rpc |18controls + native3/2 |
| BNBChainOASIS | https://bsc-dataseed.bnbchain.org/ |0x38| / |18controls + native3/2 |

All three use thin immutable descriptors over the existing unchanged EvmPublicAccountMetadataSource: fixed provider/chain/genesis/metadata/path, explicit anonymous config, one reported finalized canonical hash per selection, exact UInt256 balance strings, complete validation before visibility/paging, no source writes/signing/scans/implicit fallback or latest downgrade. Base/Avalanche genesis pins were observed at the official endpoint; BNB pin also matches the canonical BSC source. These checks do not independently verify consensus or L2 settlement.

Source classes: BasePublicAccountMetadataSource, AvalanchePublicAccountMetadataSource and BnbPublicAccountMetadataSource in their existing provider projects. Metadata keys: BaseAccountAddress, AvalancheAccountAddress and BNBChainAccountAddress. Native balances are source base units (BaseETH, AVAX, BNB respectively), not a shared currency or units-conversion promise. Public address-state projections are not stored COSMIC holons on-chain.

Controlled54tests/0skips cover point/membership/paging/budget, chain/genesis/hash/number/envelope failures, canonical block binding, malformed final records/Take(0), oversized/duplicate replies, cancellation/inactive/unknown keys, unsupported content scans/writes and forbidden credential configuration. Three actual CQL native tests pass/0skips, each returning3accounts and a2account subset with preserved native keys and one block hash per selection. Existing Ethereum CQL test host reused; only the three new classes selected. No green source tests rerun, shared implementation changed or new test environment created.

Persistent catalogues use the same existing schema and3public addresses, each with its own stable mapping owner under LocalState/COSMIC/HostedBasePublicSourceCurrent, HostedAvalanchePublicSourceCurrent and HostedBnbPublicSourceCurrent. Preserve these owners for stable mapped IDs. No wallet/node/account/contract/database installed or created, no gas or source writes. Existing SDK/cache/project references reused.

Next: readable C# walkthroughs via shared HolonManager/LINQ, native verbatim samples/actualCLI, then one HTTP+Mongo5/4/isolation gate per source with original operation journals and exact-owned cleanup. Batch the three catalogue/admission changes; strict source publishers/joint staging and browser links follow. Do not advertise them as hosted before those gates.

Reproduce protocol qualification with `Scripts/Sandbox/Test-EvmBreadthReadiness.ps1 -Network Base` (or Avalanche/BNBChain). Native CQL tests use the existing EthereumOASIS.CqlTests project and the matching explicit source env variables; do not replay other native fixtures.

Primary endpoints/protocol: [Base JSON-RPC endpoint (probed)](https://mainnet.base.org/), [Avalanche primary network](https://build.avax.network/docs/primary-network), [BNB official RPC endpoints](https://docs.bnbchain.org/bnb-smart-chain/developers/json_rpc/json-rpc-endpoint/), [canonical BSC genesis](https://github.com/bnb-chain/bsc/blob/master/params/config.go).

- `TestResults/EvmBreadthReadinessCurrent/Base.json`: 2548 B; SHA256 `604cf9584b69d15533225380f5f88f80b1a5ea35167c516c1c171b49c8693c44`.

- `TestResults/EvmBreadthReadinessCurrent/Avalanche.json`: 2557 B; SHA256 `0c17f3d1c366af5d2d68aedc35b00798e038338389b7e81f3eca1c5aa47facda`.

- `TestResults/EvmBreadthReadinessCurrent/BNBChain.json`: 2569 B; SHA256 `974da1224c7fdcfc8ba72863b92ef369d3d92d4c134d15f3ef151611f41881cb`.

- `TestResults/EvmBreadthReadinessCurrent/provider-controls.trx`: 84036 B; SHA256 `ee48ecf157e134a86916c4e74cd226d66108bf8ad81975216ee7ef9f90555eac`.

- `TestResults/EvmBreadthReadinessCurrent/provider-native.trx`: 6129 B; SHA256 `3d735cab83e9df279b1893b3e7996289d59ade88d6722327ece8d804d0131bb2`.

## Published C# walkthrough milestone

BasePublicWalkthrough, AvalanchePublicWalkthrough and BnbPublicWalkthrough now use the shared NativeKeySetWalkthrough→CosmicLinq→HolonManager pipeline. Spaced code/comments, exact native balances and reported finalized hashes; no independent consensus claim. Console and linked-source test builds pass. The3native tests execute the exact published walkthroughs and pass/0skips; actual --base-public/--avalanche-public/--bnb-public CLI commands each return3/2. No wallet, transaction, Mongo fixture, source identity reset or endpoint change.

Existing caches/projects reused; only the three new native walkthrough scopes selected, not a rerun of green source controls. [Maintained combined guide](../../Devs/EVM_PUBLIC_BALANCE_CQL_GUIDE.md) has commands and capability/units/consistency limits. Next: batch the three HTTP/admission integrations and native Mongo5/4/isolation gates with original intents/ACKs and exact-owned cleanup; then hosted presets/client links.

- `TestResults/EvmBreadthReadinessCurrent/console-native.trx`: 6129 B; SHA256 `5553d099c4bdd13099fa2a5c8bdafafc0a565e409e790b791dfa59e9da78d809`.

- `TestResults/EvmBreadthReadinessCurrent/console-base.log`: 1336 B; SHA256 `5d05fd0317ab7ac35a098ceff8e11673d9542b5da333beeede586414b5421632`.

- `TestResults/EvmBreadthReadinessCurrent/console-avalanche.log`: 1351 B; SHA256 `df3e54882db5d35283b5c6c780ebac78feadc6ca68d9fc9d1c319002d8af081e`.

- `TestResults/EvmBreadthReadinessCurrent/console-bnb.log`: 1359 B; SHA256 `edd67634086bad530f7c4617b9fb7dc789a67de81a4c1f535395f192018a223d`.

## HTTP registration/admission milestone
All three lazy public-source registrations and bounded no-order HTTP compiler profiles are implemented. Explicit read tariff catalogue now20; Core3 permission/signed-write-fence and API6 planning/discovery/admission tests pass,0skips. Evidence: TestResults/EvmBreadthReadinessCurrent/admission-{core,api}.trx. Full API build passed. These tests plan each native3/2 and paired Mongo preset and exercise source discovery; actual HTTP/Mongo data execution remains next, not yet verified. No hosted configuration applied.

HTTP native fixtures and exclusive BaseMongoHttp/AvalancheMongoHttp/BnbMongoHttp runner selections added. First Base selection rejected0 discovered tests in stale binary before any intent/DB creation; no data execution occurred. Rebuilding the existing test host before first real execution. Raw zero-test evidence retained in TestResults/BasePublicSourceCandidateCurrent until documented replacement; source catalogues unchanged.

## Native HTTP/Mongo acceptance — 31 October 2026
All three pass native3/2, private SDK-owned Mongo2, federation5/4, foreign-private0/public3 and9query receipts each. Every returned ID/provider/visibility is checked. Original intents and2ACKs per provider remain preserved; each exact-marker database was dropped and absence verified after consumers stopped. Controlled bearer/billing only; no hosted, production auth/billing or replication proof. Initial Base stale-binary0tests and fixture compile error preserved in raw logs; corrected build and actual3gates pass.
- `TestResults/BasePublicSourceCandidateCurrent/base-mongo-http.trx`: SHA256 `39bf3987aad38f958ff25151ae16df32e9fb32c1f35951cd25ab5ea1823f070f`;1pass/0skips. Persistent original intent/ACK/cleanup cohort `LocalState/COSMIC/BaseMongoHttpCurrent`: 837B.
- `TestResults/AvalanchePublicSourceCandidateCurrent/avalanche-mongo-http.trx`: SHA256 `53dce802edd194b165e4ab1a8a1507db3c5166b5fff6274b9f1f3eb3563f9759`;1pass/0skips. Persistent original intent/ACK/cleanup cohort `LocalState/COSMIC/AvalancheMongoHttpCurrent`: 847B.
- `TestResults/BnbPublicSourceCandidateCurrent/bnb-mongo-http.trx`: SHA256 `7144361cef690ae0cdb8159781955a77cfb1a0ae0dc6cbbce0aef047cf9db835`;1pass/0skips. Persistent original intent/ACK/cleanup cohort `LocalState/COSMIC/BnbMongoHttpCurrent`: 835B.

Next: source publishers/presets and sandbox local-demo links; matching hosted application rollout and live acceptance remain pending.

## Native HTTP/Mongo acceptance — 31 October 2026
All three pass native3/2,private SDK-owned Mongo2,federation5/4,foreign-private0/public3 and9receipts each. Returned IDs/provider/visibility checked. Original intents/ACKs retained; exact-marker DBs dropped and absence verified after consumers stopped. Controlled bearer/billing only; no hosted/replication proof. Initial stale-binary0tests and Base fixture compile error retained; corrected build and3actual gates pass.
- `TestResults/BasePublicSourceCandidateCurrent/base-mongo-http.trx`:1pass/0skips,SHA256 `39bf3987aad38f958ff25151ae16df32e9fb32c1f35951cd25ab5ea1823f070f`. Original intent/ACK cohort `LocalState/COSMIC/BaseMongoHttpCurrent` retained,837B.
- `TestResults/AvalanchePublicSourceCandidateCurrent/avalanche-mongo-http.trx`:1pass/0skips,SHA256 `53dce802edd194b165e4ab1a8a1507db3c5166b5fff6274b9f1f3eb3563f9759`. Original intent/ACK cohort `LocalState/COSMIC/AvalancheMongoHttpCurrent` retained,847B.
- `TestResults/BnbPublicSourceCandidateCurrent/bnb-mongo-http.trx`:1pass/0skips,SHA256 `7144361cef690ae0cdb8159781955a77cfb1a0ae0dc6cbbce0aef047cf9db835`. Original intent/ACK cohort `LocalState/COSMIC/BnbMongoHttpCurrent` retained,835B.

Next: hosted publishers/presets and sandbox demo links, then matching permitted rollout.

## Preset builders and website links — 31 October 2026
Strict Base/Avalanche/Bnb configuration builders each pass3controls; composed-candidate test also passes:10total. Each adds native3/2 and Mongo-paired presets, preserves DNA, rejects credentials/paid tariffs/conflicting enrollment and requires explicit encryption query-key policy. Composition preserves earlier seven preset groups. No host variables read/applied or protected candidate generated in this step. Do not independently apply candidates based on the same host snapshot: compose all ten against one reconciled snapshot before rollout.
Website task commit c09103b links the3spaced C# samples and guide, with hosted-pending labels;54client tests pass. Public main unchanged. Evidence: TestResults/EvmBreadthReadinessCurrent/{publisher-controls,website-links}.log. Next: actual ten-source protected candidate composition and shipping-planner preflight, then permitted matching hosted rollout/acceptance.

## Actual ten-source configuration preflight — 31 October 2026
Prepare-TenSourcePublicBatch.py composes Base/Avalanche/BNB into the preserved seven-source candidate. No host observation, mutation or deployment; original before snapshot remains inherited and must be reconciled before application. Protected ten-source-public-{before,candidate}-variables.dpapi are67,094/92,614B. Public-only review39,295B; actual40presets (20native/20Mongo-federation) pass shipping planner with real FromJson adapters/capabilities,1test/0skips. Sources remain inactive. Composition1test also passes; earlier green7source gate not rerun. Evidence: TestResults/HostedCqlIntegrationCurrent/ten-source-public-review.{json,trx,log},ten-source-composition.log. Next hosted reconciliation/application with matching release, then live HTTP/replica/isolation/browser gates.

## Independent next source: Optimism
Official [Superchain registry](https://github.com/ethereum-optimism/superchain-registry/blob/main/chainList.toml) lists mainnet.optimism.io and chain10. Explicit endpoint qualification passed chain0xa/genesis0x7ca38a1916c42007829c55e69d3e9a73265554b586a499015373241b8a3fa48b and finalized-hash-bound3/2balances. OptimismPublicAccountMetadataSource reuses unchanged EVM reader, fixed network/genesis/root path and OptimismAccountAddress key.18controlled plus1actual published C# sample pass/0skips. This is read-only public account projection, not owned native storage or independent settlement proof. No admission/HTTP/hosted configuration added, and the prepared ten-source batch remains unchanged. Console build and actual --optimism-public CLI3/2 pass. Next HTTP/Mongo integration; no ten-source rollout change. Evidence: TestResults/EvmBreadthReadinessCurrent/Optimism.json,optimism-source.trx.

### Optimism HTTP/Mongo milestone
1Core/2API/3configuration controls and1actual native HTTP gate pass/0skips. Native3/2,pairedMongo5/4,foreign-private0/public3,9receipts; IDs/provider/visibility checked. SDK saves2privatecopies; original intents/ACK retained, exact DBmarker and absenceverified after drop. Sources unchanged; noNeo/replication/billing/browser proof. Lazy CQL_OPTIMISM_PUBLIC_SOURCE_JSON registration and21type unsignedread tariff catalogue, not21hostedproviders. Strict4preset builder tested but not invoked against host. Ten-source protected candidate remains byte-for-byte unchanged. Next website recipeproof/links and future jointbatch. Evidence: TestResults/EvmBreadthReadinessCurrent/optimism-{core,api}.trx,optimism-configuration-controls.log; TestResults/OptimismPublicSourceCandidateCurrent/optimism-mongo-http.trx.

### Optimism client readiness
Website taske8df3c4 links the actual spaced C# walkthrough and guide with hosted-pending status. New focused preparationtest1passes0skips:3key preset unchanged,2key source read,2private Mongo-only saves with mappedIDs/provenance, repeated0new/2existing. Hosted acceptance runner now accepts Optimism through its existing journalled path; --help verified only, no session/source request. Raw TestResults/HostedCqlIntegrationCurrent/optimism-client-preparation.log. No publicmain/hosted change; ten-source candidate remains unchanged.

Optimism optional jointbatch prepared separately:11profiles/44presets, exact4newOptimism presets pass shipping planner1/0skips.3scope controls pass. Ten-source candidate retained unchanged; nohostapplication/deployment. See integrationplan optionalextension for commands/releasegates.

## Next independent profiles: Linea and Scroll
Official endpoints: [Linea connect](https://docs.linea.build/network/build/connect), [Scroll configuration](https://docs.scroll.io/en/developers). Read-only qualification passes chain/genesis/finalized-hash-bound3/2balances. New fixed Linea0xe708/genesis0xb6762a65689107b2326364aefc18f94cda413209fab35c00d4af51eaa20ffbc6 and Scroll0x82750/genesis0xbbc05efd412b7cd47a2ed0e5ddfcf87af251e414ea4c801d78b6784513180a80 descriptors reuse unchangedEVMreader; keys LineaAccountAddress/ScrollAccountAddress.36controlled+2realpublishedC#sampletests pass/0skips. Console build and actual --linea-public/--scroll-public CLI3/2 pass. Next HTTP/Mongo. No HTTP/admission/hostedconfig yet; existing10/11releasecandidates unchanged.

Rootstock official [endpoint](https://dev.rootstock.io/dev-tools/wallets/metamask/) passed chain0x1e/genesis0xf88529d4ab262c0f4d042e9d8d3f2472848eaafe1a9b7213f57617eb40a9f9e0 then finalized-headerrequestHTTP400. No source/profile/identity created; requires separately designed native consistency semantics or compatible explicit endpoint, no latest downgrade. Evidence: TestResults/EvmBreadthReadinessCurrent/{Rootstock,Linea,Scroll}.json,linea-scroll-source.trx.

### Linea/Scroll HTTP and client milestone
2Core/4API/6configuration/2actualnativeHTTPtests pass0skips. Both native3/2,paired5/4,foreign-private0/public3 and9receipts; mappedIDs/provider/visibility checked. Each SDKcreates2privateMongo records; originalintent/ACKs retained, exactmarkerDBdrop/absence verified,servicesstopped. New2clientpreparationcontracts also pass:Mongo-onlyprivate2saves/repeat0new2existing. API has explicitprojectreferences/lazy CQL_LINEA_PUBLIC_SOURCE_JSON/CQL_SCROLL_PUBLIC_SOURCE_JSON; unsignedreadcatalogue23types, nothostedcount. Strict4presetbuilders each tested only, nohostcall/staging. Existing10/11protectedcandidates unchanged. Evidence: TestResults/EvmBreadthReadinessCurrent/linea-scroll-{core,api}.trx,linea-scroll-configuration.log; TestResults/{Linea,Scroll}PublicSourceCandidateCurrent/*-mongo-http.trx; HostedCqlIntegrationCurrent/linea-scroll-client-preparation.log. Hosted/billing/replica/browser proof remains open.

### Thirteen-source preparedbatch
Optional13profiles/52presets prepared separately; actual8newLinea/Scroll presets pass shippingplanner1/0skips,4application-scope controls pass. Initialbuilder/test copied endpoint mismatch was found by actualretainedcatalogue preparation and corrected;6configuration controls rerun pass.10/11candidates preserved,nohostapplication. Evidence: TestResults/HostedCqlIntegrationCurrent/{linea-scroll-prepared-review.trx,thirteen-source-application-controls.log,linea-scroll-corrected-configuration.log,thirteen-source-public-review.json}. Nextmatchingpermittedrollout and liveacceptance, not moregreenhardening.

## Next independent sources: zkSync and Abstract
Existing providerprojects now have explicit sharedEVMreader publicbalance representations. Qualification passes zkSync0x144 at mainnet.era.zksync.io ([network](https://docs.zksync.io/zksync-network/zksync-era/network-details)) and Abstract0xab5 at observed api.mainnet.abs.xyz. Both report genesis0xe8e77626586f73b955364c7b4bbf0bb7f7685ebd40e852b164633a4acbd3244c; descriptors independently enforce provider/chain/key, not genesis alone.36controlled+2actualpublishedC#sampletests pass/0skips, native3/2each. Keys ZkSyncAccountAddress/AbstractAccountAddress; exactwei strings, oneRPCreportedfinalizedhash per selection, not independentconsensusproof. Consolebuild and actual --zksync-public/--abstract-public CLI3/2 pass; HTTP/admission/hosted notyetimplemented. Existing13sourcecandidate unchanged. Raw TestResults/EvmBreadthReadinessCurrent/{ZkSync,Abstract}.json,zksync-abstract-source.trx.

### zkSync/Abstract HTTP and client milestone
2Core/4API/6configuration/2actualnativeHTTP and2clientpreparationtests pass0skips. Actualretainedcatalogues also accepted by strictbuilders, no mirroredendpoint mismatch. Each native3/2,paired5/4,foreignprivate0/public3 and9queryreceipts, exactIDs/provider/visibility checked. SDKcreates2privateMongo copies each; originalintents/ACKs retained,exactDBmarker/drop/absenceverified,servicesstopped. Lazy CQL_ZKSYNC_PUBLIC_SOURCE_JSON/CQL_ABSTRACT_PUBLIC_SOURCE_JSON registration, explicitexistingprojectrefs,25typeunsignedreadcatalogue (not25hosted). Strict4presetbuilders testedonly; existing13sourcecandidate unchanged. Evidence: TestResults/EvmBreadthReadinessCurrent/zksync-abstract-{core,api}.trx,zksync-abstract-{configuration,retained-config}.log; TestResults/{ZkSync,Abstract}PublicSourceCandidateCurrent/*-mongo-http.trx; HostedCqlIntegrationCurrent/zksync-abstract-client-preparation.log. Hosted/billing/replica/browser proof remainsopen.

### Fifteen-source preparedbatch
Optional15profiles/60presets stagedseparately. Actual8newzkSync/Abstractpresets pass shippingplanner1test/0skips;5scopecontrolspass. Freshhostreadonlyreconciliation exactsnapshot,0drift/17plannedsettings. Previouscandidatespreserved,nohostapplication/deployment. Nextmatchingpermittedrollout and hosted/browseracceptance.

## Next source: Monad; Sei parsing gap
Selected rpc.monad.xyz chain0x8f/genesis0x0c47353304f22b1c15706367d739b850cda80b5c87bbc335014fef3d88deaac9 passes readonlyqualification. [Monad network information](https://docs.monad.xyz/developer-essentials/network-information). MonadPublicAccountMetadataSource reusesunchangedEVMreader,keyMonadAccountAddress,fixedrootpath.18controlled+1actualpublishedC#sampletestpass0skips,native3/2. Consolebuild andactual --monad-public CLI3/2pass;noHTTP/admission/hostedprofile yet.

Sei evm-rpc.sei-apis.com chain0x531 passedidentity but qualificationrejects genesisformat. Separateboundedblockzero observation returns hash0xF9D3845DF25B43B1C6926F3CEDA6845C17F5624E12212FD8847D0BA01DA1AB9E,number0x0:uppercasedatahash is rejected by currentstrictlowercaseparser. This does not prove missingchain/finality; investigate EthereumDATAencoding normalization and finalizedhashboundreads beforeprofileenrollment. NoSeicatalogue/identitycreated. [Sei native config](https://docs.sei.io/evm/sei-global-wallet). Evidence: TestResults/EvmBreadthReadinessCurrent/{Monad,Sei}.json,Sei-genesis-response.json,monad-source.trx.

### Monad HTTP/client milestone
1Core/2API/3config/1actualnativeHTTP/1client tests pass0skips. Actualretainedsource accepted bystrictbuilder. Native3/2,paired5/4,foreignprivate0/public3,9receipts; exactIDs/provider/visibilitychecked. SDK2privateMongocopies; originalintent/ACK retained,exactDBdrop/absenceverified,servicesstopped. Lazy CQL_MONAD_PUBLIC_SOURCE_JSON,explicitexistingprojectref,26typeunsignedreadcatalogue(not26hosted). Builder4presetstestedonly;15sourceprotectedcandidateunchanged. Evidence: TestResults/EvmBreadthReadinessCurrent/monad-{core,api}.trx,monad-{configuration,retained-config}.log; MonadPublicSourceCandidateCurrent/monad-mongo-http.trx; HostedCqlIntegrationCurrent/monad-client-preparation.log. No hosted/billing/replica/browserproof.

### Sixteen-source preparedbatch
Optional16profiles/64presets preparedseparately; actual4newMonadpresets passshippingplanner1/0skips,6application-scopecontrolspass. Priorcandidates preserved; nohostobservation/application/deployment. Futurematchingrelease/hostedacceptance remainsopen, existingapprovedroutingimage stillBUILDING.

Sei investigation update:currentEthereumexecution-API bytes32 schema requireslowercasehex,so standardreaderstrictness is intentional. ExplicitSeinativewireprobe accepts onlyexact32bytehex thennormalizescase; readonlyfinalized/hashbound3/2qualificationpasses. OriginalstrictfailurepreservedasSei-strict-canonical-failure.json; noSei shippingprofile/sharedreaderchange. Nativecasecodec stillrequired beforeenrollment.
