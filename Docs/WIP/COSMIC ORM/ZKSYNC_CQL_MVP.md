# zkSync CQL MVP admission

Updated 29 October 2026. Controls verified; native/restart/paired remain pending.

## Root fixes

Original three-string `zkSyncOASIS` constructor now supplies declared chain324 to
the shared encryption boundary. Signature retained; explicit positive chain-ID
configuration supported, zero/negative rejected before native access. Shared CQL
compiler admits explicit zkSync authoritative catalogues and retains immutable
ID capture, authority separation, shape/bound rejection and lifecycle/cancellation.
Actual repro3failed/5passed; fixed zkSync11passed and affected EVM family116passed,
0failed/0skipped. `ZkSyncCqlTests.cs` in Ethereum integration project proves controls.
Evidence `TestResults/ZkSyncCqlCurrent/zksync-family-controls.trx`, SHA256
`e13c576a13978c461d8848489c4e10672e3324cef8b91c22163d7c628956e8a7`.

## Runtime audit and exact next step

[Official interpreter overview](https://docs.zksync.io/zksync-protocol/era-vm/evm-interpreter/overview)
reviewed29October2026: EraVM hosts an EVM-bytecode interpreter, accepts ordinary
compiled EVM contracts, and has execution/gas limitations. EraVM-specific testing
requires an appropriate zkSync node. Ganache alone cannot prove EraVM behavior.
Scoped task-resource search found no retained zkSync runtime; this is not a global
machine inventory. Next inspect existing Windows/WSL runtime caches; reuse if suitable,
otherwise provision one pinned official lightweight zkSync runtime and journal it.
Verify original encrypted three-record fixture/filter-to-two/restart, then Mongo
paired C#/HTTP/browser5/4 before inventory enrollment. Do not replace this gate with
Ganache success. No runtime was installed or new chain/source created in this step.

Public network/sequencer/finality/bridging, central registration and full parity
remain unverified. No MVP proof link yet.
