# SOLID CQL host configuration

SOLID's CQL adapter reads encrypted native LDP resources through the shared
HolonManager/CQL planner. It supports an exact record key and, when explicitly
enabled, membership in up to eight canonical record GUIDs. It does not advertise
general SPARQL, text search or arbitrary metadata filters.

## Hosted sandbox source

The opt-in native runtime uses pinned Community Solid Server7.1.8 in the existing
WEB4 service. `OASIS_SANDBOX_SOLID=true` enables it; the authoritative DNA must
also set `OASIS.Sandbox.Enabled=true`. The native source lives in
`/data/COSMIC/SandboxSOLID`, separately from any retained private Pod.

`OASIS_SANDBOX_SOLID_BOOTSTRAP_JSON` supplies exactly three encrypted synthetic
public records for `http://localhost:13000/`. The CSS root-template initializer
creates these resources with public Read permission only. No account signup or
visitor credentials are forwarded to CSS. Hosting must expose WEB4's public port
only; CSS's base URL does not itself restrict the native listener's interfaces.

Bootstrap records an intent before initialization. It refuses interrupted,
unacknowledged state and source changes until explicitly reconciled. It verifies
native RDF and read-only permissions before acknowledging readiness. On normal
restart it verifies the existing source. The same process owner waits for native
IPFS/SOLID readiness before starting WEB4 and stops all owned children if a
required source exits.

DNA `OASIS.StorageProviders.SOLIDOASIS` requires:

```json
{
  "PodRoot": "http://localhost:13000/",
  "AuthenticationMode": "Anonymous",
  "EnableCqlRecordKeySets": true,
  "StorageKeyEnvironmentVariable": "OASIS_SOLID_STORAGE_KEY",
  "NativeImportContainers": ""
}
```

The key is supplied separately through the host's protected configuration.
Encrypted associated data includes each exact resource URI: do not move existing
ciphertext to a new Pod or replace its key. A different URI requires a deliberate
new export. The hosted sample is public synthetic data; anonymous access is an
explicit choice for this source, not a default for private Pods.

Enroll SOLID as query-only, with an explicit query tariff. Do not add it to the
default write/failover/replication lists merely to enable these read examples.
`Scripts/Sandbox/Publish-HostedSolidConfiguration.py` prepares protected snapshots;
its explicit `apply` mode observes and acknowledges each configuration value
without triggering deployment. Existing mutation intents block automatic replay.
No keys or tokens belong in a report or Git.

## Developer examples

The sandbox's provider discovery supplies actual source keys and ready requests.
Select SOLID, choose the three-record preset, then the two-record subset. The
paired Mongo preset can prepare two private snapshots using normal create-only
WEB4 writes; repeat preparation preserves existing records. Provider identity and
per-avatar visibility remain part of execution.

For C#, see [the commented walkthrough](../../Examples/CosmicCqlDemo/SolidPointWalkthrough.cs).
Given a configured/activated adapter, manager and caller identity:

```csharp
// keys are the native GUID strings from this configured Pod's sample selection.
await SolidPointWalkthrough.RunRecordSetAsync(
    manager, adapter, keys, actor, Console.Out);

// Change the selection to demonstrate a filtered subset, without scanning the Pod.
await SolidPointWalkthrough.RunRecordSetAsync(
    manager, adapter, keys.Take(2), actor, Console.Out);
```

The equivalent authenticated `POST /api/data/query-cql` request is:

```json
{
  "provider": "SOLIDOASIS",
  "orderBy": "None",
  "take": 8,
  "includeMetadata": true,
  "filters": [
    {
      "metadataPath": ["SolidRecordId"],
      "scalar": "String",
      "operator": "In",
      "value": ["<first canonical record GUID>", "<second canonical record GUID>"]
    }
  ]
}
```

Replace the placeholders with keys from the configured source. Responses use
`result.rows`. Membership selects native resources; it does not grant access to
private records. Unsupported shapes fail capability planning.

## Verification and recovery

Prepare/verify the retained export with
`Scripts/TestHosts/Prepare-HostedSolidSource.ps1`. This performs no Pod writes and
does not start the retained private server. Native runtime verification uses
`Scripts/TestHosts/Verify-HostedSolidRuntime.cjs` in the explicitly isolated cached
CSS probe described in the resource ledger.

Final-image verification and deployed browser acceptance remain required before
advertising a source as ready. Current releases, results, outstanding work and
resource paths are recorded in
[CURRENT_STATUS](../WIP/COSMIC%20ORM/CURRENT_STATUS.md) and its linked integration
report/cleanup ledger. The configuration guide describes supported behavior;
it is not proof that a particular deployment passed acceptance.
