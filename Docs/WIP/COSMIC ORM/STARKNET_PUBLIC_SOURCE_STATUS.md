# Starknet public source qualification

Updated 1 November 2026. Native CQL/manager/CLI, API/local HTTP and hosted native/federation/reuse/privacy/replicas verified. Card published; browser interaction remains open. Earlier failures/research below are historical.

## Observed result

The operator advertises `https://starknet-rpc.publicnode.com` as its Mainnet RPC. One anonymous read-only `starknet_specVersion` request returned HTTP403 before chain identity, accepted-block discovery or any header read. No automatic retry/alternate endpoint, API key, SDK install, wallet/account or sourceOwner/catalogue creation. The reason for403was not established; do not infer an archive requirement, paid-access requirement or a broken canonical SDK from this observation.

Raw `TestResults/StarknetPublicSourceCandidateCurrent/shape-readiness.json`, 168bytes, SHA256 `2e842dafa732cf37e4b6e06dcec295c17cad0a89c62dc90f14aa97ae5baaa944`. It records failed qualification, HTTP403, zero records and zero mutations. No native CQL tests or browser proof were run.

Existing Starknet provider uses the same advertised Mainnet default, but its signed SDK paths were not executed in this probe. Preserve this scope distinction in the full provider report.

## Next safe work

Continue the exact Elrond deployment/hosted gates first. Before another Starknet acceptance attempt, qualify a currently documented suitable free endpoint/explicit access contract; do not loop on the same403or install a node/runtime. A future source needs exact JSON-RPC envelope/chain checks, bounded complete native key selections, correct field-element/numeric domains, explicit API-reported block status, and tests before any CQL/hosted count grant. Do not imply independent settlement, signature or consensus verification.

This is agent-owned research; no new David action/account request. Browser bridge reset also remains Transport closed; no Windows control resumed.

