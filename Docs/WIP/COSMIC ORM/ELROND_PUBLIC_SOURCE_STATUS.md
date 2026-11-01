# MultiversX / Elrond public source status

Updated 1 November 2026. Native CQL/shared-manager/C# CLI verified; API/local HTTP verified; hosted/browser gates open. Programme:36implemented query types,33scoped hosted HTTP providers,5browser proofs/28open. No count grant from a public API probe.

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
