# Garnet CQL MVP — pending native admission

Updated29October2026. Remains red in the all-provider inventory.

## Verified and failed scope

- Garnet authority/compiler controls:8passed,0failed/skipped.
  `TestResults/GarnetCqlCurrent/garnet-controls.trx` SHA256 `de2fd98843e4206ceaf3e77d2df771d3fd7c5564ef11dd0d6e3975839899200a`.
- Fixture teardown controls:6passed. `Scripts/TestHosts/test_garnet_fixture_lifecycle.py`
  proves uncheckpointed or unknown-checkpoint source cannot be terminated, valid
  acknowledged checkpoint permits retirement, corrupt receipts cannot trigger a kill.
- Official Garnet2.2.5 runtime/version admitted using existing .NET10.0.12.
- Native case failed at SAVE after earlier read/write/filter/paging/budget assertions.
  Overall native result:0passed/1failed, not a green native query/restart proof.
  `TestResults/GarnetCqlCurrent/garnet-cql-native.trx` SHA256 `f1641ef1a6ab714df7568d9f012b8f65551f542809998d66fb3a1f238a2a1a22`.
- No paired preset, browser verification or hub enrollment yet.

## Actual failure and source gate

The native storage library could not load during the first checkpoint. The runner
then terminated an acknowledged but uncheckpointed synthetic source. That teardown
was wrong. The original encrypted payloads are no longer present: its checkpoint
tree contains0files. Four write acknowledgements, delete acknowledgement, IDs,
protected keys, hash-only baseline and failure intents remain; hashes cannot restore
payloads. Do not call this recovery or replay/reseed the original writes.

Preserve `/var/tmp/oasis-garnet-cql` (4,243B at audit), including runner/checkpoint
pending files. The owned PID is terminal and port16392 has no listener. A protected
rejected-startup config and pre-startup review also remain. No user database was
used; this was the new dedicated synthetic fixture.

## Root corrections

1. Use standard DOTNET_ROOT to select the already-installed .NET10.0.12; the package
   is a framework-dependent single-file apphost, not NativeAOT. No CLR/SDK install.
2. Remove LogDir when storage tiering is disabled. Original pre-start failure
   occurred before any source writes; its protected config/receipts were preserved.
3. Install official Ubuntu libaio1t64/liburing2. Native ldd identified missing IO
   dependencies; the error was not evidence that the CQL compiler needed a fallback.
4. Correct the official based archive's flat storage-library install layout: move
   the single original hash-verified libnative_device.so into the resolver's required
   net10.0/runtimes/linux-x64/native path. No duplicate/alternative implementation.
5. `garnet_fixture_lifecycle.py` now prevents teardown from discarding live,
   uncheckpointed source. Unknown outcome remains explicit; no successful replay.

These changes do not retroactively recover the failed source or certify native
checkpointing. Actual ABI/checkpoint verification is still required.

## Exact next step

First run an explicitly owned EMPTY native-checkpoint probe with the corrected
runtime/dependencies, separate from the failed source and with no holon writes.
Record its native SAVE result and verify teardown. Preserve the original fixture
unchanged. Then explicitly resolve failed-fixture disposition/new-source admission
before any new business-record setup; no automatic resets, replacement IDs or
retrying its pending SAVE. Only after native/restart/paired/browser proof may this
provider enter the ready demo list. Other red providers can proceed independently.

Do NOT rerun the current native fixture blindly: `run-cosmic-garnet-linux.py` refuses
its pending outcomes. It currently exposes only native verification, with no
unimplemented federation/serve switches. The original source gate remains intact.

## Runtime and resource commands

```powershell
# Recheck pinned runtime files/version; no service launch:
wsl -d Ubuntu -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/prepare-cosmic-garnet-linux.py

# Repeat the source-preservation controls without a native service:
wsl -d Ubuntu -- env PYTHONDONTWRITEBYTECODE=1 python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/test_garnet_fixture_lifecycle.py
```

Runtime `/var/tmp/oasis-runtimes/garnet-2.2.5`, official
[Microsoft release](https://github.com/microsoft/garnet/releases/tag/v2.2.5).
Archive5,694,440B SHA256f397cf0da94697ebd2fa17e7d3bc448fa5d62578f2c9693698a104a88d91c626
removed after admission. Apphost SHA2564e38420e097c7c72c5cba0a9a8fa864ced165d48f5ec7ad79387e0b5ac2b3750.
Native storage library SHA256053ed531bcd3ac1e7c244ffd540cf6ffda9bc290c8934bddcff7e61052f2f67d.

The planned native profile enables Lua AND LuaTransactionMode explicitly, uses
password auth,32MiB main log/512KiB pages/1MiB index, string-only storage, checkpoint
recovery and FailOnRecoveryError. External default Garnet endpoints do not thereby
inherit conditional-write certification; configuration/atomicity admission remains
a separate production gate. A completed checkpoint profile does not prove AOF,
uncheckpointed crash durability, full LINQ or HyperDrive replication.
[All resources, dependency versions and cleanup](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
