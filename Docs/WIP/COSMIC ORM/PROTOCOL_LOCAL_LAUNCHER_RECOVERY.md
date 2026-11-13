# IPFS / SOLID / ActivityPub paired local launcher recovery

Verified2November2026. Source/consumer recovery only; no native execution.

Restored Run-ReusableProtocolMongoTests.ps1 and full selected-key teaching
consumer closure. Inner IPFS/SOLID runners now admit CqlKeySet mode, preserve
single-mode/archival/pending-journal checks and select one unskipped native case.
Native test/teaching source includes three-record selection, subset filtering
and explicit Mongo-paired preset. Existing shared NativeProtocolMongoAcceptance
source matches retained pin and remains unchanged. ActivityPub native key-set
consumer already existed; compiled unchanged with the shared helper addition.

Three runner syntax checks and inner-mode PowerShell AST checks pass. Actual
IPFS, SOLID and ActivityPub native/federated HTTP consumers all compile using
cached dependencies. SOLID initially failed CS1739 on an obsolete factory flag;
only the recovered test now uses settings.EnableCqlRecordKeySets, preserving
current shipping SolidProviderFactory/DNA/public import configuration.

No provider/API code was restored backward. Newer hosted activation/source tests
remain intact. No Kubo/Pod/Mastodon/Mongo service, identity, key or original native
fixture was opened; accepted cohorts were not replayed. Compilation does not
establish current runtime/hosted/browser availability.

```powershell
Scripts/TestHosts/Run-ReusableProtocolMongoTests.ps1 -Provider IPFS -Serve
Scripts/TestHosts/Run-ReusableProtocolMongoTests.ps1 -Provider SOLID -Serve
Scripts/TestHosts/Run-ReusableProtocolMongoTests.ps1 -Provider ActivityPub -Serve
```

These commands require the existing admitted environments/original journals;
they were not executed in this recovery. Preserve failed/pending reports and
original identities. Do not replace/reseed them to obtain another green proof.

Compact build-log hashes/mode evidence:
LocalState/COSMIC/LauncherRecoveryCurrent/protocol-launcher-recovery-evidence.json.
Query-profile counts remain95local/60hosted. Published missing launchers3to2.
