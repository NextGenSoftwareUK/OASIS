# CQL over public Matrix room aliases

This read-only profile resolves a small configured catalogue of public aliases
through a Matrix homeserver. It does not log in, join rooms, read messages or
access encrypted content. The authenticated legacy Matrix provider is separate.

## Configuration

Retain one configuration and source owner; changing the owner changes the
projected holon identities. No password or access token belongs in this profile.

```json
{
  "schema": 1,
  "apiUrl": "https://matrix.org/",
  "aliases": [
    "#matrix:matrix.org",
    "#synapse:matrix.org",
    "#element:matrix.org"
  ],
  "sourceOwner": "<one retained nonempty GUID>"
}
```

The profile accepts one to eight distinct ASCII aliases with DNS homeservers,
up to255 characters each. The API URL must be an anonymous HTTPS root without
credentials, query or fragment. Production configuration uses the shared native
connection guard; returned routing servers are data and are never fetched.

## C# example

Reference `NextGenSoftware.OASIS.API.Providers.MatrixOASIS`. Use the normal
application `HolonManager` and the caller's avatar ID. Explicit scope selects
this read profile; it does not change the default write or replication provider.

```csharp
using var source = MatrixPublicRoomMetadataSource.FromJson(
    await File.ReadAllTextAsync(configurationPath));

var activation = await source.ActivateQuerySourceAsync(cancellationToken);
if (activation.IsError)
    throw new InvalidOperationException(activation.Message, activation.Exception);

string[] aliases = { "#matrix:matrix.org", "#synapse:matrix.org" };

var scope = CosmicQueryScope.Named(ProviderType.MatrixOASIS).Result;
var access = CosmicQueryReadAccess.OwnerOrPublic(avatarId).Result;

// Contains plans a bounded native key selection. It does not scan rooms.
var selection = CosmicQuery<Holon>.Create(scope)
    .WithReadAccess(access)
    .Where(room => aliases.Contains(
        CosmicQueryMetadata.Value<string>(room, "MatrixRoomAlias"))).Result;

var deferred = CosmicLinq.Create(
    manager, source, selection, maximumResults: 40);

if (deferred.IsError)
    throw new InvalidOperationException(deferred.Message);

var result = await deferred.Result.ToCosmicListAsync(cancellationToken);
if (result.IsError)
    throw new InvalidOperationException(result.Message, result.Exception);

foreach (var room in result.Result)
    Console.WriteLine($"{room.Name} -> {room.MetaData["MatrixRoomId"]}");
```

Equality on `MatrixRoomAlias` also selects one configured alias. Membership,
`Skip` and `Take` operate over the bounded selection. Every selected response
must validate before a page is published, including an empty page. Unsupported
predicates fail capability planning; arbitrary name/description searches and
room-directory scans are not implemented by this profile.

The runnable example is `Examples/CosmicCqlDemo/MatrixPublicWalkthrough.cs`:

```powershell
dotnet Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --matrix-public LocalState/COSMIC/HostedMatrixPublicSourceCurrent/source.json
```

Build the example using the repository's provider build lease and bounded build
workers. This read-only walkthrough requires exactly three configured aliases;
it runs the full3-key and filtered2-key selections through HolonManager/LINQ.
It creates no Mongo copies. The qualification runner is a single journalled
acceptance action, not the normal interactive demo command.

## Projected fields and guarantees

| Field | Meaning |
|---|---|
| `Name` / `MatrixRoomAlias` | Exact configured alias |
| `MatrixRoomId` | Opaque server-reported room ID, including domain-less v12 IDs |
| `MatrixServersJson` | Complete bounded routing list as JSON text; never fetched |
| `MatrixObservationPolicy` | `server-reported-alias-resolution` |
| `Description` | Plain-text alias-resolution summary |

Each alias is requested once per read, with a15-second request timeout and
30-second total selection deadline. Each response is limited to64KiB, with
256KiB total, at most4096 routing entries and bounded JSON depth. Errors,
cancellation and incomplete responses return explicit errors without partial
rows, retries or alternate endpoints. Native writes/deletes are unsupported.

Aliases can be reassigned. These observations do not prove a common snapshot,
message history, room state, membership, user identity or federation consensus.
Different aliases can resolve to the same room; projected identity represents
the configured alias, not a globally deduplicated room entity.

## Hosted demo status

Hosted activation/admission, HTTP native3/2, federated Mongo5/4, privacy,
replication and sandbox-card/browser gates are separate acceptance work. Do not
infer that they pass from this local source or the anonymous native qualifier.
See the maintained [Matrix status](../WIP/COSMIC%20ORM/MATRIX_PUBLIC_SOURCE_STATUS.md).

Official contracts: [alias resolution](https://spec.matrix.org/latest/client-server-api/#get_matrixclientv3directoryroomroomalias)
and [room ID changes](https://matrix.org/blog/2025/09/17/matrix-v1.16-release/).
