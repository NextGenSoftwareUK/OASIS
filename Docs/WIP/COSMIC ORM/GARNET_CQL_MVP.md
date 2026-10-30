# Garnet CQL MVP — verified generation2 checkpoint scope

Updated29October2026. Green only for this explicit new synthetic fixture;
full ORM/provider parity and original failed-source recovery are not certified.

## Run

```powershell
# Preview without starting services:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Garnet -Federated -ShowPlan

# Run C#/HTTP/browser preset; use the page Stop button afterward:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Garnet -Federated

# Automated native/checkpoint/restart/SDK/HTTP verification and teardown:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Garnet -Federated -VerifyOnly
```

Open http://127.0.0.1:5121/ when ready. All records returns5physical rows
(Garnet3+Mongo2); two-ID subset returns4(2+2). Matching IDs retain both provider
copies; the unrelated third Mongo record is excluded. Source2 is retained after
Stop, and only the verified temporary Mongo fixture is removed.

## C# usage

The preset compiles the shared, spaced and commented
[IdentityFederationWalkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs).
With activated providers and trusted catalogues:

```csharp
// Freeze canonical holon IDs; authorization belongs to each source.
var selectedIds = readableGarnetIds.Take(2).ToArray();

// HolonManager dispatches explicit Mongo + Garnet scope.
var result = await IdentityFederationWalkthrough.RunAsync(
    manager,
    mongo,
    garnet,
    selectedIds,
    readableMongoIds,
    readableGarnetIds,
    actor,
    Console.Out);

// Both native sources evaluate Where(row => selectedIds.Contains(row.Id)).
// Physical copies remain labelled in result.Rows.
```

Garnet binds its own fixed authority through the existing encrypted Redis transport
executor/canonical save keys. There is no second executor or plaintext source.
Bounded ID equality/membership/Boolean algebra supports at most100authorized IDs;
missing/deleted rows are excluded before paging. Unsupported fields/order/children
reject before reads. Decoded total is capped at4MiB after individual reads; budget
errors publish no partial rows. Full LINQ, central registration, external configuration/
atomicity admission, AOF/crash durability, HyperDrive/cluster acceptance and distributed
snapshots remain separate. Native profile explicitly enables Lua AND LuaTransactionMode.

## Evidence

| Proof | Result | Evidence | SHA256 |
|---|---|---|---|
| Authority/shape/cancellation controls | 8 passed, 0 failed/skipped | `TestResults/GarnetCqlCurrent/garnet-controls.trx` | `de2fd98843e4206ceaf3e77d2df771d3fd7c5564ef11dd0d6e3975839899200a` |
| Empty native checkpoint ABI probe | 1 passed, 0 failed/skipped | `TestResults/GarnetCqlCurrent/garnet-empty-checkpoint.trx` | `8bf76a28cdb7496b033313019e711282fbd65e0f003ece9a07286dc81fa8489d` |
| Generation2 native CQL/checkpoint | 1 passed, 0 failed/skipped | `TestResults/GarnetCqlCurrent/garnet-g2-cql-native.trx` | `1d51d5873dc69eaac924a03a0633bb0e808c4d6094dfa08f32007f36c8c63f2d` |
| Generation2 checkpoint restart | 1 passed, 0 failed/skipped | `TestResults/GarnetCqlCurrent/garnet-g2-cql-restart.trx` | `361b72c3d834dc2416832a18184fba06e5da40bbee338e9edf02d5a003e66393` |
| Mongo bootstrap | 1 passed, 0 failed/skipped | `TestResults/GarnetCqlCurrent/garnet-g2-mongo-init.trx` | `4eecc24d97078c7a89a581274be52ab323a65825e2094068dd99c4ec4c64be78` |
| Paired SDK/HTTP repeat | 1 passed, 0 failed/skipped | `TestResults/GarnetCqlCurrent/garnet-g2-mongo-federation.trx` | `0e4f28ca6e019c9b79f9910684c1cb705152d551fa81d7aea16f532cd1bb507b` |

