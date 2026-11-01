# CQL over public Azure Blob properties

This read-only profile queries real public Azure Blob Storage using the Azure SDK. It needs no Azure account, SAS or subscription. It is separate from Azure Cosmos DB and owned Azure holon persistence.

## What the demo shows

The retained catalogue contains three objects from Microsoft Azure Open Datasets: one US county-population Parquet file and two completion-marker objects. CQL selects all three or a two-key subset. Azure returns object properties through HEAD requests; the demo never downloads or queries the census rows inside the Parquet file.

The profile supports equality or bounded membership on `AzureBlobKey`, with `Skip`/`Take`. One query can select at most eight configured keys. Names can contain up to 512 characters. General Name/Description predicates, arbitrary metadata searches, ordering, aggregates and object-body queries are not admitted.

Returned metadata includes account, container, exact blob name, ETag, last modification time, content type and byte length. Byte length is a decimal string to preserve values beyond JavaScript integer precision. Each read observes that object's current properties; the objects do not share a transactional snapshot.

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
    LocalState/COSMIC/HostedAzureBlobPublicSourceCurrent/source.json
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

The example above illustrates the schema; use the verified retained catalogue for the actual demo. Arbitrary endpoints, credentials, SAS fields, duplicate properties and noncanonical path segments are rejected. Public source permissions and an explicit query tariff must also be enrolled; configuration alone does not certify a live service.

A leaf request to `api/data/query-cql` uses `provider: AzureStorageOASIS`, `orderBy: None` and an `In` string filter at metadata path `["AzureBlobKey"]`. A federated request to `api/data/query-cql/federation` names `["MongoDBOASIS", "AzureStorageOASIS"]` and supplies the same source filter. The two private Mongo snapshots are explicit copies, not native Azure writes; `PreserveCopies` exposes both providers' provenance.

Hosted publication and live sandbox acceptance are tracked separately in [the current Azure report](../WIP/COSMIC%20ORM/AZURE_PUBLIC_BLOB_STATUS.md). Do not infer their completion from a local test. Hosted Cosmos DB remains deferred.

## Verification and limits

The source tests cover anonymous HEAD-only access, bounded selection, missing objects, denied requests, cancellation, paging, precision and complete failure rather than partial rows. The native tests exercise the actual SDK and documented manager/LINQ walkthrough. The local HTTP gate additionally covers private Mongo copies, owner isolation and provider receipts.

SDK retries are disabled. Container listing is not a query strategy; the original bounded metadata listing was only used to choose the fixed catalogue. Source saves/deletes, automatic replication from Azure, provider-wide discovery and full ORM parity are outside this profile.

Microsoft references: [Azure Open Datasets county-population source](https://learn.microsoft.com/en-us/azure/open-datasets/dataset-us-population-county), [Get Blob Properties](https://learn.microsoft.com/en-us/rest/api/storageservices/get-blob-properties).
