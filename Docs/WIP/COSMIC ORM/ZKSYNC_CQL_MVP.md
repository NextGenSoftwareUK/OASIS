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

## Reproduced native checkpoint gate

`Scripts/TestHosts/Test-CosmicZkSyncCheckpoint.py` journalled three synthetic mined
blocks (no business writes), exported original `anvil_dumpState(true)` and retained
`/var/tmp/oasis-zksync-cql/checkpoint.hex`371,240B plus acknowledgement94B.
The exact decoded gzip `checkpoint.bin`185,619B is retained for CLI-load evidence.
Total retained environment556,953B. Original pending intent is resolved; no fixture
contract/records exist. Export/hash succeeded; restart acceptance **fails**.

Both `anvil_loadState` into a separately started interpreter node and CLI
`--load-state checkpoint.bin --evm-interpreter` reject `LoadingStateOverExistingState`.
Pinned upstream root cause: [CLI startup](https://github.com/matter-labs/anvil-zksync/blob/v0.6.11/crates/cli/src/main.rs)
executes the interpreter-enabling transaction before loading state;
[the loader](https://github.com/matter-labs/anvil-zksync/blob/v0.6.11/crates/core/src/node/inner/in_memory_inner.rs)
rejects `storage.blocks.len() > 1`. This is not evidence of corrupted source.
Immediate shutdown also did not produce the promised dump; explicit RPC export did.
Do not switch off interpreter checks, load over nonempty state, reset/reseed, or
claim Ganache proves native zkSync recovery.

Test command: `wsl -d Ubuntu -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/Test-CosmicZkSyncCheckpoint.py`.
It reuses and verifies the existing checkpoint, never replays the initial probe.
Current nonzero result is the reproduced upstream startup-order gate. Evidence:
`TestResults/ZkSyncCqlCurrent/checkpoint-summary.log` and `checkpoint-reload.log`.
All owned processes exited;18548 has no listener. Next zkSync step requires a
verified upstream startup-order correction compatible with interpreter checkpoints.
The native/paired MVP remains red; continue breadth with Monad's provider audit.

Public network/sequencer/finality/bridging, central registration and full parity
remain unverified. No MVP proof link yet.
