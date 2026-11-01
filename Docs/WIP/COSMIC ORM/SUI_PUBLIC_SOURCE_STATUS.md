# Sui public object CQL status

Updated 1 November 2026. Qualification complete; CQL/API/hosted/browser gates open. Existing programme counts:34implemented query types and31scoped hosted HTTP providers.

## Qualified contract

- Use the official anonymous `https://graphql.mainnet.sui.io/graphql` endpoint. The existing Sui SDK bridge already uses `SuiGrpcClient` with pinned `@mysten/sui2.35.0`; leave it intact. No SDK restore, keys, account creation or source writes.
- Three canonical object addresses, numeric IDs5,6,7 represented as lowercase `0x` plus64hex characters. Proposed predicate key `SuiObjectId`, equality/bounded membership within an explicit catalogue.
- One parameterized GraphQL **query**, not a mutation: request chainIdentifier, genesis checkpoint digest, and the selected objects under `checkpoint { sequenceNumber digest query { ... } }`.
- Bind the full API-reported Mainnet genesis digest `4btiuiMPvEENsttpZC7CZ53DruC3MAgfznDbASZ7DR6S` in both reported identities. This GraphQL digest is not the old JSON-RPC eight-character chain fingerprint.
- All selected objects share the explicit per-query checkpoint scope. Preserve object identity, version, digest and checkpoint metadata. Later queries can select newer checkpoints; this is not a cross-provider historical snapshot.
- Versions/checkpoint sequences use the native UInt53 domain and are exposed as decimal strings. Digests must be canonical Base58 encodings of32bytes. Digest encoding validation is not independent BCS/object hashing or consensus/state verification.
- Reject GraphQL errors even alongside data, missing/null/extra selected aliases, mismatched identity, malformed digests, invalid numeric domains and duplicate JSON. No partial page or JSON-RPC/endpoint fallback.
- The public endpoint is rate-limited development/public-good access, not a production SLA.

Primary references: [current Sui RPC status](https://docs.sui.io/references/sui-api), [GraphQL endpoint and checkpoint scope](https://docs.sui.io/develop/accessing-data/graphql/query-with-graphql). Sui Foundation's public JSON-RPC service is retired; this profile starts on the supported GraphQL interface. Existing gRPC bridge needs no transport replacement.

## Evidence

`Scripts/Sandbox/Probe-SuiPublicReadiness.py`: one bounded anonymous GraphQL query, three complete object observations at checkpoint340483550, zero mutations. Three earlier memory-only read/schema queries established the response shape. `test_sui_public_readiness.py`: eight boundary tests pass, no skips/network dependencies.

- Raw `TestResults/SuiPublicSourceCandidateCurrent/readiness.json`,1494bytes; SHA256 `cc77dc43149227044b5d26c51909c62fa504fd67fe364b1c3c7eac2029f7c4cd`.
- Durable `LocalState/COSMIC/HostedSuiPublicSourceCurrent/{source,catalogue}.json`,1982bytes. Stable sourceOwner created once; preserve it and all three object addresses. Candidate schema is not consumed by shipping code yet.
- Retained checkpoint/version values are historical evidence, not a permanently pinned/prunable configuration view. The eventual source selects a fresh checkpoint per native query.

## Next gates

1. Implement explicit read-only GraphQL object source with checkpoint-scoped selection, variable binding, bounded/cancelled reads and exact complete-page validation. Reject unknown config/history/signing fields and unsupported scans.
2. Controlled tests and real shared HolonManager/LINQ3/2 plus readable C#/CLI walkthrough and fresh-checkout JSON example.
3. Independent query admission, native/Mongo presets, shipping planner, local HTTP/Mongo privacy and focused client preparation.
4. Guarded hosted rollout, native3/2+paired5/4, repeat reuse, foreign-private0 and replica checks; retain original save intents/ACKs.
5. Publish sandbox/local-demo links and verify browser interaction separately. No provider-count promotion before the corresponding gate passes.

## Native CQL complete — 1 November 2026

30 native tests pass without skips, including real shared HolonManager 3/2; actual `--sui-public` CLI also passes 3/2. One read-only GraphQL query binds all object addresses under one checkpoint. Strict genesis, identity, complete-page, digest encoding, UInt53 precision, response-size and cancellation validation is covered. No source mutations, new SDK or runtime installs.

- `TestResults/SuiPublicSourceCandidateCurrent/sui-source.trx`: SHA256 `2232c8efe7166ce3f0006ed4853c538db9987752250c2faad03ef1763b8fa212`.
- `TestResults/SuiPublicSourceCandidateCurrent/demo-run.log`: SHA256 `d5431694cf2426b5d1952f8143dfb9f5260e335892a35642041cf5d47c8ce1ea`.
- Existing catalogue/sourceOwner retained. API admission, local HTTP/Mongo, hosted acceptance and sandbox browser gates remain open; previous implementation/test gates above are complete.

## API and local HTTP complete — 1 November 2026

Two API admission/discovery tests, one independent Core tariff test, one shipping planner test over all four actual publisher-generated presets and one native HTTP/Mongo test pass without skips. Three pure Python configuration tests also pass. Preset review used the retained public source and a controlled admission baseline; it is not a captured hosted configuration.

The native HTTP fixture proves 3/2 public objects, paired 5/4 and foreign-private zero. Controlled authentication/admission is test infrastructure, not production signup/billing or hosted replication proof. Exact owned database `cosmic_cql_sui_http_demo` was marker-verified and dropped; `LocalState/COSMIC/SuiMongoHttpCurrent` retains intent, two save ACKs and cleaned acknowledgement. Port27029 is no longer listening. Do not replay this green fixture.

The first runner invocation selected zero tests because its frozen binary predated the added fixture. Its count gate failed before fixture creation; the new test was compiled and the subsequent selected test passed. No failed writes or duplicate seed cohort.

Core task commit `0e540eb911671a6a249458ea9b69d3e96fb1d212` is pushed and remote-ref verified. No Development deployment or Sui setting mutation yet. Next: prepare/review exact hosted candidate, publish verified application and observe exact SUCCESS before applying journalled settings and running hosted acceptance.

Raw evidence stays in `TestResults/SuiPublicSourceCandidateCurrent/`:

| Evidence | Passed | SHA256 |
|---|---:|---|
| sui-admission.trx | 2 | `f7884fd9d6fe6b6c5eabf9f02e4ddf7569bfc20936c928a4bfda495e931f5dce` |
| sui-tariff.trx | 1 | `4f6e220cfee521fe79c0c10652aa612fd669d563a17ed75ff4af655e63a346c9` |
| sui-shipping-presets.trx | 1 | `983ffece31170a7d54397cecd9f0fa831598e966ac16140ad3a381054e02e8ad` |
| sui-mongo-http.trx | 1 | `9df1446dcc463117f02a91ef1bbbbf1b332ff98b0afdc2ca492426010f1cbdbd` |

Hosted candidate prepared against current exact variables; all four actual prepared presets pass shipping capabilities (one test, no skips), SHA256 `a3c70bb8bd2a33049a7f8bcfc32b5e858c23036b9db1e9804d67b9754e4fedf2`. Core Development remote verified at0e540eb911671a6a249458ea9b69d3e96fb1d212; parent application publication next. Protected three-setting candidate remains unapplied.