Lifecycle controls6pass; original fixture refusal/unchanged-files check passes.
`TestResults/GarnetCqlCurrent/lifecycle-controls.log` and `original-refusal.json`.
Native source2 has3live records/1soft-deleted record. Every encrypted native string
key/value hash survives queries, completed native SAVE and process restart. Missing/
deleted/paging/budget checks pass. Paired SDK/HTTP5/4, forged actor/unknown preset400,
inactive source503 without rows, unchanged source2 and exact Mongo cleanup pass.

Browser visibly returned5then4 with MongoDBOASIS/GarnetOASIS labels; screenshot
`TestResults/GarnetCqlCurrent/browser.jpg` is regenerable. Hub routing controls pass:
8single/16plans,30paired/60plans,46paired rejections,3archive rejections; listeners
unchanged. Actual interactive and automated-repeat launcher runs passed.

## Initial loss and explicit new-source admission

The first synthetic fixture lost its uncheckpointed in-memory payloads when the
old harness stopped it after a native storage failure. Its acknowledgements/IDs/
keys/hash baseline/intents remain at `/var/tmp/oasis-garnet-cql`; its pending gates
remain intact. Default original-fixture execution is verified to refuse unchanged.
This loss is not recovery. [Original failure record](Archive/GARNET_CQL_PRE_GENERATION2_2026-10-29.md).

Root corrections selected existing .NET10.0.12, removed invalid non-tiered LogDir,
installed the two native IO dependencies, placed the single original storage library
at its required RID path and protected live uncheckpointed source from teardown.
The empty probe then acknowledged SAVE with0records before/after, wrote1,050,624B
of real checkpoint files, verified original-source fingerprints unchanged, and
removed its empty checkpoint/config after retirement. Compact probe receipt remains.

An explicit admission separately created NEW actor/holon IDs for generation2,
disjoint from the lost fixture. No original IDs/writes were replayed, reset or
reseeded. It reused the same runtime, CLR and protected credentials by reference.
`admit-cosmic-garnet-demo-generation.py` binds the successful empty probe, original
file fingerprints and planned new IDs before writes. Normal runs never create a
new generation on failure. Generation2's native/restart/paired proofs alone support
this MVP claim; original loss evidence stays separate and unchanged.

## Resources, retention and limits

- Runtime `/var/tmp/oasis-runtimes/garnet-2.2.5`, existing .NET10.0.12; official
  [Microsoft release](https://github.com/microsoft/garnet/releases/tag/v2.2.5).
  Archive5,694,440B SHA256f397cf0da94697ebd2fa17e7d3bc448fa5d62578f2c9693698a104a88d91c626
  removed after admission; apphost SHA2564e38420e097c7c72c5cba0a9a8fa864ced165d48f5ec7ad79387e0b5ac2b3750.
- Data/receipts `/var/tmp/oasis-garnet-cql/generations/2` (1,062,244B at milestone).
  Retain its original IDs/checkpoints/admission and the protected password/storage-key
  files in the parent root: generation2 actively references those files. Do not delete
  the parent as disposable failure evidence. No new runtime/SDK/key copy was needed.
- Native16392/db0,32MiB main log/512KiB pages/1MiB index, string-only store, password
  authentication, checkpoint recovery/FailOnRecoveryError; Lua transaction mode true.
- Empty probe `/var/tmp/oasis-garnet-storage-probe`:1,524B retained receipts;16492 stopped.
  Its data/config were verified empty/owned and removed. No business writes occurred.
- Existing Linux Mongo8.3.13/store27039 is reused. Temporary
  cosmic_cql_mongo_garnet_demo3records were journalled, verified and dropped.
- All owned services stopped. Native SIGTERM exit-15 is recorded AFTER completed
  checkpoint; this is not a clean-dispose or uncheckpointed-crash-durability claim.
  Unknown source/checkpoint outcomes preserve live source/PID/socket and refuse replay.

Runtime preparation, explicit generation admission and native/paired runners live
under Scripts/TestHosts. Normal hub runs reuse admitted generation2; they cannot
silently initialize or replace it. [Resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
