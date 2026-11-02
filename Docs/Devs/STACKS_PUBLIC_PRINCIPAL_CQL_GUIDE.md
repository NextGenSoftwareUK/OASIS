# CQL over public Stacks principal state

`StacksPublicPrincipalMetadataSource` is an anonymous, read-only CQL profile
under `BlockStackOASIS`. It reads public Stacks mainnet principal state through
the node RPC API. It uses the shared HolonManager planner and deferred LINQ
execution. The legacy Gaia/user-directory implementation is separate.

## Configuration

Retain one configuration and source owner. The owner participates in stable
projected holon identities; do not generate a replacement for each demo run.

```json
{
  "schema": 1,
  "apiUrl": "https://api.hiro.so/",
  "principals": [
    "SP000000000000000000002Q6VF78",
    "SP000000000000000000002Q6VF78.pox-4",
    "SP000000000000000000002Q6VF78.pox-5"
  ],
  "sourceOwner": "<one retained nonempty GUID>"
}
```

The catalogue contains one to eight distinct principals. Addresses must use
canonical mainnet C32 versions and valid checksums. A contract suffix is an
ASCII letter followed by up to39letters/digits/hyphens/underscores. The full
principal is bounded to128characters. The API URL is an anonymous HTTPS root
without credentials, a query or fragment. Unknown configuration fields fail.

The host factory uses the shared native connection guard, disables cookies and
does not accept signer/account/API-key configuration. No endpoint or catalogue
is selected from the user's query body.

## Runnable C# walkthrough

`Examples/CosmicCqlDemo/StacksPublicWalkthrough.cs` is spaced and commented for
developers. It activates the existing configuration and runs the full3-key and
filtered2-key selections through HolonManager and deferred LINQ.

```powershell
dotnet ./Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll `
    --stacks-public LocalState/COSMIC/HostedStacksPublicSourceCurrent/source.json
```

Build with the repository provider lease and bounded workers. Reuse the same
source file; the qualification runner is a single journalled acceptance step,
not the normal interactive demo command. The walkthrough creates no wallet,
source write, avatar account or Mongo record.

## Application example

Use your application `HolonManager` and authenticated avatar ID:

```csharp
using var source = StacksPublicPrincipalMetadataSource.FromJson(
    await File.ReadAllTextAsync(configurationPath));

var activation = await source.ActivateQuerySourceAsync(cancellationToken);
if (activation.IsError)
    throw new InvalidOperationException(activation.Message, activation.Exception);

string[] principals =
{
    "SP000000000000000000002Q6VF78",
    "SP000000000000000000002Q6VF78.pox-4"
};

var scope = CosmicQueryScope.Named(ProviderType.BlockStackOASIS).Result;
var access = CosmicQueryReadAccess.OwnerOrPublic(avatarId).Result;

// Contains plans bounded metadata membership over the configured principals.
var selection = CosmicQuery<Holon>.Create(scope)
    .WithReadAccess(access)
    .Where(row => principals.Contains(
        CosmicQueryMetadata.Value<string>(row, "StacksPrincipal"))).Result;

var deferred = CosmicLinq.Create(
    manager, source, selection, maximumResults: 40);

if (deferred.IsError)
    throw new InvalidOperationException(deferred.Message);

var result = await deferred.Result.ToCosmicListAsync(cancellationToken);
if (result.IsError)
    throw new InvalidOperationException(result.Message, result.Exception);

foreach (var row in result.Result)
{
    Console.WriteLine(row.Name);
    Console.WriteLine(row.MetaData["StacksBalanceMicroStx"]);
    Console.WriteLine(row.MetaData["StacksBlockId"]);
}
```

Equality on `StacksPrincipal` selects one configured key. Membership, `Skip`
and `Take` operate over the bounded selection. All selected states validate
before paging, including an empty page. Unsupported predicates, arbitrary
principal scans and child graph requests fail capability planning.

## Exact values and tip semantics

| Metadata key | Meaning |
|---|---|
| `StacksPrincipal` | Exact configured standard or contract principal |
| `StacksBalanceMicroStx` | Spendable micro-STX, exact decimal uint128 string |
| `StacksLockedMicroStx` | Locked micro-STX, exact decimal uint128 string |
| `StacksNonce` | Exact decimal uint64 string |
| `StacksUnlockHeight` | Exact decimal uint64 string |
| `StacksBlockId` | Explicit derived state selector used for all selected reads |
| `StacksBlockHash` / `StacksConsensusHash` | Captured node hashes used to derive that selector |
| `StacksTipHeight` | Captured node height as an exact decimal string |
| `StacksGenesisChainstateHash` | Node-reported genesis chainstate hash |
| `StacksObservationPolicy` | `node-reported-explicit-tip` |

Each selection reads synchronized mainnet node information once, derives the
reference block ID with SHA-512/256 over block hash followed by consensus hash,
then reads each selected principal with `proof=0` at that exact block ID. Plain
SHA-512 truncation is a different algorithm. Every selector is canonical before
I/O; malformed selectors can cause the node to choose its default state.

The source does not use `tip=latest`, which can include unconfirmed state.
Selections have a30-second total deadline,15-second request timeout,64KiB
per-response and256KiB aggregate limits. Missing/error/malformed responses fail
the whole selection. There is no retry, alternate provider or partial success.

These are trusted node-reported state observations, without independent MARF
proof verification, consensus or finality guarantees. Separate queries may see
different tips. Zero balances do not prove that an account/contract exists.
Native saves/deletes, signing, submission, Gaia storage and full ORM parity are
outside this read profile.

## Demo acceptance

Source controls, actual C# execution, HTTP/Mongo pairing, ownership/privacy,
replication, hosted admission and browser interaction are separate gates.
See the maintained [Stacks status](../WIP/COSMIC%20ORM/STACKS_PUBLIC_SOURCE_STATUS.md)
for verified scope; local or native evidence alone is not hosted certification.

Primary contracts: [node RPC](https://github.com/stacks-network/stacks-core/blob/main/docs/rpc-endpoints.md),
[tip selector](https://github.com/stacks-network/stacks-core/blob/main/docs/rpc/components/parameters/tip.yaml),
[block-ID derivation](https://github.com/stacks-network/stacks-core/blob/main/stacks-common/src/types/chainstate.rs),
[C32 addressing](https://docs.stacks.co/more-guides/c32check).
