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
Runtime cache inspection found no suitable retained zkSync node. Installed official
[anvil-zksync v0.6.11](https://github.com/matter-labs/anvil-zksync/releases/tag/v0.6.11)
x86_64 Linux at `/var/tmp/oasis-runtimes/anvil-zksync-0.6.11/anvil-zksync`.
Archive25,446,040B SHA256127f1a75b6c4ccc5eaef00ca62753f243a76ba927cbd64a7b1bd99a5b5fb8728
verified before extraction; archive removed. Runtime69,374,656B SHA256
4a801b25f543099838fa88df50f0e59f5e5c93b7a22330948ca7ed029f48228b,
version/help verified. Original reusable environment `/var/tmp/oasis-zksync-cql`.
No contract or business record has been created yet.

Startup smoke with `--evm-interpreter --chain-id31339` (separate CLI arguments),
loopback18548 passed `eth_chainId`; owned node received SIGINT/exited0. However,
`--dump-state` produced no checkpoint on this immediate shutdown. `--state` on a
missing file initially refused before startup. No source was lost: environment
remains empty and no deployments/writes occurred. Logs under
`TestResults/ZkSyncCqlCurrent/startup-summary.log` and `startup.log`.

Exact next: establish and verify the pinned runtime's explicit checkpoint/export
and load semantics before any fixture writes, then journal encrypted three-record
fixture/filter-to-two/restart and Mongo-paired C#/HTTP/browser5/4. Preserve original
source after first write; never replace this native gate with Ganache success.

Public network/sequencer/finality/bridging, central registration and full parity
remain unverified. No MVP proof link yet.
