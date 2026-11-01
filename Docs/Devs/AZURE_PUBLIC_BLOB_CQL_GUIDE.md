# CQL over public Azure Blob properties

This read-only profile queries real public Azure Blob Storage using the Azure SDK. It needs no Azure account, SAS or subscription. It is separate from Azure Cosmos DB and owned Azure holon persistence.

## What the demo shows

The retained catalogue contains three objects from Microsoft Azure Open Datasets: one US county-population Parquet file and two completion-marker objects. CQL selects all three or a two-key subset. Azure returns object properties through HEAD requests; the demo never downloads or queries the census rows inside the Parquet file.

The profile supports equality or bounded membership on `AzureBlobKey`, with `Skip`/`Take`. One query can select at most eight configured keys. Names can contain up to 512 characters. General Name/Description predicates, arbitrary metadata searches, ordering, aggregates and object-body queries are not admitted.

Returned metadata includes account, container, exact blob name, ETag, last modification time, content type and byte length. Byte length is a decimal string to preserve values beyond JavaScript integer precision. Each read observes that object's current properties; the objects do not share a transactional snapshot.

## First local setup: one reusable catalogue

Use an initialized OASIS checkout with your existing .NET 10 SDK and dependencies. The catalogue below identifies public objects and assigns a stable local mapping owner; it contains no credentials. Create it once, then preserve it between runs.

```powershell
$sourcePath = Join-Path $PWD 'LocalState/COSMIC/AzureBlobDemo/source.json'

if (Test-Path -LiteralPath $sourcePath) {
    throw 'Reuse your existing catalogue; do not replace its owner.'
}

New-Item -ItemType Directory -Force (Split-Path $sourcePath) | Out-Null

$source = @{
    schema = 1
    account = 'azureopendatastorage'
    container = 'censusdatacontainer'
    sourceOwner = [Guid]::NewGuid().ToString('D')
    keys = @(
        'release/us_population_county/year=2010/part-00178-tid-926394737839939592-51ecde30-440a-40fd-9b41-831814678ab5-1919151.c000.snappy.parquet'
        'release/us_population_county/year=2010/_committed_926394737839939592'
        'release/us_population_county/year=2010/_SUCCESS'
    )
}

$source | ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath $sourcePath -Encoding utf8NoBOM
```

Build once using the existing repository lease; do not nest it inside another lease:

```powershell
& ./Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner 'azure-blob-demo' -Operation {
    dotnet build Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
        /m:2 /nr:false /p:UseSharedCompilation=false

    if ($LASTEXITCODE -ne 0) { throw 'Demo build failed.' }
}
```

This creates a small local configuration file, not a cloud account or dataset copy. Its local projection IDs are scoped to this stable owner; they need not equal the hosted profile's projection IDs.

## C# demonstration

The readable entry point is [AzureBlobPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/AzureBlobPublicWalkthrough.cs). It activates the anonymous source, creates a WEB4 V2 manager, then runs the same shared LINQ walkthrough used by other native-key providers:

```csharp
// These are existing, explicitly configured Azure blob names.
var selectedKeys = catalogueKeys.Take(2).ToArray();

var scope = CosmicQueryScope.Named(ProviderType.AzureStorageOASIS).Result;
var access = CosmicQueryReadAccess.OwnerOrPublic(avatarId).Result;

var sourceQuery = CosmicQuery<Holon>.Create(scope)
    .WithReadAccess(access)
    .Where(holon => selectedKeys.Contains(
        CosmicQueryMetadata.Value<string>(holon, "AzureBlobKey")))
    .Result;

// The manager plans and dispatches native Azure properties reads.
// Unsupported operators produce an OASISResult error; no container scan is substituted.
var query = CosmicLinq.Create(manager, azureSource, sourceQuery).Result;
var result = await query.ToCosmicListAsync(cancellationToken);
```

After building the existing demo, run:

```powershell
dotnet Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --azure-blob-public `
    LocalState/COSMIC/AzureBlobDemo/source.json
```

Reuse the retained catalogue. The command does not create an account, source object or Mongo fixture. It returns 3 full / 2 filtered records. Inspect `IsError` before using an `OASISResult.Result` in application code.

## HTTP and Mongo federation

Host enrollment uses `CQL_AZURE_BLOB_PUBLIC_SOURCE_JSON`. Its exact schema is:

```json
{
  "schema": 1,
  "account": "azureopendatastorage",
  "container": "censusdatacontainer",
  "keys": ["the-explicit-native-blob-name"],
  "sourceOwner": "11111111-1111-1111-1111-111111111111"
}
```

The example above illustrates the schema; use the exact three-object catalogue from the setup above for the actual demo. Arbitrary endpoints, credentials, SAS fields, duplicate properties and noncanonical path segments are rejected. Public source permissions and an explicit query tariff must also be enrolled; configuration alone does not certify a live service.

A leaf request to `api/data/query-cql` uses `provider: AzureStorageOASIS`, `orderBy: None` and an `In` string filter at metadata path `["AzureBlobKey"]`. A federated request to `api/data/query-cql/federation` names `["MongoDBOASIS", "AzureStorageOASIS"]` and supplies the same source filter. The two private Mongo snapshots are explicit copies, not native Azure writes; `PreserveCopies` exposes both providers' provenance.

Hosted acceptance passes at WEB4 release `bccef53d6` / Core `d133cc2`, with settings rollout `8e382a80-86ec-4d1d-8709-ca459f9e9781`: native3/2, Mongo-paired5/4, repeat reuse, visitor privacy and exact SQLite/Neo4j replicas. Browser interaction remains an open gate. See [the current Azure report](../WIP/COSMIC%20ORM/AZURE_PUBLIC_BLOB_STATUS.md) for hashes and immutable cohort evidence. Hosted Cosmos DB remains deferred.

## Live sandbox workflow

Open the [WEB4 sandbox](https://web4.oasisomniverse.one/web4-sandbox.html), start a temporary session and select `AzureStorageOASIS` in the CQL tab. The provider supplies ready three-object and two-object presets.

For a Mongo-paired preset, prepare your two private snapshots through the normal WEB4 save path, then run the query. The full preset returns five physical copies and the subset returns four, with each provider identified. Repeating setup reuses the same copies. An uncertain save retains its operation identity and must be reconciled before another attempt; never blindly resubmit it.

The Azure side remains anonymous and read-only. These presets query object properties, not census rows or arbitrary blobs; use the configured keys. A visitor's Mongo snapshots stay private, even though the Azure source itself is public.

## Verification and limits

The source tests cover anonymous HEAD-only access, bounded selection, missing objects, denied requests, cancellation, paging, precision and complete failure rather than partial rows. The native tests exercise the actual SDK and documented manager/LINQ walkthrough. The local HTTP gate additionally covers private Mongo copies, owner isolation and provider receipts.

SDK retries are disabled. Container listing is not a query strategy; the original bounded metadata listing was only used to choose the fixed catalogue. Source saves/deletes, automatic replication from Azure, provider-wide discovery and full ORM parity are outside this profile.

Microsoft references: [Azure Open Datasets county-population source](https://learn.microsoft.com/en-us/azure/open-datasets/dataset-us-population-county), [Get Blob Properties](https://learn.microsoft.com/en-us/rest/api/storageservices/get-blob-properties).
