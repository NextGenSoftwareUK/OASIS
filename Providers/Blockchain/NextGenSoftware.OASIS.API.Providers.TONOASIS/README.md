# Current native-source distinction

TonLocalTvmStorageSource is an explicit local TVM/emulator source with encrypted
canonical storage and bounded CQL. Four admission controls/current builds and a native local TVM test pass
(encrypted reopen, queries3/2/0 and ownership/budget checks). Public-network,
adapted retained-source/Mongo and hosted execution remain unverified. See
[scope and commands](../../../Docs/WIP/COSMIC%20ORM/TON_CQL_MVP.md).

The existing TONOASIS EVM wrapper remains legacy and is not native TON TVM.
Public-header queries use a separate profile. Older text below is historical
and does not establish public-network native storage, NFT/wallet or full parity.

# TONOASIS Provider

`TONOASIS` now uses the native TVM backend and mandatory shared private storage.
The former EVM inheritance is removed; its obsolete constructor rejects EVM
configuration. This is an explicit **local TVM emulator** profile, not public TON
network access or certification of HyperDrive replication/failover.

The new `Contracts/TonHolonStore.tact` uses TON's native TVM, with an immutable
owner, keyed cell values and owner-authorized update/delete messages. Its local
TVM tests cover three records, rejected foreign changes, long/Unicode payloads,
missing reads and saved-state reload in a separate process.

Run from the repository worktree:

Once configured, enable `TONOASIS` in HyperDrive replication/failover settings to
include TON alongside the rest of the blockchain quorum.
