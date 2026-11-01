# XRPL public ledger header CQL demo

This profile exposes a small, explicitly configured set of public XRPL ledger headers as holons. It uses HolonManager, capability-aware CQL planning and the shared LINQ adapter. It requires no wallet and submits no transaction.

## Run the developer demo

After building the existing console project from the COSMIC worktree:

```powershell
dotnet run --project Examples/CosmicCqlDemo --no-build -- --xrpl-public LocalState/COSMIC/HostedXrplPublicSourceCurrent/source.json
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

Reuse `LocalState/COSMIC/HostedXrplPublicSourceCurrent/source.json`, preserving the source owner and retained hash catalogue. Its exact schema is `schema` (1), `rpcUrl`, `networkId` (0), `ledgerHashes` and `sourceOwner` (nonempty GUID). The qualified endpoint is `https://xrplcluster.com/`, listed by XRPL as a public Mainnet full-history service. Unknown fields and malformed configuration are rejected; production JSON loading uses the existing guarded HTTP handler.

Native/C#/CLI and local HTTP/Mongo5/4/private-isolation evidence, plus remaining hosted/browser work are tracked in [XRPL status](../WIP/COSMIC%20ORM/XRPL_PUBLIC_SOURCE_STATUS.md). XRPL must not appear as a verified live sandbox provider until those separate gates pass. The planned hosted demo includes a ready Mongo-paired preset.

References: [XRPL public servers](https://xrpl.org/docs/tutorials/public-servers), [XRPLF header hashing](https://github.com/XRPLF/rippled/blob/develop/src/libxrpl/protocol/LedgerHeader.cpp).
