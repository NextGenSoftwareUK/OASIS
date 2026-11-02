# Akash public provider CQL demo

This read-only profile queries an acknowledged sample of public Akash provider
metadata. It uses the same CQL planner, HolonManager and deferred LINQ executor
as the other providers. It does not provision compute or require a wallet.

## Run the C# walkthrough

After building the demo, run from the repository root:

```powershell
dotnet run --project Examples/CosmicCqlDemo --no-build -- `
  --akash-public LocalState/COSMIC/HostedAkashPublicSourceCurrent/source.json
```

The walkthrough reads three providers, then filters to two. It prints OASIS IDs,
native owner addresses, display names and indexer-reported online flags, with
spacing and comments in [the C# source](../../Examples/CosmicCqlDemo/AkashPublicWalkthrough.cs).
It creates no source records, Mongo snapshots or compute deployments.

The retained configuration is local state, not committed credentials or a seed
file. Keep its projection owner and provider sample stable across runs. Completed
native qualification/acceptance gates must not be replayed to produce more green
evidence. Ordinary visitor queries are separate from those acceptance gates.

## Explicit configuration

`AkashPublicProviderMetadataSource.FromJson` accepts exactly four fields:

| Field | Meaning |
|---|---|
| `schema` | Integer `1` |
| `apiUrl` | Anonymous HTTPS indexer API root |
| `providerOwners` | One to eight unique canonical lowercase Akash owner addresses |
| `sourceOwner` | Stable nonempty OASIS projection-owner GUID |

The development sample is bound to its original acknowledged native observation.
The projection owner identifies the OASIS projection; it does not establish
ownership of a provider account. API keys, signer/billing settings, credentials,
unknown fields and duplicate fields are rejected.

Activation checks every configured provider through bounded point reads. Query
execution uses only configured keys; it never rediscovers the network catalogue.

## Query shape

The walkthrough calls `NativeKeySetWalkthrough`, which follows this pattern.
Here `source` is the activated explicit Akash adapter and `manager` is the shared
HolonManager. Check each result before accessing its `Result`:

```csharp
// Capture the requested native identities, not OASIS projection IDs.
var selectedOwners = configuredOwners.Take(2).ToArray();

var scope = CosmicQueryScope.Named(ProviderType.AkashOASIS);
// Handle scope.IsError before continuing.

var query = CosmicQuery<Holon>.Create(scope.Result)
    .Where(row => selectedOwners.Contains(
        CosmicQueryMetadata.Value<string>(row, "AkashProviderAddress")));
// Handle query.IsError before continuing.

var deferred = CosmicLinq.Create(
    manager,
    source,
    query.Result,
    maximumResults: 40);
// Handle deferred.IsError before continuing.

var rows = await deferred.Result.ToCosmicListAsync(cancellationToken);
// Handle rows.IsError before using rows.Result.
```

Equality on `AkashProviderAddress` also works. Bind a single scalar first, such
as `var selectedOwner = configuredOwners[0]`, and capture `selectedOwner` in the
predicate. Captured array indexing inside a predicate is outside this profile's
verified compiler shapes. Captured offset/limit paging is supported.

Unknown keys fail before HTTP, including keys a page would otherwise skip. Native
errors fail the whole selected batch, including an otherwise empty page. Result
budgets, cancellation and inactive sources produce explicit errors. No scan,
partial success or legacy-adapter substitution is used.

## Returned data and limits

Public holons contain identity/name/description plus `AkashProviderAddress`,
`AkashProviderHostUri`, `AkashProviderOnline`, `AkashObservationPolicy` and
`AkashConsensusVerified`. Stable IDs bind projection owner, provider type,
configured API endpoint and native owner address. Names and online observations
may change without changing identity.

Service URLs are descriptive text; the adapter never follows them. Contact email,
arbitrary attributes, capacity, precise location and unneeded nested payloads are
excluded. Name/description/online/service-URL predicates, joins, arbitrary metadata
queries, native writes, rollback/history and full ORM parity are not supported by
this read profile. They are not implied by the returned metadata.

This is indexer-reported metadata of a configured sample, not a complete network
listing, a shared finalized snapshot or independent chain-consensus verification.
An online flag is not an uptime guarantee. Initial discovery streams the first
three complete unique-owner records and closes the unread tail; it does not
certify that tail or claim a globally sorted network catalogue.

Limits are a two-MiB discovery read budget, 256-KiB buffered discovery record,
256-KiB point response, JSON depth 16 and a 30-second method deadline. The shared
process gate permits at most one request start per second; it is not an upstream
SLA or a distributed rate limiter. There are no automatic retries.

The actual compiled C#console has passed three-full/two-filtered execution
through HolonManager and deferred LINQ. HTTP/Mongo federation, hosted enrollment
and browser interaction have separate gates. See [current Akash evidence](../WIP/COSMIC%20ORM/AKASH_PUBLIC_SOURCE_STATUS.md)
for what has actually passed; this guide does not certify those later stages.
