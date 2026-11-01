# Radix public source qualification

Updated 1 November 2026. Exact guarded .NET native API qualification now passes3/2at one requested ledger version; CQL/API enrollment/hosted/browser gates remain open. Earlier Python denials below are retained history, not a global endpoint failure.

## Observed access

The documented Mainnet Gateway `https://mainnet.radixdlt.com` returned HTTP403 for read-only `/status/network-configuration` and `/status/gateway-status` POSTs. No network configuration, ledger state, resource/entity records or native filters were observed.

A separate bounded denial diagnostic returned403with server `cloudflare`, JSON content type, body indicators `cloudflare`/`access denied`, and no retry-after or proxy-block-reason header. This identifies a Cloudflare-fronted denial, not its policy cause. Do not infer chain outage, archive/payment requirements or a canonical SDK bug. No after-failure fallback or repeated acceptance attempt.

- Raw `TestResults/RadixPublicSourceCandidateCurrent/shape-readiness.json`, 220bytes; SHA256 `6b3991b74ff016786b1381c95d99ea3f38ab7b1946eb2ff75a0071a1625acff1`.
- `denial-diagnostic.json`, 596bytes; SHA256 `661d290229bd5d7562e3f9f7aa53eccdd225c28e7c8fc192035864f82a13afbc`.
- No sourceOwner/catalogue, blockchain account, key, SDK/runtime install, database or local process/port. Existing Radix signed-storage client was inspected only; not executed or certified.

## Potential native design — not implemented

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
