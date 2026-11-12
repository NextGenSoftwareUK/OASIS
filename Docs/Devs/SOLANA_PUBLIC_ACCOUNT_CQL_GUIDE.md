# CQL over public Solana account metadata

This explicit read-only profile queries configured public accounts at finalized commitment. It uses the shared CQL planner and HolonManager executor. It needs no wallet, funding or chain transaction. It is separate from canonical Solana holon/memo storage and indexed history queries.

## Developer example

The spaced and commented [NativeKeySetWalkthrough](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs) is the executable LINQ example. Its `RunAsync` method has been run verbatim against three mainnet system accounts and a two-account subset.

```csharp
// Reuse your explicit public source configuration. Keep its mapping owner stable.
using var source = SolanaPublicAccountMetadataSource.FromJson(
    await File.ReadAllTextAsync(sourceConfigurationPath));

var activation = await source.ActivateQuerySourceAsync(cancellationToken);
if (activation.IsError)
    throw new InvalidOperationException(activation.Message, activation.Exception);

// The manager uses V2 DNA. Leave it unbound: the query supplies its own adapter.
// Do not register this representation over an existing wallet-backed provider.
var manager = new HolonManager(null, dna, registry);

// Membership becomes one bounded native account selection. The walkthrough
// prints each returned OASIS ID and its SolanaAccountAddress metadata.
await NativeKeySetWalkthrough.RunAsync(
    manager,
    source,
    configuredAccountAddresses,
    actorId,
    Console.Out,
    cancellationToken);
```

Required namespaces are in the linked sample. `dna.OASIS.HyperDriveMode` must be `V2`. The adapter is in `NextGenSoftware.OASIS.API.Providers.SOLANAOASIS`; no additional query implementation belongs in the application.

## HTTP and sandbox

Host configuration uses `CQL_SOLANA_PUBLIC_SOURCE_JSON`: schema `1`, credential-free HTTPS `rpcUrl`, pinned `genesisHash`, one to eight unique `addresses`, and a stable nonempty `sourceOwner` GUID. Unknown fields, including signer fields, are rejected. Enrollment also needs an explicit query-only permission and read tariff. Existing canonical Solana DNA remains unchanged.

Discover ready presets at `GET /api/data/query-cql/providers?examplesVersion=2`. Native requests use provider `SolanaOASIS`, `orderBy: "None"` and a string `In` filter with `metadataPath: ["SolanaAccountAddress"]`. The paired preset explicitly selects Mongo and Solana. Use the sandbox's normal preparation action to create two private Mongo snapshots before expecting five/four physical rows. Query requests still require normal authentication and an operation ID.

The [live sandbox](https://web4.oasisomniverse.one/web4-sandbox.html) shows actual source readiness; a configured or building source is not proof of availability. See [current acceptance status](../WIP/COSMIC%20ORM/SOLANA_PUBLIC_ACCOUNT_QUERY_STATUS.md) for release and browser gates.

## Limits

Only configured address equality/membership and bounded paging are supported. This is not arbitrary chain search, token transfers, transaction history or full Solana ORM parity. RPC reads verify chain identity and request a zero-length data slice. Slot, lamports, rent epoch and account space are returned as exact decimal strings to preserve UInt64 precision in browser clients. Native failures return errors without partial results or alternate-provider substitution.

Snapshots capture a demo record at preparation time; they are not a continuously synchronized account-state feed. Public RPC availability and rate limits still apply.
