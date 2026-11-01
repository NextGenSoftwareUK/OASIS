# Stellar public ledger header CQL demo

This read-only profile maps an explicit public Stellar ledger catalogue into holons and queries it through HolonManager/CQL/LINQ. It verifies complete native header XDR before returning metadata. No wallet or transaction is needed.

## Run the commented developer demo

After building the existing console project from the COSMIC worktree:

```powershell
dotnet run --project Examples/CosmicCqlDemo --no-build -- --stellar-public LocalState/COSMIC/HostedStellarPublicSourceCurrent/source.json
```

[Spaced/commented C# walkthrough](../../Examples/CosmicCqlDemo/StellarPublicWalkthrough.cs) · [Shared query construction](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs)

The walkthrough activates the retained source, selects all three configured ledger sequences, then uses a real two-key Contains predicate. It prints each sequence and native ledger hash. Expected counts are three and two. It creates no native transactions or Mongo records.

## Query contract

- Explicit provider scope: `ProviderType.StellarOASIS`.
- Query key: `StellarLedgerSequence`, a canonical positive UInt32 decimal string.
- Equality or membership over one to eight configured sequence/hash pairs.
- Complete selection/access checks precede bounded Skip/Take and publication; errors never return partial rows, including empty output windows.
- Arbitrary scans, ordering and unsupported predicates are rejected by capabilities.

Rows expose native ledger hash, parent, protocol version, close time, total/fee-pool stroops, unsigned ID pool and public-network passphrase. Integer values stay exact decimal strings. Exposed metadata is not a promise that every field supports a predicate.

## Native proof and limits

Every row must match its configured sequence and native SHA256 hash. The complete XDR decoder consumes bounded upgrade/signature values and known StellarValue/header-extension variants, binds sequence, rejects trailing/truncated/noncanonical data and preserves exact signed/unsigned64bit fields. API hash/sequence/parent/protocol must agree with decoded data.

Horizon must explicitly report the public-network passphrase. This is not independent SCP validator-signature/consensus validation, a full account-state proof, or signed Stellar persistence. The source remains separate from the canonical provider's transaction/write configuration.

## Configuration

Reuse the retained source owner/catalogue. For a fresh developer checkout only, this public example can be saved as the source file; its stable projection namespace is not an account or signing permission:

```json
{
  "schema": 1,
  "horizonUrl": "https://horizon.stellar.org/",
  "networkPassphrase": "Public Global Stellar Network ; September 2015",
  "ledgers": [
    {
      "sequence": 65247993,
      "hash": "c8deae88416e676c30358bef81cfbb7aa78546a7713d3c252653591e3c3340ce"
    },
    {
      "sequence": 65247992,
      "hash": "4cc75f196c333bcc325fc90d1a42ea39209513e7ad1696c1866573d90a74d768"
    },
    {
      "sequence": 65247991,
      "hash": "d898b8941c4cb2d0c03d5cf5fac6fb18b52888102fdb3283c26b779a039054d9"
    }
  ],
  "sourceOwner": "00000000-0000-0000-0000-000000000123"
}
```

The exact schema rejects unknown fields, duplicate sequences, malformed hashes, non-public network configuration and empty owners. JSON construction uses the existing guarded HTTP client. Native reads use the configured Horizon root and bounded point reads, not discovery scans or endpoint fallback.

## Current availability

Native controlled CQL, real C# and actual CLI3/2 pass. A read-only probe from the existing hosted container reaches the same public network/retained header; it is not hosted CQL acceptance. API read admission and exact four-preset planner pass; local HTTP proves native3/2, Mongo5/4 and private isolation. Hosted rollout/replicas and browser checks remain unfinished gates. [Current evidence and next steps](../WIP/COSMIC%20ORM/STELLAR_PUBLIC_SOURCE_STATUS.md).

References: [Stellar header XDR](https://github.com/stellar/stellar-xdr/blob/master/Stellar-ledger.x), [Horizon ledgers](https://developers.stellar.org/docs/data/apis/horizon/api-reference/resources/ledgers).
