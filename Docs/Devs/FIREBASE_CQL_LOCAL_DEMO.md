# Firebase Realtime Database CQL local demo

Run one bounded CQL identity selection through HolonManager across Firebase RTDB
and MongoDB. This uses the official RTDB emulator, separately from Firestore.
Full selection returns three Firebase rows and two Mongo rows; the subset returns
two of each. Each copy retains its provider. Reads may observe different moments.

## Recovery laptop

From `C:/Source/OASIS2`, with the admitted recovery runtime/source and Mongo8.2
replica present:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicFirebaseCqlNative.ps1 -RecoveryLaptop -Federated -Serve
```

Open <http://127.0.0.1:5126/>. Choose **Query all records**, then
**Query two-record subset**. Finish with **Stop demo** and wait for verified
fixture cleanup and owned process shutdown. Start one native demo at a time.
Occupied ports are refused, never adopted.

The retained recovery fixture is
`LocalState/COSMIC/RecoveryLaptopFirebaseRtdb4112/FirebaseCql`. Its four SDK-saved
records include one soft-deleted record. It is a separate cohort from the
original laptop; the absent historical export and original IDs were not restored.
The emulator imports this recovery checkpoint into its empty volatile service.
This demonstrates export restoration, not native disk or crash durability.

First initialization is separate, only for an admitted runtime with no fixture:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicFirebaseCqlNative.ps1 -RecoveryLaptop -Initialize
```

Never initialize to repair a missing checkpoint or unresolved journal. Existing
sources refuse reinitialization. Do not replay writes after uncertain outcomes.
The pinned official emulator4.11.2 JAR is34,495,935bytes, SHA256
`b70d99344caf17c98b6f910fa8f6edf32a7c016cb1035e8915f70d38901eb97f`.
The launcher reuses the admitted Temurin21JRE and Mongo8.2 replica; it installs
neither. The original laptop profile remains available without `-RecoveryLaptop`.

## Scope

Selection uses bounded authorized native key equality reads. Deleted rows are
excluded and failures never publish partial success. SDK/HTTP and browser checks
preserve source provenance. Local emulator owner authorization stays local;
hosted sign-in is never forwarded. Public emulator reads are denied by rules.

Hosted Firebase credentials/rules, Firestore, general text/metadata selection,
wider numeric/array/graph roundtrips and full ORM parity remain separate work.
See [the WIP handoff](../WIP/COSMIC%20ORM/FIREBASE_CQL_MVP.md) and
[recovery cleanup ledger](../WIP/COSMIC%20ORM/RECOVERY_LAPTOP_CLEANUP_LEDGER.md).
