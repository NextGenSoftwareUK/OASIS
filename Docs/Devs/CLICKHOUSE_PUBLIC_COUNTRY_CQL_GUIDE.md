# ClickHouse public country queries with CQL

This read-only profile projects countries from the public ClickHouse playground
into holons. It uses the shared HolonManager planner and deferred LINQ execution.
It is separate from the provider's owned-database storage implementation.

## Scope

The initial catalogue is France, Germany and Italy in `blogs.countries`.
Each record exposes its country name and polygon count. Raw geometry is excluded.
Counts stay canonical UInt64 decimal strings, without floating-point conversion.

`ClickHouseCountryName` supports string equality, bounded IN and paging. The
shared native-key strategy currently accepts printable ASCII characters33-126,
without spaces, at most128characters and eight keys. Unsupported configuration
keys fail immediately. Unicode/spaced country keys and arbitrary SQL are not
advertised by this profile. The query values use typed HTTP String parameters;
they are never concatenated into SQL text.

## Configure once

Retain one projection owner and catalogue so native projection IDs stay stable.
The accepted local source is outside Git at:

`LocalState/COSMIC/HostedClickHousePublicSourceCurrent/source.json`

Its schema contains `schema:1`, an HTTPS root `apiUrl`, `countries` (one to eight
unique supported names) and a nonempty `sourceOwner` GUID. The public source uses
`https://play.clickhouse.com/` and the operator's documented read-only `explorer`
user with an empty password. Do not put owned-database credentials in this profile.

## Run the developer walkthrough

Build `Examples/CosmicCqlDemo/CosmicCqlDemo.csproj` using the repository's normal
bounded build workflow, then:

```powershell
dotnet Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --clickhouse-public `
    LocalState/COSMIC/HostedClickHousePublicSourceCurrent/source.json
```

See `ClickHousePublicWalkthrough.cs` for source activation and manager setup,
and `NativeKeySetWalkthrough.cs` for the shared query implementation. The example
runs three keys, then a two-key selection, and prints polygon counts21/22/29
from the original qualified observation. Remote data can change; these values
are evidence of that run, not a permanent country geometry specification.

The core query is:

```csharp
// Select only the explicitly enrolled provider.
var scope = CosmicQueryScope.Named(ProviderType.ClickHouseOASIS);
if (scope.IsError)
    throw new InvalidOperationException(scope.Message);

// Contains becomes a bounded, typed native membership predicate.
var countries = new[] { "France", "Germany", "Italy" };
var selection = CosmicQuery<Holon>.Create(scope.Result)
    .Where(row => countries.Contains(
        CosmicQueryMetadata.Value<string>(row, "ClickHouseCountryName")));

if (selection.IsError)
    throw new InvalidOperationException(selection.Message);

// Use the activated source and existing HolonManager; handle result errors.
var deferred = CosmicLinq.Create(manager, source, selection.Result, maximumResults:40);
if (deferred.IsError)
    throw new InvalidOperationException(deferred.Message);

var loaded = await deferred.Result.ToCosmicListAsync(cancellationToken);
if (loaded.IsError)
    throw new InvalidOperationException(loaded.Message);
```

The runnable shared walkthrough also applies caller read access before planning.

## Failure and evidence boundaries

Activation verifies the exact country-table schema and read-only user. Each
query verifies public read identity and the complete selected key set. Missing,
duplicate, foreign, oversized, malformed or exception-bearing native responses
fail without partial rows. HTTP requests have15second deadlines,256KiB body
limits and one-request/second pacing. There is no implicit retry. Save/delete
return `CQL_READ_ONLY_SOURCE` before network activity or input mutation.

The original native source qualification and compiled manager/LINQ/CLI3/2run
passed. Local HTTP/Mongo3/2+5/4/privacy checks and server tariff/admission controls
now pass. Original hostednative3/2, Mongo5/4, repeatpreparation/privacy and
exactSQLite/Neo4jreplicas also pass the independent audit. Sandboxcard publication
and browser interaction remain separate: see `Docs/WIP/COSMIC ORM/CLICKHOUSE_PUBLIC_SOURCE_STATUS.md`.
Preserve original source owners/intents/ACKs; do not replay completed fixtures.

[Official free playground and quotas](https://github.com/ClickHouse/clickhouse-docs/blob/89036285e33b88b424ebc397cce3cb70e9c2b5a9/docs/getting-started/playground.md)
