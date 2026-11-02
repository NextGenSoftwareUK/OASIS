# Nostr signed public events with CQL

This read-only adapter queries a small retained catalogue of immutable Nostr kind-1 event IDs. It verifies each event's canonical NIP-01 SHA256 hash and BIP-340 signature before returning a holon. No Nostr login, signing key, relay publication or account discovery is needed.

Local native CQL and the actual C# walkthrough pass three-event and two-event selections. API read admission/local HTTP native3/2/Mongo5/4/visitor privacy and all four prepared presets now pass. Hosted functional3/2/Mongo5/4, repeat0new/2existing, visitor0/3 and matching SQLite/Neo4j replicas2/2 now pass for the original cohort. Page publication/browser remain pending; earlier intermittent read errors are unresolved. Check the [current evidence](../WIP/COSMIC%20ORM/NOSTR_PUBLIC_SOURCE_STATUS.md) before presenting a hosted demo.

## Retain a catalogue

Reuse the existing qualified file on the agent machine:

```text
LocalState/COSMIC/HostedNostrPublicSourceCurrent/source.json
```

On another developer machine, retain one configuration with these exact fields:

```json
{
  "schema": 1,
  "relayUrl": "wss://relay.damus.io/",
  "sourceOwner": "00000000-0000-0000-0000-000000000123",
  "eventIds": [
    "919bebed5ec73504f67cc516a58bf5ae6285d63323f086becd0c3ac6f5f80602",
    "27a99026c487b5d756157901ca41b2d093d3a2d63a27112fe5a874c4c9f123e3",
    "0708143a224525037e712987c963567fecd6027b6d496cb1c16dc47ff5b8ec13"
  ]
}
```

The owner is an illustrative public projection identity, not a credential. Choose your own stable nonempty GUID once. Keep the same owner and IDs between runs. A relay may prune or refuse an old event; the source returns an error if the complete selection is unavailable. It never replaces missing events with newer notes or tries another relay.

## Run the commented C# demo

Reuse .NET10 and the existing checkout. Build producer before consumer under the repository build lease, with bounded workers. Then run:

```powershell
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
  --nostr-public LocalState/COSMIC/HostedNostrPublicSourceCurrent/source.json
```

The [walkthrough](../../Examples/CosmicCqlDemo/NostrPublicWalkthrough.cs) activates the explicit adapter, runs three-event and two-event selections through shared HolonManager/LINQ and prints event IDs, public keys and original Unix timestamps. It does not print untrusted note bodies, publish events or create Mongo copies.

## Write a bounded LINQ query

```csharp
// Select immutable event IDs captured during qualification.
// This is not a search over the relay's entire history.
var selected = new[]
{
    "919bebed5ec73504f67cc516a58bf5ae6285d63323f086becd0c3ac6f5f80602",
    "27a99026c487b5d756157901ca41b2d093d3a2d63a27112fe5a874c4c9f123e3"
};

var source = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.NostrOASIS).Result)
    .Where(row => selected.Contains(
        CosmicQueryMetadata.Value<string>(row, "NostrEventId")))
    .Result;

// Supply the activated read-only adapter explicitly.
// Check OASISResult errors as shown in the shared walkthrough.
var query = CosmicLinq.Create(manager, adapter, source, maximumResults: 8).Result;

var rows = await query.ToCosmicListAsync(cancellationToken);
```

Exact event-ID equality and membership of up to eight configured lowercase 64-character IDs are supported. Unknown IDs fail before native I/O; unsupported name/text predicates or ordering fail planning. Offset/limit applies only after the complete selected native set has been validated. A bad or missing event fails the query, including an otherwise empty output page.

## HTTP request shapes (hosted enrollment pending)

After explicit API admission, use an authenticated OASIS demo session and a fresh `Idempotency-Key` for each request. Keep the bearer private. No Nostr credentials are needed.

Send this two-event selection to `POST /api/data/query-cql`:

```json
{
  "provider": "NostrOASIS",
  "maximumDurationMilliseconds": 30000,
  "orderBy": "None",
  "take": 25,
  "includeMetadata": true,
  "filters": [
    {
      "metadataPath": [
        "NostrEventId"
      ],
      "scalar": "String",
      "operator": "In",
      "value": [
        "919bebed5ec73504f67cc516a58bf5ae6285d63323f086becd0c3ac6f5f80602",
        "27a99026c487b5d756157901ca41b2d093d3a2d63a27112fe5a874c4c9f123e3"
      ]
    }
  ]
}
```

