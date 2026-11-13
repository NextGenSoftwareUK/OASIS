# SubqueryOASIS

SubQuery - open-source blockchain data indexing framework.

Part of the [OASIS HOT-Swappable Provider Architecture](https://oasisomniverse.one).

## Bounded dictionary CQL profile

`SubqueryDictionaryBlockQuerySource` is a separate read-only representation of
historical Transfer-topic block matches from the fixed SubQuery V1 Arbitrum
dictionary. The reviewed catalogue contains heights400000000,400000002,400000003.
ERC20 and ERC721 share this event signature; matches do not identify token type,
full logs, transaction bodies or independently verified block headers/finality.

The common CQL planner accepts exact mapped holon ID equality/membership, with
one to eight explicitly configured positive heights. Each native request binds
one height, topic, ascending block-height ordering and block-height distinctness.
Reported genesis and index coverage must match before publication. Unknown IDs,
GraphQL errors, malformed/oversized/duplicate replies and failed selected reads
produce explicit errors without partial results or transport retries. Unsupported
scans, historical versions and native source writes remain errors.

```csharp
using var source = SubqueryDictionaryBlockQuerySource.ForReviewedTeachingCatalogue(owner);
var activated = await source.ActivateQuerySourceAsync(cancellationToken);
if (activated.IsError)
    throw new InvalidOperationException(activated.Message, activated.Exception);

var ids = new long[] { 400000000, 400000002, 400000003 }
    .Select(height => SubqueryDictionaryBlockQuerySource.LogicalId(owner, height)).ToArray();
var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SubqueryOASIS).Result)
    .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(owner, ProviderType.SubqueryOASIS, ids).Result).Result
    .Where(row => ids.Contains(row.Id));
if (query.IsError)
    throw new InvalidOperationException(query.Message);
var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.SubqueryOASIS,
    new[] { new CosmicQueryTarget(ProviderType.SubqueryOASIS, source.QueryCapabilities) });
if (plan.IsError)
    throw new InvalidOperationException(plan.Message);
var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancellationToken);
if (result.IsError)
    throw new InvalidOperationException(result.Message, result.Exception);
```

Use the explicit `CQL_SUBQUERY_DICTIONARY_QUERY_SOURCE_OWNER` WEB4 setting only
with a reviewed canonical owner, separate query-only permission/read tariff,
public-ID grant and presets. Registration alone neither activates nor enrolls
the source. Task integration is undergoing cloud qualification; hosted acceptance
and browser verification remain open. Do not apply task settings to a release
that has not passed its exact source/image gates.

[Current evidence and remaining gates](../../../Docs/WIP/COSMIC%20ORM/SUBQUERY_PUBLIC_SOURCE_STATUS.md).

## Usage

Register in your `OASIS_DNA.json`:

```json
{
  "DefaultProviders": ["SubqueryOASIS"]
}
```

## Links

- [OASIS Website](https://oasisomniverse.one)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS)
- [NuGet](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.SubqueryOASIS/)
