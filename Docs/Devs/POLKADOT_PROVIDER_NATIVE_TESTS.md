# Reusable Polkadot/Substrate native transfer checks

## Scope and safety

These checks exercise the real ordinary `SendToken`/`SendTokenAsync` path: official SDK metadata/signing, independent original verification, the existing execution-owner persistence callback, fsync-before-ACK, one-shot SCALE submission, finalized dispatch/native fee evidence and exact balance deltas. The test owner uses a real flushed file, **not production Mongo outbox/quota/hosted billing**. This is local Substrate runtime proof, not certification of every Polkadot relay/parachain/runtime, assets, NFTs, storage, bridges or V2 routing matrix.

The public development seeds `02` and `03` repeated32bytes and `//Alice` are exclusively local fixtures. Never fund them on public/value-bearing networks. Runner and bootstrap hard-pin loopback `127.0.0.1:19944` and genesis `0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208`. No real funds or USD valuation are involved. `UNIT` at12decimals is explicitly defined by this local specification, not guessed for a public chain. Capped sponsored spending remains blocked: `partialFee` is not a signed all-fees maximum.

## Retained resources

- Official Parity `substrate-contracts-node` v0.42.0-f209befc88c: `/var/tmp/oasis-runtimes/substrate-contracts-node-0.42.0/substrate-contracts-node`, binary SHA256 `b5e30ad4bcf37ab0a875f88c690460cf68a0847c03e74045d15bb16513892991`.
- ONE persistent ledger: `/var/tmp/oasis-polkadot-evidence/ledger`; no reset/purge/new ledger per run.
- ONE generated specification: `/var/tmp/oasis-polkadot-evidence/chain-spec.json`; `Prepare-ReusablePolkadotSpec.mjs` creates it only if missing and checks the retained runtime hash. Its loader requires `relay_chain`/`para_id` schema fields even for Development; the Development service does not launch a relay chain.
- ONE installed SDK: retained `C:\Source\OASIS-hyperdrive-v2-gaps\Providers\Blockchain\NextGenSoftware.OASIS.API.Providers.PolkadotOASIS\SdkBridge\node_modules`. Dev's same-path `node_modules` is a directory junction to this installation, **not a copy**. Preserve the target while this junction is used. No second npm installation.
- Existing restored dev unit-test host compiles the single canonical native source with `IncludePolkadotLocalNativeTests=true`. No integration-project restore, new clone or container.
- Fixed compact evidence: `C:\Source\OASIS\TestResults\PolkadotCurrent`; reports/completed records are reused, not timestamped directories.

Official runtime/source: [release](https://github.com/paritytech/substrate-contracts-node/releases/tag/v0.42.0), [loader](https://github.com/paritytech/substrate-contracts-node/blob/v0.42.0/node/src/command.rs), [schema](https://github.com/paritytech/substrate-contracts-node/blob/v0.42.0/node/src/chain_spec.rs). Its SDK/runtime age is an explicit coverage limit, not current-mainnet certification.

## Resume and run

First inspect the port/process; reuse a running correct node. If stopped, run the retained binary against the SAME specification/ledger (foreground, owned terminal):

```powershell
wsl -d Ubuntu -- bash -lc 'exec taskset -c 0-3 /var/tmp/oasis-runtimes/substrate-contracts-node-0.42.0/substrate-contracts-node --chain /var/tmp/oasis-polkadot-evidence/chain-spec.json --alice --force-authoring --base-path /var/tmp/oasis-polkadot-evidence/ledger --rpc-port 19944 --listen-addr /ip4/127.0.0.1/tcp/19945 --no-prometheus --no-telemetry --no-mdns --out-peers 0 --in-peers 0 --offchain-worker never --state-pruning archive --blocks-pruning archive --database rocksdb --name oasis-reusable-polkadot > /var/tmp/oasis-polkadot-evidence/node-current.log 2>&1'
```

Do not use bare `--dev` without the explicit specification: this runtime's default properties are empty and the provider correctly refuses to infer native precision. No `--tmp`, purge, reset or reinitialization. Genesis was checked unchanged after adding properties and restarting the original ledger.

From `C:\Source\OASIS`:

```powershell
./Scripts/TestHosts/Run-ReusablePolkadotNativeTests.ps1 -CheckOnly
./Scripts/TestHosts/Run-ReusablePolkadotNativeTests.ps1
```

The runner takes an exclusive fixed lease, rejects pending originals, checks chain/properties, reuses completed bootstrap funding, builds without restore and requires exactly2passing/0skipped native rows. Bootstrap first-ever funding signs using the public local-dev Alice account, independently verifies original bytes, writes/flushes a create-only pending record BEFORE broadcasting once, and only reconciles it into a completed record after finalized success. A repeat uses the completed record; no automatic top-up/resubmission. A depleted test balance requires review, not resetting the ledger.

Each test sends `1000000000001`baseunits and independently proves `recipientAfter-recipientBefore == transfer` and `senderBefore-senderAfter == transfer+actualFee`. Async includes a Unicode memo, sync is plain native transfer. Actual fee includes tip already; never add the tip twice. No sponsored admission/retail fee is inferred.

## Pending outcomes and shutdown

`local-bootstrap-pending.json` or `local-native-pending.json` is a fence. Preserve it and the ledger. Read/verify the SAME original, use the retained signing block and read-only outcome reader to establish finality/fees. Never delete an uncertain record to rerun, re-sign, change client correlation or recreate the ledger. Tests remove only an exact finalized original after flushed completed evidence and exact balance assertions; failures retain pending state.

After terminal tests and no unresolved original, stop only the owned process: inspect `ss -ltnp`, verify `/proc/<pid>/exe` equals the pinned binary, then SIGINT that PID and observe its original process handle to terminal. Retain ledger/runtime/specification/completed evidence; remove no unrelated data. The runner does not start/stop nodes or install dependencies.

Remaining: actual Mongo/hosted owner and billing integration; native fee price/cap/reservation/settlement; cross-runtime historical signing verification and durable recovery worker; other provider interfaces and full HyperDrive V2 matrices. See `HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md` for exact current reports and hashes.
