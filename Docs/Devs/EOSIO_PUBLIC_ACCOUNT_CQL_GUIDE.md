# EOSIO / Vaulta public account CQL

This anonymous read-only profile projects a configured native EOSIO/Vaulta account catalogue into public holons. It is separate from the credentialed EOSIO storage provider and from Vaulta EVM. It cannot sign transactions, save native records or become a write/replication authority.

## First setup on another developer machine

No EOSIO account, wallet, funding or local chain node is required. Reuse a suitable existing checkout and .NET10 installation. Create one retained anonymous catalogue; keep its owner stable and never overwrite an existing configuration:

```powershell
$cataloguePath = Join-Path $PWD 'LocalState/COSMIC/HostedEosioPublicSourceCurrent/source.json'
New-Item -ItemType Directory -Force -Path (Split-Path $cataloguePath) | Out-Null

$catalogue = @{
    schema = 1
    apiUrl = 'https://vaulta.greymass.com/'
    chainId = 'aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906'
    accounts = @('eosio', 'eosio.token', 'eosio.msig')
    sourceOwner = [Guid]::NewGuid().ToString('D')
} | ConvertTo-Json

# CreateNew refuses replacement, preserving projected identities across runs.
$stream = [IO.File]::Open($cataloguePath, [IO.FileMode]::CreateNew)
try {
    $stream.Write([Text.Encoding]::UTF8.GetBytes($catalogue))
    $stream.Flush($true)
} finally {
    $stream.Dispose()
}
```

This creates local public configuration only; it does not create blockchain accounts or save holons. The normal application may query this catalogue repeatedly. The agent qualification/acceptance runners have stricter journal rules to preserve their original test cohorts.

## Run the C# demo

Reuse the retained public source configuration in this worktree:

```powershell
dotnet run --project Examples/CosmicCqlDemo --no-build -- --eosio-public LocalState/COSMIC/HostedEosioPublicSourceCurrent/source.json
```

[Walkthrough](../../Examples/CosmicCqlDemo/EosioPublicWalkthrough.cs) uses [the shared manager/LINQ example](../../Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs). It activates the explicit source, queries three selected accounts, then two. No source data or Mongo copies are created. Build producer before consumer using the repository build lease and bounded build settings.

## Query explicitly selected accounts

```csharp
// Select the native account keys; this is not an unrestricted chain scan.
var accounts = new[] { "eosio", "eosio.token" };
var scope = CosmicQueryScope.Named(ProviderType.EOSIOOASIS).Result;

var query = CosmicQuery<Holon>.Create(scope)
    .Where(row => accounts.Contains(
        CosmicQueryMetadata.Value<string>(row, "EOSIOAccountName")))
    .Result;

// Supply the activated adapter explicitly. Provider scope does not change
// the canonical storage provider or HyperDrive's write authority.
var linq = CosmicLinq.Create(manager, adapter, query, maximumResults: 8).Result;
var rows = await linq.ToCosmicListAsync(cancellationToken);
```

Use OASISResult error handling in application code; the abbreviated example focuses on the query. Exact key equality and bounded key membership are admitted; unsupported predicates fail planning. At most eight unique configured names are allowed, each at most thirteen native characters. Selection outside the catalogue is rejected without native I/O. Paging applies only after complete validation; failures never publish partial rows.

## HTTP and live CQL tab

The sandbox CQL tab obtains the admitted presets from the API. Choose `EOSIOOASIS`, then a public native preset or its Mongo paired preset. Hosted availability is shown by discovery; consult the linked current evidence before claiming the rollout is complete. Use your authenticated demo session for HTTP requests and a fresh `Idempotency-Key`; never share its bearer token.

Native filtered request to `POST /api/data/query-cql`:

```json
{
  "provider": "EOSIOOASIS",
  "orderBy": "None",
  "take": 8,
  "includeMetadata": true,
  "filters": [
    {
      "metadataPath": ["EOSIOAccountName"],
      "scalar": "String",
      "operator": "In",
      "value": ["eosio", "eosio.token"]
    }
  ]
}
```

For `POST /api/data/query-cql/federation`, select only Mongo and this explicit native source. The CQL tab's preparation action creates two private Mongo copies once and reuses them; native source data stays read-only. An HTTP federation query itself never creates those copies:

```json
{
  "scope": "Named",
  "providers": ["MongoDBOASIS", "EOSIOOASIS"],
  "identityMode": "PreserveCopies",
  "maximumProviderAttempts": 2,
  "take": 8,
  "source": {
    "orderBy": "None",
    "take": 8,
    "includeMetadata": true,
    "filters": [
      {
        "metadataPath": ["EOSIOAccountName"],
        "scalar": "String",
        "operator": "In",
        "value": ["eosio", "eosio.token"]
      }
    ]
  }
}
```

The native response contains two public rows. The paired response contains those two plus your two matching private Mongo copies after preparation; another visitor cannot see your copies. Generic browser preparation retains identity/name/description/provenance, not all native metadata. Source errors fail the paired query rather than silently substituting Mongo results.

## Configuration and semantics

`EosioPublicAccountMetadataSource.FromJson` requires exactly `schema` (1), `apiUrl` (anonymous HTTPS root), `chainId` (native mainnet), `accounts` and nonempty `sourceOwner`. Unknown/duplicate fields and credential fields are rejected. The production factory uses NativeConnectionGuard; injected HttpClient is for controlled transports/tests. Keep sourceOwner stable to preserve projected IDs.

The native chain ID is `aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906`. The qualified public endpoint is `https://vaulta.greymass.com/`. Every query checks native `get_info` identity before reading only its selected `get_account` keys. Responses are bounded to256KiB; cancellation and HTTP/native errors fail the entire result.

Metadata includes `EOSIOAccountName`, `EOSIOChainId`, `EOSIOHeadBlockNum`, `EOSIOCreated` and `EOSIORamQuota`. Numeric values remain exact decimal strings; no floating-point conversion. RAM quota is signed64-bit native data. These are latest state per account response, not a common finalized snapshot or an independently verified consensus result. Do not infer current freshness from the identity response: its reported head can lag the account responses. No token balance is inferred from absent fields.

## Verification and remaining gates

[Controlled tests](../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.UnitTests/EosioPublicAccountMetadataTests.cs) cover complete selection, precision, identity, shape/size errors, cancellation, budgets and write rejection. [Current evidence and remaining integration](../WIP/COSMIC%20ORM/EOSIO_PUBLIC_SOURCE_STATUS.md) distinguishes native qualification from manager/CQL, hosted HTTP, Mongo federation and browser proof. Query-only API configuration is `CQL_EOSIO_PUBLIC_SOURCE_JSON`; independent read permission and an explicit free read tariff are still required. Local HTTP native3/2/Mongo5/4/privacy checks and all four prepared presets now pass. Hosted HTTP acceptance now passes: native3/2, Mongo5/4, repeat0new/2existing, visitor privacy0/3 and matching replicas2/2 each on SQLite/Neo4j. Both normal private saves are version1 with no pending preparation intents. The sandbox card is published. Browser interaction is still unverified; HTTP and public asset matching do not establish it.

Native references: [Vaulta reading state](https://docs.vaulta.com/docs/latest/web-applications/reading-state/), [Chain API](https://docs.eosnetwork.com/apis/spring/latest/chain.api/).
