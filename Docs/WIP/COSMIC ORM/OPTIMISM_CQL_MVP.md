# Optimism V2 CQL MVP — native verified, paired pending

Updated 29 October 2026. Optimism remains red in the all-provider demo inventory.

## Reproduced root causes and fixes

The shipping Web3Core Optimism class inherited a query adapter, but its authority
was excluded by the shared compiler. Its constructor also omitted the chain
identity used by authenticated storage encryption. Initial controls reproduced
**3failed/5passed**: missing default chain10, rejected authorized catalogue, and
an access error before the expected inactive-source result.

- Admit explicit Optimism authority to the existing Web3Core bounded GUID compiler;
  exact trusted record catalogue, frozen selection, unsupported-shape rejection,
  at most100 identities. No scan or alternate provider execution path.
- Preserve the original three-string constructor and delegate to one initializer,
  using mainnet default10. A new explicit chain overload supports configured test
  networks (31338 control), rejecting zero/negative identities before native access.
- Legacy Optimism files were not changed. Their remaining gaps are not certified.

## Verification

[Optimism controls](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/OptimismCqlTests.cs):
**11passed**. Changed-compiler regression controls Polygon7/Arbitrum7/Base7/
Avalanche16 also pass: **48total**, zero failed/skipped. Web3Core/Optimism/test
producer builds pass; cached-only restores, bounded workers, no native services.

Evidence under `TestResults/OptimismCqlCurrent`:
- `optimism-repro.trx` SHA256 `6d8be64b9a3ad3418938c64bf6c58e05030fe6a1f61dd0dcd0eed9640b7f297b` (preserved original failures).
- `optimism-family-controls.trx` SHA256 `0f24cb3b12ac9c41eae6030927bb62d53acdc7a2fee176ec16263a32da37070f`.

## Native local EVM proof

Reused Ganache7.9.2/solc0.8.26/ethers6.13.1 and original persistent local chain31338,
loopback18547, with the same original DPAPI storage key. No runtime/download/key
or second chain created. Optimism constructor explicitly selects chain31338.
Dedicated original contract `0xb09bcc172050fbd4562da8b229cf3e45dc3045a6` contains
three encrypted records, CQL membership filters them to two. Names read correctly
through the shipping V2 provider. Raw native lookup response hashes and account
nonce remain unchanged during queries. Full runner restart reuses the same
contract/IDs/ciphertext with no further source writes: **native1/restart1 pass**.

[Native test](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/OptimismCqlNativeTests.cs).
Original source metadata885B at `LocalState/COSMIC/EvmCql/Optimism`: durable initial
intent, deployment receipt, per-write acknowledgements, immutable manifest and
native-record-hashes. Pending/unknown outcomes refuse replay, missing prior native
hashes refuse adoption. Shared chain and protected key retained outside TestResults.
Owned node flushed/stopped through its stdin protocol, no18547listener remains.

Evidence:
- `TestResults/OptimismCqlCurrent/optimism-native-first.trx` SHA256 `877cf34b6f3bdcb9296363b432db76575a7c4ece079a6f4e0fed3933f1da9c7e`.
- `TestResults/EthereumCqlCurrent/optimism-cql-native.trx` SHA256 `d7dc3663832c5e23f05c56406cd70b3f7e6392524f7f4823673b8c415dc94004`.
- Source lookup-hash manifest SHA256 `1a0963b73c2445aa9419cfa7780e307ce053fb338a88e868914812a3c3ec4ed3`.

```powershell
Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider Optimism
```
This verifies a local EVM implementation, not public Optimism execution/finality.

## Exact next step and remaining gates

Reuse this original source for the Mongo-paired C#/HTTP/browser5/4 preset and hub.
Do not increment the scoped demo count until paired/browser proof passes.
Central Optimism BootLoader case is currently commented out; registration/factory
configuration needs separate acceptance. Direct construction must be labelled.
Public Optimism/OP Stack/sequencer/finality/bridging/gas behavior is not certified
by a local EVM fixture. Legacy files remain unchanged; full child/history/
concurrency/placement/parity and production network integration remain open.
