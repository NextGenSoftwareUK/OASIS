# Optimism V2 CQL MVP — controls verified, native pending

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

## Exact next step and remaining gates

Reuse retained Ganache7.9.2/solc0.8.26/ethers6.13.1 resources and EVM chain31338,
with its original chain files/encryption key. Admit one Optimism-specific original
contract/actor/IDs with durable pre-dispatch intents. Use explicit local chain31338,
verify encrypted three-record native membership query2records, reuse/restart without
new writes, then Mongo-paired C#/HTTP/browser5/4 through the shared teaching host.
No Optimism fixture/contract/transaction or new runtime has yet been created here.

Central Optimism BootLoader case is currently commented out; registration/factory
configuration needs separate acceptance. A direct-construction demo must say so.
Do not count inherited interfaces or other EVM-provider evidence as Optimism proof.
Public Optimism/OP Stack/sequencer/finality/bridging/gas behavior is not certified
by a local EVM fixture. Full child/history/concurrency/placement/parity remains open.
