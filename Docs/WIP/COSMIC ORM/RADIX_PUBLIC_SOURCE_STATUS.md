# Radix public source qualification

Updated 1 November 2026. Native CQL50/50 tests and actual console3/2 pass; API enrollment/local HTTP/hosted/browser remain open. Earlier Python denials below are retained history, not a global endpoint failure.

## Observed access

The documented Mainnet Gateway `https://mainnet.radixdlt.com` returned HTTP403 for read-only `/status/network-configuration` and `/status/gateway-status` POSTs. No network configuration, ledger state, resource/entity records or native filters were observed.

A separate bounded denial diagnostic returned403with server `cloudflare`, JSON content type, body indicators `cloudflare`/`access denied`, and no retry-after or proxy-block-reason header. This identifies a Cloudflare-fronted denial, not its policy cause. Do not infer chain outage, archive/payment requirements or a canonical SDK bug. No after-failure fallback or repeated acceptance attempt.

- Raw `TestResults/RadixPublicSourceCandidateCurrent/shape-readiness.json`, 220bytes; SHA256 `6b3991b74ff016786b1381c95d99ea3f38ab7b1946eb2ff75a0071a1625acff1`.
- `denial-diagnostic.json`, 596bytes; SHA256 `661d290229bd5d7562e3f9f7aa53eccdd225c28e7c8fc192035864f82a13afbc`.
- No sourceOwner/catalogue, blockchain account, key, SDK/runtime install, database or local process/port. Existing Radix signed-storage client was inspected only; not executed or certified.

## Initial design — historical, implemented in milestone below

Official docs identify Mainnet network ID1and `_rdx` addresses. A future explicitly qualified Gateway profile could read a small enrolled resource/entity catalogue with native `/state/entity/details` membership and an acknowledged `at_ledger_state`, if the actual endpoint proves those contracts. Bech32M/checksum/entity type, exact decimal strings, complete identities, ledger-state equality, response budgets, cancellation and public visibility need implementation/tests before any CQL grant. Do not reuse ordinary Bech32 validation or silently drop a requested ledger state.

Next: qualify suitable documented free access without a new runtime or blind retry; continue exact Elrond hosted gates first. No David action/account request yet. Full ORM/owned Radix writes remain outside this failed public-read probe.

