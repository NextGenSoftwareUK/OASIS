# TRON CQL MVP — native restart and Mongo/browser verified

Updated 30 October 2026. Backend task branch; sandbox catalogue publication is tracked below. The native demo runs locally.

## Verified scope

- Seven CQL controls pass. Bounded Id equality/membership, constants, AND/OR and paging; at most 100 explicitly authorized keys.
- A separately declared local contract stores three synthetic holons through the shipping encrypted provider. Native selected-three/subset-two queries passed after deployment.
- Restart exposed a real local-runtime persistence defect: the last acknowledged synthetic record and its transaction receipt disappeared; the first two records survived.
- Fixed the runner's mining/shutdown boundary. TRE defaults to transaction-triggered mining; installed SnapshotManager bytecode flushes during a following session and does not flush the current session in shutdown(). The runner enables interval mining, then drains two empty blocks before graceful shutdown. No business transaction is replayed by the drain.
- Explicit one-time maintenance restored only the demonstrated lost synthetic Tea record with a new signed operation and separate intent/preparation/ACK. Its exact source manifest, record and original transaction are checked; both sibling payloads remain unchanged. No automatic query/save repair exists.
- After that shutdown/restart, the native test passed: three selected records, two-record subset, successful empty missing-record query, exact canonical payload preservation and unchanged hashes of every signed preparation. Reads cannot request signing material.
- Container exited and 9090/50051/50052 listeners absent. Original seed manifest hash unchanged.

This is local TRE evidence. No mainnet/testnet, general metadata/text queries, cross-provider snapshot or full ORM/HyperDrive parity claim. Unsupported content/order/graph queries reject before I/O. Canonical responses have a 4 MiB budget after SDK decoding.

## Run and source ownership

Normal reusable native verification:

```powershell
./Scripts/TestHosts/Run-CosmicTronCqlDeclared.ps1

# Start the paired HTTP/browser teaching demo, then open http://127.0.0.1:5108/.
./Scripts/TestHosts/Run-CosmicTronCqlDeclared.ps1 -Federated -Serve
```

The runner checks exact container/image, 3 GiB limits, loopback bindings, genesis and independent-source admission. It preserves failed reports and refuses unknown pending setup. Shipping Solidity is compiled using cached solc 0.8.26; nothing is downloaded. Builds use the provider lease and bounded workers.

Persistent source: `LocalState/COSMIC/TronCqlDeclared` (16 files, 14,765 bytes at this checkpoint). Retain the original DPAPI key, manifest, four original signed preparations/ACKs, and three repair records. Never place them in cleanup-targeted TestResults.

`-RepairAcknowledgedLostRecord` selects incident-specific one-time maintenance, not a demo preset. It refuses an existing repair intent and must not be repeated. It requires exact original manifest SHA256 `8265D4E7C4304E61DE78CCAFEC0273DC02F7D7AE2E9B45765D28DBBB8EDFC562`, absent affected record and absent original solidified receipt. The original lost transaction remains recorded; restoring the intended synthetic holon does not recover that transaction or its original timestamps.

`set-owned-tron-mining.sh` uses existing WSL Python/nsenter inside only the exact container's network namespace; administrative port 8545 is not newly exposed. No new runtime/container/ledger was installed or created. The same retained ledger and container are reused.

## Compact evidence

Paths below are under `TestResults/TronCqlCurrent/`; raw evidence is not committed to Docs.

| Evidence | Result | SHA256 |
| --- | --- | --- |
| `tron-scoped-integration-controls.trx` | 7 passed | `37D55A77115943633FDF9ECB48ED4F7DBC1643E9285088078371CB081C8E7DFB` |
| `Archive/54CF44038FBCC1F993B811588D1B85773AE255541231F650AA095459E8FC6CA3-tron-declared-native.trx` | initial native schedule passed | filename hash |
| `Archive/6D2C22E1350ADAF8F75C1AE11C46086D778224CBEE86E9CB11684A7D341305D2-restart-failed.trx` | restart failed; retained | filename hash |
| `Archive/114CC152E1C7C83088B4ABF549A4E4E568BB84D4B25049D5C6B81AB04602C646-diagnostic-failed.trx` | two retained keys; last absent | filename hash |
| `tron-declared-repair.trx` | explicit restoration: 1 passed | `80B963A188AAACD1FC86D62EE9706890095FFA7028CAAC860A87F4499560474F` |
| `tron-declared-native.trx` | fixed restart/missing/unchanged reads: 1 passed | `6B227AF2BAC0EDF740984F8F887A9F4C27D9E1A049D67DC252EE3F4AAE68C059` |

