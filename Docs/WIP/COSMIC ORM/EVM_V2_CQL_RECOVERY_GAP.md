# EVM V2 owned-holon CQL recovery gap

Verified 2 November 2026. This is a red scope, not native acceptance.

## Broken invariant

A demo labelled as WEB4 V2 must execute the provider selected by its shipping
factory. Restoring the historical Ethereum launcher alone would not prove that.

- `EthereumProviderFactory.CreateForMode` returns `EthereumOASIS_Web3Core` in V2.
- `EvmProviderFactory.CreateArbitrum` returns `ArbitrumOASIS_Web3Core` in V2.
- Both inherit `Web3CoreOASISBaseProvider`. The shared base now implements
  `ICosmicQueryStorageProvider` for bounded owned-holon reads (controlled proof below).
- The retained `EthereumCqlNativeTests` at pin237403a75 constructs
  `EthereumOASIS` and deploys its legacy contract; it is not a V2 proof.
- The retained federated runner defaults to that legacy Ethereum instance;
  its Arbitrum helper does select the shipping V2 factory. The combined launcher
  therefore cannot be recovered as a uniformly V2-certified demo unchanged.
- `EvmPublicAccountMetadataSource` already supports a separate public read-only
  account profile. Its accepted hosted proofs remain valid for that precise scope.

## Implementation work next

1. Implement bounded, authorized owned-holon CQL once in the shared V2 storage
   base using the existing canonical native read/decryption/identity contract.
   Declare actual cancellation, read consistency, limits and unsupported features.
2. Add controlled tests proving authority/admission before native I/O, canonical
   identity/deletion checks, complete result budgets and no partial publication.
3. Make teaching/native tests construct actual V2 factory-selected providers and
   use a compatible existing Web3Core contract. Never deploy or substitute a legacy
   ABI after a V2 failure. Preserve original ledgers/keys and unknown outcomes.
4. Restore the launcher only after the full named consumer closure compiles.
   Fresh native/hosted proof remains a separate gate; do not count compilation as it.

Legacy implementation remains unchanged. Record its gaps only. No new runtime,
contract deployment, chain transaction, environment, credential access or native
fixture replay was performed during this review. Counts remain local88/hosted60;
11 published launcher paths are missing. Public profiles must not be overwritten.

## Shared V2 implementation milestone

The shared base now implements bounded identity equality/membership, boolean
composition and paging through the existing canonical `LoadHolonAsync` path.
Read authorization/admission precedes I/O. Only explicit RecordNotFound is
absence; native errors/timeouts, identity mismatches and complete-page budgets
fail without partial rows. Missing/deleted records do not consume page positions.
Decoded records are limited to 4MiB; the existing canonical RPC decoder still
owns native payload handling. This is not a transport-body allocation bound.

Each key is an independent observation. No pinned-block/cross-provider snapshot,
full ORM parity, arbitrary metadata search or native chain acceptance is claimed.
Caller cancellation stops publication/next dispatch; the existing RPC has no
caller-token parameter, so already dispatched native work may finish afterward.

10 controlled cases pass: visibility/paging, identity mismatch, native errors,
SDK timeout distinction, decoded-byte/result-count budgets, admission/inactive/
cancellation before dispatch, cancellation during a read, and actual shipping
Arbitrum V2 factory exposing the shared query contract. Provider and unit consumer
builds pass. Tests override canonical reads; no real chain/decryption acceptance
was performed. Original native fixtures and all legacy code remain unchanged.

Next: controlled actual native-RPC/decryption boundary tests and V2 teaching
consumer wiring, then compile/restore the launcher. Native and hosted gates stay
red. Existing aggregate coverage stays local88/hosted60 and missing launchers11.

Original TRX SHA256 `0ea77249a11ec79fbd3de2ba7bfd990e6a1a0412c9f45fe13951c4a3ddf2e3d3`; durable manifest `LocalState/COSMIC/LauncherRecoveryCurrent/web3-v2-owned-cql-evidence.json`.

## Production decoding boundary milestone

Six additional cases use the actual shipping Arbitrum V2 factory, production
Nethereum ABI decoding, authenticated encrypted records and canonical load/graph
projection. Only IClient transport responses are substituted in process using the
existing ReadRpc test helper; no external RPC/server/native chain is involved.
Valid records, tampered ciphertext, identity/context mismatch, transport errors,
explicit absent tuple and deleted records pass. Failures publish no partial rows.
The temporary synthetic encryption key is restored/zeroed in finally.

Original TRX SHA256 `0fb1b817ce6b5400fe5f6eff11ec3720c983f6405a8790964d2ac881908d3265`; retained manifest
`LocalState/COSMIC/LauncherRecoveryCurrent/web3-v2-owned-cql-rpc-boundary-evidence.json`.
Current total16 controlled owned-CQL cases across two test classes. This resolves
the controlled decoding test step above, not live chain acceptance. Next wire
V2 teaching/native consumers and restore a correctly labelled launcher. Coverage
counts unchanged; no legacy code modifications or native fixture replay.

## V2 C# teaching consumer

[Spaced/commented source](../../../Examples/CosmicCqlDemo/EvmV2OwnedWalkthrough.cs)
accepts an activated Web3Core V2 facade, existing HolonManager, trusted three-ID
catalogue, actor and output. It cannot accept legacy Ethereum. It deploys/seeds
nothing, queries all three through CosmicLinq/HolonManager, then uses Contains
to select two. RunWithMongoAsync reuses the existing federation preset and
separate trusted policies.

```csharp
// Application supplies activated V2 facade/manager and policy-approved IDs.
var counts = await EvmV2OwnedWalkthrough.RunAsync(
    manager,
    activatedV2Provider,
    authorizedChainIds,
    actorId,
    Console.Out,
    cancellationToken);

var combined = await EvmV2OwnedWalkthrough.RunWithMongoAsync(
    manager,
    activatedMongo,
    activatedV2Provider,
    selectedIds,
    authorizedMongoIds,
    authorizedChainIds,
    actorId,
    Console.Out,
    cancellationToken);
```

Six boundary tests pass with the positive case executing this exact teaching
source, production manager/LINQ and V2 ABI/crypto:full3/subset2. The Mongo method
compiles, but was not executed against Mongo. This is a reusable C# consumer,
not a completed CLI launcher or real-chain proof. Next reconcile a compatible
retained Web3Core deployment/config, wire native V2 launcher and verify. Never
reuse the old legacy Ethereum seed as Web3Core. Coverage counts unchanged.

Original TRX SHA256 `437ff00da50acf79942838e151b2c820aaf78a5c115f612e4ee6fb6423824916`; retained manifest `LocalState/COSMIC/LauncherRecoveryCurrent/web3-v2-owned-cql-walkthrough-evidence.json`.
