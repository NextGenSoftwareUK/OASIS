# MultiversX / Elrond public source status

Updated 1 November 2026. Native CQL/shared-manager/C# CLI verified; API/local HTTP and hosted HTTP verified; browser gate open. Programme:36implemented query types,34scoped hosted HTTP providers,5browser proofs/29open. No count grant from a public API probe.

## Qualified contract

- Existing provider type: `ElrondOASIS`; use current official Mainnet `https://api.multiversx.com/` for the explicit read profile. Existing signed wallet provider/default URL is unchanged and untested here.
- Public `dapp/config` reports `id:mainnet`, `chainId:"1"`, `egldDenomination:"18"`. UI `decimals:"4"` is display precision, not denomination. Independent gateway network/config also reports chain1/denomination18. These are API-reported identities, not independent genesis/consensus proofs.
- Three retained canonical62character `erd` Bech32 account addresses. Require checksum and32byte payload/canonical padding; existing Core `Helpers/Crypto/Bech32.cs` can be reused in the C# adapter without an SDK dependency.
- Native API membership uses `accounts?addresses=<comma-separated selection>&from=0&size=<selected count>&fields=address,balance,nonce,shard`. Real selections returned3and2records. No unbounded list scan or client content filtering is needed; response identity sets must exactly match the requested keys.
- LatestIndexed semantics: public API caching/indexing may lag; no pinned state, shared historical snapshot, freshness or consensus promise.
- Balances are canonical nonnegative decimal strings, preserved exactly (qualification includes a balance above64bits). A4096digit bound is a response contract budget, not a claim about protocol integer width.
- Nonce is an API JSON number: accept only exact safe nonnegative integers through2^53-1and expose strings. Larger values must fail rather than claim unverified UInt64 fidelity through the indexing service. Mainnet shard0/1/2or metachain4294967295; validate as integer, never coerce booleans/fractions/strings.
- Native profile must remain explicit read-only, outside signed wallet/default-write/replication enrollment. No source writes, account/key creation, SDK installation or paid access.

## Evidence

Nine pure tests pass: network/denomination, checksums, exact identity sets, native request binding, decimal balance/nonce precision, shard/metachain domain and duplicate JSON. [Controls](../../../Scripts/Sandbox/test_elrond_public_readiness.py).

[Probe](../../../Scripts/Sandbox/Probe-ElrondPublicReadiness.py) performs API identity plus native3/2membership reads. Raw `TestResults/ElrondPublicSourceCandidateCurrent/qualified-readiness.json`, 2138bytes, SHA256 `0c88e3364b8aca835e748ac91b9ddf6ee38000f69cbac1fab63226297dc8d36c`. Initial schema/network observation: `shape-readiness.json`, public-only, bounded64KiB per response.

Durable `LocalState/COSMIC/HostedElrondPublicSourceCurrent/{source,catalogue}.json`,742bytes. SourceOwner is a stable projection namespace, not a wallet, credential or avatar ownership claim. Preserve the original keys/namespace; do not rediscover or regenerate on each run. Source schema is consumed by the native read profile; original namespace retained.

## Exact next gates

1. Implement bounded key-set/equality native adapter using common CQL/read-activation interfaces, existing Bech32 helper, strict config/response/precision checks, cancellation and no partial pages.
2. Add controlled tests plus real shared HolonManager/LINQ3/2 and a spaced/commented C# walkthrough with complete first-setup JSON.
3. Independent query admission/tariff, four native/Mongo presets, shipping planner and local HTTP/privacy fixture before hosted grants.
4. Guarded hosted application/settings rollout and visitor3/2+5/4/reuse/isolation/replicas; retain all original save intents/ACKs.
5. Publish sandbox/local demo links and verify browser separately. Keep completed cohorts frozen.

