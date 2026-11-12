# XRPL public ledger header CQL demo

This profile exposes a small, explicitly configured set of public XRPL ledger headers as holons. It uses HolonManager, capability-aware CQL planning and the shared LINQ adapter. It requires no wallet and submits no transaction.

## Run the developer demo

After building the existing console project from the COSMIC worktree:

```powershell
dotnet run --project Examples/CosmicCqlDemo --no-build -- --xrpl-public LocalState/COSMIC/HostedXrplPublicSourceCurrent/source-free-endpoint.json
```

[Commented walkthrough](../../Examples/CosmicCqlDemo/XrplPublicWalkthrough.cs) · [Shared query construction](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs)

The walkthrough activates the retained source, selects all three ledger hashes, then applies a real two-key `Contains` predicate. Expected results are three and two records. Each row prints its ledger index and verified native hash. Neither XRPL nor Mongo records are written by this console example.

## Supported query scope

- Provider: `ProviderType.XRPLOASIS`.
- Metadata key: `XRPLLedgerHash`, exactly64uppercase hexadecimal characters without a prefix.
- Equality and membership over one to eight explicitly configured hashes.
- Bounded Skip/Take after the entire native selection and access check; no partial rows on failure, including an empty output window.
- Arbitrary scans, ordering and unsupported predicates are rejected by provider capabilities.

Rows expose name, description, network0, ledger index, parent/account/transaction hashes, total XRP drops as an exact decimal string and XRPL close time. Exposed fields do not imply arbitrary predicates are supported.

## What is verified

Each native header is serialized using XRPLF's fixed-width big-endian format with the LWR prefix; SHA512Half must match the requested and returned hash. Envelope/header ledger indices must agree, and the endpoint must explicitly report a closed, validated header and Mainnet network_id0. The retained three-header sample is linked by parent hashes.

Network identity and validation are reported by the authenticated public endpoint. This adapter does not independently verify validator signatures, UNL consensus, account-state trees or transaction inclusion. It does not configure the canonical provider's archive account or signed memo storage.

## Configuration

Reuse `LocalState/COSMIC/HostedXrplPublicSourceCurrent/source-free-endpoint.json`, preserving the source owner and retained hash catalogue. Its exact schema is `schema` (1), `rpcUrl`, `networkId` (0), `ledgerHashes` and `sourceOwner` (nonempty GUID). The hosted-qualified free endpoint is `https://s2.ripple.com:51234/`, listed by XRPL as Ripple’s Mainnet full-history service. The original xrplcluster.com endpoint returned HTTP402 from the hosted environment; no payment or automatic fallback was enabled. Unknown fields and malformed configuration are rejected; production JSON loading uses the existing guarded HTTP handler.

## Live sandbox

Open the [WEB4 sandbox](https://web4.oasisomniverse.one/web4-sandbox.html), choose the CQL tab and select `XRPLOASIS`. Four ready presets show three public headers, a two-header subset, and each selection paired with your private Mongo snapshots. Prepare the paired example once to create two private Mongo records; repeating preparation reuses them. Expected paired counts are five and four. Another visitor sees public headers and no private snapshots.

Hosted native/Mongo/reuse/isolation and exact SQLite/Neo4j replica checks pass on c6e7f14ba/Coree032d0a, correction74bdf044. Browser interaction verification remains open. These are public native reads plus owned Mongo snapshots, not signed XRPL memo persistence or independent validator consensus.

For a fresh developer checkout, save this public example as your source JSON. `sourceOwner` is a stable projection namespace, not a wallet or signing permission. Keep it fixed between runs; reuse existing configured source identities.

```json
{
  "schema": 1,
  "rpcUrl": "https://s2.ripple.com:51234/",
  "networkId": 0,
  "ledgerHashes": [
    "3AA1A57F3E24012170CD2922194620B795D9CBA8698E79A4EEFA10A26A8F6592",
    "EDDBA0BAB31F6EEC15E0C3913B2CB0207D11F7F8020DC77C76CFED364AF3520C",
    "9F7C6331BE65A669963F42357757C6E0C47B7B2A52E67793E86AA87A877BC87A"
  ],
  "sourceOwner": "00000000-0000-0000-0000-000000000123"
}
```

[Native/local/hosted scope and retained proof](../WIP/COSMIC%20ORM/XRPL_PUBLIC_SOURCE_STATUS.md).

References: [XRPL public servers](https://xrpl.org/docs/tutorials/public-servers), [XRPLF header hashing](https://github.com/XRPLF/rippled/blob/develop/src/libxrpl/protocol/LedgerHeader.cpp).
