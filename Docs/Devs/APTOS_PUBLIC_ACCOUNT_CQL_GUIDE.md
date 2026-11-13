# Aptos public account CQL guide

This explicit read-only profile queries a small public Mainnet account catalogue through COSMIC's shared planner and HolonManager. No wallet, signing key or account creation is required. Check the [current acceptance report](../WIP/COSMIC%20ORM/APTOS_PUBLIC_SOURCE_STATUS.md) for local versus hosted support.

## First setup in a fresh checkout

The retained local configuration is intentionally untracked. If it is absent, create `LocalState/COSMIC/HostedAptosPublicSourceCurrent/source.json` with the complete public example below. If it already exists, reuse it. No chain account, key or environment is created by this configuration.

```json
{
  "schema": 1,
  "restUrl": "https://api.mainnet.aptoslabs.com/v1/",
  "chainId": 1,
  "consistency": "PerQueryLedgerVersion",
  "accounts": [
    "0x0000000000000000000000000000000000000000000000000000000000000001",
    "0x0000000000000000000000000000000000000000000000000000000000000003",
    "0x0000000000000000000000000000000000000000000000000000000000000004"
  ],
  "sourceOwner": "00000000-0000-0000-0000-000000000123"
}
```

The example GUID is a public projection namespace, not a credential or signing account. For your own application, choose one nonempty GUID once (for example with `[Guid]::NewGuid()`), save it and retain it between runs. Do not generate a new namespace on every launch.

## Run the C# walkthrough

Reuse `LocalState/COSMIC/HostedAptosPublicSourceCurrent/source.json`. Its sourceOwner is a stable projection namespace, not a signing identity. After restoring/building the demo project:

```powershell
dotnet run `
    --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    --no-build `
    --no-restore `
    -- --aptos-public LocalState/COSMIC/HostedAptosPublicSourceCurrent/source.json
```

The [commented walkthrough](../../Examples/CosmicCqlDemo/AptosPublicWalkthrough.cs) queries three accounts, then a genuine two-account subset. It prints account addresses, exact sequence numbers and the selected ledger version. No chain or Mongo write is performed.

## Express a selection

```csharp
// Native addresses must belong to the explicit configured catalogue.
// Capture selected values in a local variable for expression translation.
var selected = configuredAccounts.Take(2).ToArray();

var scope = CosmicQueryScope.Named(ProviderType.AptosOASIS).Result;

var query = CosmicQuery<Holon>
    .Create(scope)
    .Where(holon => selected.Contains(
        CosmicQueryMetadata.Value<string>(holon, "AptosAccountAddress")))
    .Result;

// NativeKeySetWalkthrough.RunAsync demonstrates execution through
// HolonManager with the activated explicit source and caller read access.
```

Supported predicates are string equality and bounded membership on `AptosAccountAddress`, over one to eight configured addresses. A result window operates on the complete verified selection. Arbitrary scans, sorting, Name/Description or other metadata predicates, application resources and signed storage are separate capabilities.

## Version and configuration semantics

The exact configuration schema has `schema: 1`, anonymous HTTPS `restUrl` ending `/v1/`, `chainId: 1`, `consistency: "PerQueryLedgerVersion"`, `accounts`, and stable nonempty `sourceOwner`. Addresses are canonical lowercase `0x` plus64hex characters. Unknown properties, credentials and caller-specified historical overrides are rejected.

Each query reads Mainnet ledger info once, checks body/header agreement, then includes that selected `ledger_version` in every account request. Response headers must remain on Mainnet and report a retained range containing it. Failure or pruning rejects the complete page; the source never silently reissues an unversioned request. Later queries can select newer versions. A Mongo snapshot is a captured observation and does not imply a cross-provider historical snapshot.

Metadata includes `AptosAccountAddress`, `AptosSequenceNumber`, `AptosAuthenticationKey`, `AptosLedgerVersion`, `AptosChainId` and `AptosConsistency`. UInt64 values remain exact decimal strings. Authentication keys are public account data and do not establish the caller's ownership or signing authority.

The API does not echo the account address in its account body. The profile preserves the explicit request identity and validates the documented account response; it does not independently verify state proofs or consensus. [Official account API](https://aptos.guide/rest-api/operations/get_account).

## Live sandbox

Open the WEB4 sandbox CQL tab, start a temporary session and select AptosOASIS. Choose the three-account or two-account preset. The Mongo-paired preset prepares two private snapshots and reuses them on repeat setup. Hosted HTTP proof covers native3/2, paired5/4, privacy and SQLite/Neo4j replicas. Native reads select a version per query; captured Mongo observations need not have that same version. Browser interaction is a separate gate.
