# Radix public source qualification

Updated 1 November 2026. Qualification red; no native CQL/API/hosted grant or count change.

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
