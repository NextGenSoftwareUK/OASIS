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