Primary contracts: [official API role and Mainnet endpoints](https://docs.multiversx.com/sdk-and-tools/rest-api/multiversx-api/), [network configuration](https://docs.multiversx.com/sdk-and-tools/rest-api/network/), [public API reference](https://api.multiversx.com/), [native address filter release](https://multiversx.com/release/release-mx-api-service-v1-10-0).

## Native CQL milestone

34tests passed without skips, including real shared HolonManager/LINQ3/2. After correcting a response-identity fixture that previously shortened its page, all33controlled tests pass with an actual unexpected valid address at unchanged count. Native code is unchanged by that fixture correction. Actual commented C# CLI also passes3/2. No source or Mongo writes.

The adapter uses existing Core Bech32 decoding/encoding, common query/read-activation interfaces and Shared key-value base. One API network identity check precedes the native address-filter request per source query. Complete results validate before windows/publication; HTTP/identity/precision/cancellation errors return real OASISResult failures. This does not certify full UInt64 nonce precision or arbitrary metadata querying.

[Fresh-checkout guide](../../Devs/ELROND_PUBLIC_ACCOUNT_CQL_GUIDE.md), [commented C# walkthrough](../../../Examples/CosmicCqlDemo/ElrondPublicWalkthrough.cs).

Raw evidence stays in `TestResults/ElrondPublicSourceCandidateCurrent/`:

| Evidence | Passed | SHA256 |
|---|---:|---|
| elrond-source.trx | 34 | `210ba37c78d4ea2e756b8c52faacb6c1eaaf8f2bc7bb723b87cfd96250824638` |
| elrond-controls.trx | 33 | `bf6892307634314a8a837dcddddab0c3337ef3b14bd30baae5d3112adf6aa1fc` |
| demo-run.log | CLI3/2 | `41abfff644c0eb68c71d4400013a09f68043c584da7f1039ca775a328163a701` |

Next: independent API query-only enrollment/tariff, native/Mongo presets, shipping planning and local HTTP privacy. No hosted configuration/card/count promotion before those gates.

## API and local HTTP complete

2API admission/discovery tests,1Core tariff,1shipping planner over all4actual prepared presets,1native HTTP/Mongo case and3pure configuration tests pass without skips. Native3/2, paired5/4and foreign-private0verified over loopback shipping endpoints. Controlled bearer/admission are fixture infrastructure, not production billing/signup or hosted replication proof.

Exact owned database `cosmic_cql_elrond_http_demo` removed after marker verification, Mongo stopped/27029absent. Durable `LocalState/COSMIC/ElrondMongoHttpCurrent` retains intent,2save ACKs and cleaned acknowledgement. Never replay this green fixture. Prepared protected3setting candidate is unapplied; canonical wallet DNA unchanged.

Core task `f41b2a11253c222aac6bfdf67278d226e01ff96e` pushed and remote-ref verified. Next select verified Elrond/Sui files into the existing release branch, exclude TRON adapter/test changes, fetch/integrate shared refs without force, validate committed pins and require exact application SUCCESS before guarded settings/one rollout/hosted acceptance.

| Raw evidence under TestResults/ElrondPublicSourceCandidateCurrent | Passed | SHA256 |
|---|---:|---|
| elrond-admission.trx | 2 | `44abea992ed5b2f9fc37dfc8bee622e6809b3b4176e980dcca748efc9fc0c8b6` |
| elrond-tariff.trx | 1 | `c1cd0b8513f76a8fe1f6f7b918e7100e51ce4e25d90db5777dc84c17d690f73b` |
| elrond-shipping-presets.trx | 1 | `fab9657b82aecce53626467e89b85e64e38c0015abf481c124dc0bab8eb502a4` |
| elrond-mongo-http.trx | 1 | `fdca17392d2a71255cf26a58d7b69b1d9cbc082fc2ee378a110d88eb266d14ba` |

Selective release prepared from39b12bc28 on existing work/cosmic-cql-public-release (base7e0b60994),41reviewed files. Excluded TRON native adapter, its test file and test-project change. TRON qualification reports/probes remain evidence only; no API/tariff grant. Next verify actual release API/planner build and absence of candidate TRON type before Development publication.

Scoped release43ca9f0d0verification passes3API/planner cases without skips and5SDK source/import closures. Rebuilt TRON DLL lacks task-only candidate type identifier; source also absent. Raw elrond-release.trx SHA256 `3a71d8f79e5a11d786500fb4b174c2e6876d4b4e0667e79a82ebfb62e9c0f2ce`; release-tron-assembly.json SHA256 `254e2097a792e8e152a97dae4d7893984d589e0d817d75ed199fd94bac722a58`; release-file-scope.json SHA256 `fcb887d9ed7a5179263259e160b721075a9de47758f150e5907aca7491de70a4`. Native green Mongo fixture was not replayed. Ready for fresh-fetch/non-force Development publication; no Elrond hosted variables applied yet.

Published verified parent801795a648a1ae0b9211c0a478027673cb79c156 and Coref41b2a11253c222aac6bfdf67278d226e01ff96e to Development after fresh-fetch/ancestry checks, non-force pushes and exact ls-remote verification. Exact app19f65f59-e370-4cad-94ce-b0246872b891 QUEUED; settings candidate still unapplied. Do not restart job or replay green seeds. Existing frozen release branch matches801795a64 for later apply guard.

Exact app19f65f59 SUCCESS at801795a64/Coref41b2a1. Guarded three-setting apply completed once using matching frozen release HEAD; each value and whole protected candidate observed exactly. One journalled settings job18c4f04e-6ac3-4fa7-ac2a-ff312d095159 BUILDING. No hosted visitor/snapshot cohort yet; observe same job before normal-visitor acceptance. No settings replay/restart or extra hosting spend.

## Elrond hosted HTTP complete — 1 November 2026

**34 providers have scoped hosted HTTP evidence; 5 have browser evidence and 29 browser gates remain open.** MultiversX/Elrond now passes native 3/2, Mongo federation 5/4, repeat reuse (zero new/two existing), foreign-private zero/public three, and two matching replicas on both SQLite and Neo4j. No source/chain writes or browser certification are inferred.

Latest verified hosted acceptance: application `801795a648a1ae0b9211c0a478027673cb79c156`, Core `f41b2a11253c222aac6bfdf67278d226e01ff96e`, app `19f65f59-e370-4cad-94ce-b0246872b891` and settings `18c4f04e-6ac3-4fa7-ac2a-ff312d095159` succeeded. Elrond card publication is next; last byte-verified website main remains `9679699`.

There are **36 implemented query types**: 3 canonical adapters, 31 bounded public/source profiles and 2 locally scoped Azure/Oracle implementations. Elrond has34native tests + actual shared-manager/CLI3/2, API2/Core1/shipping1/localHTTP1/config3passes and a scoped release3test/artifact check excluding TRON's unverified adapter. Azure/Oracle hosting remains explicitly deferred. TRON's real429and Starknet/Radix403access qualifications stay red and uncounted. Full ORM/parity/HyperDrive certification remains incomplete. Dated milestones retain their original scope.

Original sourceOwner/catalogue and owner/foreign cohort retained. Completion ACK under `LocalState/COSMIC/HostedQueryAcceptance-ElrondOASIS/` verified from raw native/federated/isolation/replica counts. Do not replay its saves. LatestIndexed is not a pinned/shared historical snapshot. Mongo demo copies capture private name/description/native-key observations, not full native account/state snapshots. API nonce safe-integer limitation remains explicit.

| Evidence under TestResults/HostedCqlIntegrationCurrent | SHA256 |
|---|---|
| ElrondOASIS-native-http.json | `f104103460d61fad4742b13a2f79031b28db5103c8a7e73a4b93d8ce7dc72f71` |
| ElrondOASIS-client-http-flow.json | `71e8b470f90b49c69e9d023636d90c487fd8edae7a08f072e25964c6c001b42b` |
| ElrondOASIS-isolation-http.json | `03a08a90c1efd0b55c9dcb5e9519ff22830f7539c640d4cac7aa3c45485e200c` |