Read-only diagnostics: `receipt-inspection.log`, `mining-inspection.log`, `snapshot-bytecode.txt`, `tre-bytecode.txt`; run logs `declared-native-run.log`, `declared-native-restart.log`, `declared-native-diagnostic.log`, `declared-native-repair.log`, `declared-native-fixed-restart.log`. Inspection reused installed JDK 21.0.12.101; extracted small classes only, no full JAR copy. Upstream also discusses [snapshot flush versus shutdown persistence](https://github.com/tronprotocol/java-tron/issues/6610); the installed runtime inspection and restart test are the evidence for this local fix.

## Historical source remains unresolved

Exact retained container: `ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12`; pinned TRE image digest `cbf2973095933e3ae3237767082e29add2f9b647d7a4aebdfb7a953c6e5656ba`. Genesis `0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f`.

Older `TestResults/HyperDriveTron/pending-native-outcome.json` and `independent-prepared-native-pending.json` remain missing. Historical unknown transaction `db05ea6d3067038a56dfc75b64d2c46c32824282e8242335ce007d1e34a415bb` is not recovered, replayed or declared absent. Existing public local test credential derivation does not recover missing contract/operation provenance. Keep `Run-ReusableTronInspection.ps1`'s original-fence guard unchanged. This independent contract is explicitly marked originalRecovery=false.

## Mongo federation and browser acceptance

`TronMongoCqlNativeTests.NativeTronMongoFederationRunsSdkAndHttpWithProviderProvenance` passes one native schedule (33 seconds, no skips). It runs the shared C# walkthrough and HTTP host through HolonManager, verifies all-five/subset-four physical rows (three/two distinct selected identities), excludes the unrelated third Mongo record, rejects actor injection and unknown presets, and returns 503 without partial rows when TRON is inactive. Both browser buttons independently returned the same counts and source labels.

Read-only TRON access has no signing resolver. All three canonical source payloads and every signed preparation hash are checked unchanged. Exact temporary database `cosmic_cql_mongo_tron_demo` is verified for owned records/collections, dropped, and confirmed absent before its intent is removed. The original TRON seed/key/ledger remain. Runner exit 0; container exited; 9090/50051/50052/27029/5108 listeners absent; pending/ready files absent; seed hash unchanged.

Evidence `TestResults/TronCqlCurrent/tron-mongo-federation.trx`, SHA256 `0FAA79F2F76454CBB3193F2145F15119F1221520034B2F54D0EF694DE049938E`. Browser captures `tron-mongo-all.png` and `tron-mongo-subset.png`; runner log `tron-mongo-runner.log` in the same directory. The source lease, provider build lease, retained Mongo8.3.11 runtime/data and protected TRON source are reused. Consumer assets restored from local NuGet cache only.

The local demo preset is ready. Sandbox CQL section13 is published on Web4Site main `836e4af2a249d67709307c19df90018d26ee7fb0`, production deployment `7451345916` success. Public browser verification confirmed the expanded command and fixed5108 link. Screenshot `TestResults/TronCqlCurrent/tron-catalogue-public.png`, SHA256 `E7FB0B056898C4565B3161683A5F860D71528545751AC99BFC44EBBFF50E70E9`. The catalogue is public; native services remain local and stopped. Metadata/text/general ordering, production authentication and full ORM/HyperDrive parity remain deferred red scopes; see [after-demo backlog](AFTER_DEMO_BACKLOG.md) and [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).