For `POST /api/data/query-cql/federation`, name both providers explicitly:

```json
{
  "scope": "Named",
  "providers": [
    "MongoDBOASIS",
    "NostrOASIS"
  ],
  "source": {
    "orderBy": "None",
    "take": 25,
    "includeMetadata": true,
    "filters": [
      {
        "metadataPath": [
          "NostrEventId"
        ],
        "scalar": "String",
        "operator": "In",
        "value": [
          "919bebed5ec73504f67cc516a58bf5ae6285d63323f086becd0c3ac6f5f80602",
          "27a99026c487b5d756157901ca41b2d093d3a2d63a27112fe5a874c4c9f123e3"
        ]
      }
    ]
  },
  "identityMode": "PreserveCopies",
  "maximumProviderAttempts": 2,
  "maximumDurationMilliseconds": 30000,
  "take": 25
}
```

A query creates no copies. Once the hosted preparation gate is verified, prepare two private Mongo identity/name/description/provenance copies once and reuse them. `PreserveCopies` then returns the two public events plus your two private copies; the three-event preset returns five rows. Other visitors cannot see your private copies. Generic browser copies do not retain the full native signed-event metadata. Neither pairing nor a valid native signature establishes a common snapshot, fresh content or an OASIS identity.

## Data and trust

`Name` is a stable display label derived from the event ID. `Description` and `NostrContent` preserve the signed note text. Treat that text as untrusted data and escape it when displaying HTML.

Metadata includes `NostrEventId`, `NostrPublicKey`, `NostrCreatedAt`, `NostrKind`, `NostrContent`, `NostrTagsJson`, `NostrSignature` and `NostrObservationPolicy="signed-event-relay-observed"`. Timestamps are exact decimal Unix-second strings; kinds remain explicit. The original signed tags/content are retained without fetching embedded links or images.

A valid signature establishes integrity for the stated public key. It does not establish real-world identity, OASIS authorization, freshness, network consensus, relay completeness or a shared snapshot. This profile uses kind-1 events, not replaceable kind-0 profile records.

Each query uses one anonymous guarded WSS connection and one bounded REQ subscription, waits for matching EOSE and closes the socket. Fragmented text messages are reassembled within a 20-second deadline, 64-frame, 64KiB-message and 256KiB-total budget. Unexpected subscriptions, notices/auth demands, invalid UTF8, early closure or incomplete sets fail without partial rows. There is no automatic retry or relay failover in this source. Native saves/deletes are rejected.

## Verification

[Event boundary tests](../../Providers/Social/TestProjects/NextGenSoftware.OASIS.API.Providers.NostrOASIS.UnitTests/NostrPublicEventVerificationTests.cs) cover all 19 official BIP-340 public verification vectors and signed-event schema, hash, tampering and Unicode behavior. [CQL/transport tests](../../Providers/Social/TestProjects/NextGenSoftware.OASIS.API.Providers.NostrOASIS.UnitTests/NostrPublicEventMetadataTests.cs) cover complete selections, paging, cancellation, capability rejection, read-only behavior and bounded fragmented transport.

Primary contracts: [NIP-01](https://github.com/nostr-protocol/nips/blob/master/01.md), [BIP-340](https://github.com/bitcoin/bips/blob/master/bip-0340.mediawiki). Exact local/native/hosted gates and retained failure evidence are recorded in the linked status report.

Hosted checkpoint (2 November): native3/2, Mongo5/4 and matching SQLite/Neo4j replicas2/2 passed for the original visitor, and the original cohort later completed repeated-preparation/privacy through an explicit read-only continuation. The earlier intermittent query failure remains an unresolved reliability issue. The live page card remains unpublished. See the current evidence before using this as a hosted demo. No complete hosted reliability claim.

Operator diagnostics: event4831 records known failure category, activation/query phase, transport stage and typed HTTP status/WebSocket error when available. Raw exception text, native URLs, headers, tokens and payloads are excluded. Public errors remain generic. Diagnostic deployment/live-cause verification is tracked in the current evidence; instrumentation alone does not resolve the intermittent read.
