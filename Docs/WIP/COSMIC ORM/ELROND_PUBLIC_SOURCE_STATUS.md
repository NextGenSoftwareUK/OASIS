# MultiversX / Elrond public source status

Updated 1 November 2026. Qualification complete; native CQL/API/hosted/browser gates open. Programme remains35implemented query types,33scoped hosted HTTP providers,5browser proofs/28open. No count grant from a public API probe.

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

Durable `LocalState/COSMIC/HostedElrondPublicSourceCurrent/{source,catalogue}.json`,742bytes. SourceOwner is a stable projection namespace, not a wallet, credential or avatar ownership claim. Preserve the original keys/namespace; do not rediscover or regenerate on each run. Source schema is a candidate until native implementation consumes it.

## Exact next gates

1. Implement bounded key-set/equality native adapter using common CQL/read-activation interfaces, existing Bech32 helper, strict config/response/precision checks, cancellation and no partial pages.
2. Add controlled tests plus real shared HolonManager/LINQ3/2 and a spaced/commented C# walkthrough with complete first-setup JSON.
3. Independent query admission/tariff, four native/Mongo presets, shipping planner and local HTTP/privacy fixture before hosted grants.
4. Guarded hosted application/settings rollout and visitor3/2+5/4/reuse/isolation/replicas; retain all original save intents/ACKs.
5. Publish sandbox/local demo links and verify browser separately. Keep completed cohorts frozen.

Primary contracts: [official API role and Mainnet endpoints](https://docs.multiversx.com/sdk-and-tools/rest-api/multiversx-api/), [network configuration](https://docs.multiversx.com/sdk-and-tools/rest-api/network/), [public API reference](https://api.multiversx.com/), [native address filter release](https://multiversx.com/release/release-mx-api-service-v1-10-0).