Primary contracts: [operator endpoint listing](https://starknet.publicnode.com/), [official Starknet OpenRPC specification](https://github.com/starkware-libs/starknet-specs/blob/master/api/starknet_api_openrpc.json).

Denial diagnostic: HTTP403, servercloudflare, JSON response, cloudflare/access-denied body indicators, no retry-after/proxy-block-reason header. Policy cause remains unverified. Raw denial-diagnostic.json 574bytes, SHA256 `ca81564db2db3d33c317cfd20f9196abbaeeefccfb39adbe643ebd0cb155bbbe`. No acceptance retry or endpoint/config migration.

## Intended transport reaches Mainnet

Existing deployed Node24and local Node24returned HTTP200for specVersion/chainId; local .NET10using actual Coref41b2a1 NativeConnectionGuard handler also returned200. RPC reports version **0.10.3**, chain ID **0x534e5f4d41494e (SN_MAIN)**. No endpoint fallback, guard suppression, credentials, new runtime or network write. Earlier Pythonurllib403remains preserved; cause is client-specific/undetermined, not proved to be a service-wide denial.

Evidence/hashes are shared with [Radix transport qualification](RADIX_PUBLIC_SOURCE_STATUS.md#guarded-native-transport-and-entity-contract-qualified). No Starknet header catalogue or three/two-record CQL selection is proven yet. Next qualify the current RPC's bounded immutable/header selection and exact FELT/numeric/status contract before implementation. Prioritize the now-qualified Radix native3/2resource contract first.

## Guarded native header contract qualified â€” 1 November 2026

Exact guarded .NET client read spec/chain/genesis, explicitly selected blocks1/2/3 and subset1/2. All8requests returned200; response-reported version0.10.3/SN_MAIN, statusACCEPTED_ON_L1. Genesis hash `0x47c3637b57c2b079b93c61539950c17e868a28f46cdef28f88521067f21e943`; selected parent links and repeated result objects match (RPC envelope IDs differ). No chain scan/fallback/SDK execution/sourceOwner allocation/writes. Integer/field elements checked without float conversion; endpoint observations do not independently verify consensus/finality.

`TestResults/StarknetPublicSourceCandidateCurrent/header-readiness.json` SHA256 `cc12910444c7f12efad09641a966147792ac7bc760c754f86d8eb09c5fe9e19a`; header-validation.json records3/2and exact checks. Next native adapter/tests must preserve FELT/unsigned integer domains, explicit identity/status, complete pages/cancellation before CQL/API admission/count increase. [Version-pinned official contract](https://github.com/starkware-libs/starknet-specs/blob/v0.10.3/api/starknet_api_openrpc.json). Earlier Python403retained.


## Native CQL adapter verified — 1 November 2026

Task-only StarknetPublicHeaderMetadataSource now passes31controlled tests (zero skips) and one actual native CQL test selecting3then2headers. Fixed Mainnet/RPC/genesis, exact FELT/UInt64 strings, requested hash, reported ACCEPTED_ON_L1 and complete-page/cancellation/error boundaries are exercised. No API admission, tariff, hosted configuration or browser grant. Existing37admitted/locally scoped query-type headline excludes this new candidate until API/demo admission is verified.

- Controls: TestResults/StarknetPublicSourceCandidateCurrent/header-cql-controls.trx SHA2566a8f748cc6079137da8d495379258010d6cee194cd797a231da9d7e95cf344fd.
- Native3/2: header-cql-native.trx SHA2568ecf2f5b50d469f303f8381d7838808c5198a9477ad1c57491468883dd8fc67c.
- Stable anonymous catalogue LocalState/COSMIC/HostedStarknetPublicSourceCurrent/source.json,466bytes; reuse original sourceOwner. No wallet, key, new service, source transaction or hosted visitor created.
- Actual spaced/commented C# walkthrough passes3/2through shared HolonManager/LINQ; build0errors. Next API admission/local HTTP/federation before hosted rollout. Red TRON work remains excluded from verified releases.
- [Setup and usage guide](../../Devs/STARKNET_PUBLIC_HEADER_CQL_GUIDE.md). Field domain follows the official Cairo reference linked there; UInt64 is an explicit bounded profile choice, not a claim that OpenRPC defines all integers this way.

Actual CLI evidence: TestResults/StarknetPublicSourceCandidateCurrent/cli-native.log SHA2560fdf6b017020fd778a499a13d8853a8310343b64ecae8a944a66dfa74901f879. Compact native-cql-milestone.json records all counts and evidence hashes. No source writes, Mongo copies or hosted visitor creation.

API candidate wiring and four-preset publisher controls are task-only. Publisher3/3passes. Initial API tests: schema1pass/admission2fail because Core StorageQueryTariff.SupportedQueryProviders has not admitted Starknet. This is an expected incomplete enrollment gate, not native query failure; no hosted grant or count increase. Finish the reproduced logout ownership root fix first, then explicitly add/test read-only Core admission and local HTTP/Mongo proof.


## API/local HTTP and prepared candidate verified

Core8a5ab453ee6734195331abeec0ad5c04e84b5332 extends the explicit unsigned-read catalogue only; independent permission/tariff still required. API/schema3/3and actual prepared-candidate1/1pass, zero skips. Publisher3/3passes. Actual local HTTP source3/2, private Mongo federation5/4and visitor isolation0private/3public pass; two SDK-owned copies,9controlled billing receipts. Owned DB deleted only after marker/nonce/count verification; fixture.cleaned.json and port27029absence confirmed. No production billing, replication or browser proof inferred.

38implemented types now include this locally verified candidate; deployed tariffs35/hosted35/browser5remain unchanged. Three protected settings/four presets prepared without any host mutation. Preserve sourceOwner, fixture journals and protected snapshots. Next exact selected release, then settings apply/one deployment/hosted acceptance after the session lifecycle correction.

- TestResults/StarknetPublicSourceCandidateCurrent/api-admission-final.trx:6568bytes, SHA25626f59165de4f59747ceb5c76c2094bd7bb20fa73be7ed62c95bae8585347bf70.
- TestResults/StarknetPublicSourceCandidateCurrent/starknet-mongo-http.trx:3371bytes, SHA2565534d70eecde7d0d4a3bd821274817aa1035e1d4ff22478e8643e254de1a2cf0.
- TestResults/StarknetPublicSourceCandidateCurrent/actual-prepared-candidate.trx:3338bytes, SHA2566b66a0f133baac3838cf5b56f16e6aea8bf9d2ccce976b29b37a2c9f4a7229e0.
- TestResults/StarknetPublicSourceCandidateCurrent/starknet-mongo-native-summary.json:229bytes, SHA2566cabf9ca67d44ad32fc5fc1d496db1a67460749a3f6a532a3337a381f6e9f6bd.

Selected release274ccf42eb91e1de518c299f93ce3d6155b56dcd/Core8a5ab453ee6734195331abeec0ad5c04e84b5332 passes4/4shipping API/schema/actual-preset tests,0skips. Actual DLL metadata contains StarknetPublicHeaderMetadataSource and excludes unqualified TronPublicHeaderMetadataSource. selected-release.trx SHA256fed6648fc21ef028eff888fa75ea5d7f9084aec933e9bf00de217c068bb8f683; artifact hashes in TestResults/StarknetPublicSourceCandidateCurrent/selected-release-artifact.json. Ready for scoped Development publication; hosting counts unchanged until fresh acceptance.

Application releasecafd0f8c8ea93c22f5d4638acc336df5795a05d7/Core8a5ab453ee6734195331abeec0ad5c04e84b5332 pushed; job151477e9-730b-42b2-a29a-fa7b13378502 BUILDING. No configuration applied yet. Exact apply guard requires checked-out HEAD=cafd0f8c8; pending checkpoint docs intentionally uncommitted until apply. After exact SUCCESS, use COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT=cafd0f8c8ea93c22f5d4638acc336df5795a05d7 and Publish-HostedStarknetPublicConfiguration.py apply; reconcile per-key ACKs/whole candidate, then one journalled settings deployment. Never resend uncertain mutations.

Application151477e9 SUCCESS. Guarded three-setting apply completed with per-key ACKs and exact whole-candidate comparison; canonical DNA unchanged. One journalled redeploy requested, protected outcome retained: settings job03760bc9-9984-40c1-8963-55f907f51e66 QUEUED on same cafd0f8c8 image. Never repeat apply or redeploy. Hosted config now36query tariffs/33query-only profiles; runtime/native/federation acceptance still pending. Once exact settings SUCCESS, run Verify-HostedQuerySource.py StarknetOASIS --prepare with its one durable new cohort, then inspect all native/federation/reuse/isolation/replica evidence before completion ACK/card/count promotion.


## Hosted acceptance complete

Settings03760bc9-9984-40c1-8963-55f907f51e66 SUCCESS on cafd0f8c8/Core8a5ab45. Fresh original cohort native3/2 and Mongo federation5/4pass;2new private copies, repeat0new/2existing, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2. Every native header hash matches the configured selection and reports ACCEPTED_ON_L1. Exactly2save ACKs, private copies version1; preparation intents empty. Completion ACK stored in LocalState/COSMIC/HostedQueryAcceptance-StarknetOASIS/cql-http-acceptance.acknowledged.json; never rerun/reseed. Browser remains open. Counts38implemented/36hosted/36tariffs/browser5/open31;28added profiles/56copies/112presets.

- TestResults/HostedCqlIntegrationCurrent/StarknetOASIS-native-http.json:15591bytes; SHA2565bc12e889363694c5dfa20e863fcf330b2da102be84b57e406c48c44dbe4f599.
- TestResults/HostedCqlIntegrationCurrent/StarknetOASIS-client-http-flow.json:40878bytes; SHA25632500441fc33a07d4915d3eb92c16cfbba404cf43f6d9511ab855b49b9b3c59d.
- TestResults/HostedCqlIntegrationCurrent/StarknetOASIS-isolation-http.json:6301bytes; SHA2564e2e971826748e20d207f01936c2478205bb564fc1bd4eef8bad2220d1866609.

Next publish the staged card with verified HTTP scope and first-setup/C# links; do not claim browser or independent consensus verification. Full ORM/signed Starknet writes remain outside this bounded read profile.

Website main80ba51052c14a2f705e24e9825a9fe8b495fe642 now publishes the Starknet card and pinned guide164b4bb96/C#cafd0f8c8 links byte-identically:152265bytes, SHA25658bd2b0b5edec25c29d16f9351006dc4621dd6ca2818df5d08dec1551163a2a6. Public site-publication.json is under TestResults/StarknetPublicSourceCandidateCurrent. Publication does not certify browser interaction; gate remains open. Next breadth candidate is documented in CARDANO_PUBLIC_SOURCE_STATUS.md.