Primary references: [Mainnet environment/Gateway](https://docs.radixdlt.com/docs/concepts-environments/), [network APIs](https://docs.radixdlt.com/docs/network-apis/), [well-known resources](https://docs.radixdlt.com/docs/well-known-addresses/).

## Guarded native transport and entity contract qualified

Both local Node24and the existing dev container returned200for network/gateway status. Local PowerShell7.6.5/.NET10.0.11 then used the **actual built Coref41b2a1 `NativeConnectionGuard.CreateGuardedHttpHandler`** and also returned200. No production transport change, guard suppression, alternate endpoint, user agent spoofing, API key or account was needed. Python urllib403is client-specific in these observations; its policy/TLS cause is not established. Do not treat that probe as evidence the shipped .NET reader cannot connect.

Four guarded .NET POST reads (network, status, entities3, entities2) returned exact Mainnet identities and complete native address sets at requested/returned state version **566676604**. Response order differs from request order; the adapter must map by address and restore selected order. One fungible and two nonfungible resource definitions,67character addresses; observed total supplies are exact strings, including `13563896903.306018702426495105`.

- `TestResults/RadixPublicSourceCandidateCurrent/entity-readiness.json`, 3191bytes; SHA256 `3dc5ff02cc03a9bc68805526d07c2350c4d63965ca932602791e4e0ad6e35f9f`.
- Full bounded public response bytes retained as network/status/entities-3/entities-2 `.response.json` in that folder, each hashed in the report. No sourceOwner/catalogue allocated yet.
- These are native API/transport observations, not shared-manager CQL, production SLA, independent consensus or cross-provider historical-snapshot proof.

Shared transport evidence in `TestResults/PublicSourceEgressCurrent/`:

| Evidence | Bytes | SHA256 |
|---|---:|---|
| reachability.json | 2377 | `f8f56280a20ac5eadfc432372cc63445fe93d35df3b1abb7feae3bb8bc3d2e72` |
| local-node-reachability.json | 2378 | `903d6b09622f38c69688a71f4b58b8f6c54cf45e95b23ba77cd96b08de9fd1bf` |
| dotnet-reachability.json | 1206 | `1a23703de60e4a691f77bb99c5b3d61f1e4235db91392426b22cdab7dbfa01ca` |

Exact next work: implement explicit read-only Radix resource-address CQL source with per-query ledger pinning, Bech32M/resource type validation, native decimal-string precision and complete page/cancellation checks; controlled tests, real shared manager3/2 and spaced C# guide; then query-only admission/planner/local HTTP before hosted rollout. Keep all access guards enabled and preserve successful/failed observations. No CQL/count grant yet.

## Native CQL and console milestone — 1 November 2026

Read-only adapter implemented: exact Bech32m resource identity/kind, signed192-bit scale18 supply strings, complete selected identities, optional explicit string metadata, per-query ledger tuple, budgets and cancellation. Focused MSTest50/50 pass, zero skips, including real HolonManager/LINQ3/2. Actual compiled console --radix-public also returns3/2 (full-query ledger566680138). Stable source/catalogue retained under LocalState/COSMIC/HostedRadixPublicSourceCurrent; no wallet/key/database or source writes.

- `TestResults/RadixPublicSourceCandidateCurrent/radix-source.trx`, 75493bytes; SHA256 `f1a6d9cb6310a7c9318388b512cca1425f60febc77695a56ab5153d6f0ba5a8d`.
- `TestResults/RadixPublicSourceCandidateCurrent/cli-native-execution.log`, 1530bytes; SHA256 `6cb7304932b1ae9caa3652d00c60397d2e384579f7fbe1e13fd7d3cc3fddec2b`.
- `TestResults/RadixPublicSourceCandidateCurrent/entities-explicit.response.json`, 25060bytes; SHA256 `8439d1f88dfbd2b4f9ea8c85fada1116b8a4335122878c5b61f4f73e184d23c7`.

Initial cache-only restore stopped NU1101; existing pinned toolkit2.2.2 then restored into the shared NuGet cache. Initial dotnet-run forwarded build switches to app/printed usage; direct execution of compiled CLI passed. Neither was a native query failure.

Next: query-only API admission/planner tests, local HTTP/Mongo federation, then hosted rollout/card/browser. Counts37 implemented/36tariff/34hosted/5browser. No full ORM/write/consensus claim. [Developer guide](../../Devs/RADIX_PUBLIC_RESOURCE_CQL_GUIDE.md).

## Query admission and local HTTP federation — 1 November 2026

API read-only enrollment/preset planning2, shipping GraphQL schema1, Core tariff1, configuration controls3 and local native HTTP1 all pass with no skips. Required explicit Radix project reference added to the shipping API after first build identified it missing. No fallback paths. Core task commit e4d6e4b274f719a9acee2a0b8a4150ec5e660299 pushed/ls-remote verified.

Actual native endpoint results: Radix3/2, Mongo+Radix5/4, foreign-private0, foreign-public pair3. Two SDK-owned private Mongo saves acknowledged,9receipts; controlled authentication/admission is test infrastructure, not production billing or replication proof. Exact marker-verified DB cosmic_cql_radix_http_demo dropped after success, Mongo owned process stopped/27029absent. Preserve original durable intent/ACKs/cleanup marker; never reseed this green cohort.

- `TestResults/RadixPublicSourceCandidateCurrent/radix-api.trx`, 6547bytes; SHA256 `ee5d0513a905594b7a9190a379d479d219f29822fbf3cea5988c125bd7919a36`.
- `TestResults/RadixPublicSourceCandidateCurrent/radix-tariff.trx`, 3294bytes; SHA256 `3897d26ade4229a8ab06613158ccbfa6ecebe196be913d64335bab2fccf37fae`.
- `TestResults/RadixPublicSourceCandidateCurrent/radix-mongo-http.trx`, 3353bytes; SHA256 `a69c1dba9696b430ded1c971e3924514fd477ed71e004a6627af4d94e9b1c92b`.
- `TestResults/RadixPublicSourceCandidateCurrent/radix-mongo-native-summary.json`, 223bytes; SHA256 `5a36344f3d765302e9271ab0bac8e67a91e0d6a2f2782e1088cbfeccd1d318fe`.
- `TestResults/RadixPublicSourceCandidateCurrent/config-tests.log`, 106bytes; SHA256 `383a12fba9b5b327054a24ce7e5739569c4f0b6621facd981f2d6a4271063b3c`.
- `TestResults/RadixPublicSourceCandidateCurrent/sdk-closure.log`, 388bytes; SHA256 `b18bbfd5e5fe62d2de36ddc0de77e6d344d7e93f30817b777092834f19a64885`.
- `TestResults/RadixPublicSourceCandidateCurrent/idle-worker-cleanup.json`, 75bytes; SHA256 `bd7160e6f1b926126dffa368b5dc48179fb36b078b174c099f47ea4094d5eec6`.

Next: prepare/review protected hosted configuration, selectively assemble verified release excluding red TRON adapter, verify artifact/dependency pins, publish, apply and run one original hosted cohort; then sandbox card/browser. Hosted count remains34; native/API support alone does not increment it.
