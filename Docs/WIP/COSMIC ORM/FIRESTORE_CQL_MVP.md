# Google Firestore CQL demo MVP

## Owned-source recovery — 2 November 2026

7 controlled compiler/admission/cancellation cases pass, zero failures/skips.
Provider/native/federated consumer compile and launcher syntax pass. Public
Google source, README and public walkthrough links preserved. No original
emulator/export/credential/Mongo fixture opened. Native acceptance below is
historical; current emulator/hosted Firestore availability remains unverified.
Bounded document-key batches are not collection-wide metadata search or a
distributed snapshot; name/metadata/graph queries are unsupported.
Original TRX SHA256 `1ca6a1dc29fbc677e35d39f2c5e071e257ab5949eda2db19c2cde624ef7743f3`; retained manifest
`LocalState/COSMIC/LauncherRecoveryCurrent/firestore-recovery-evidence.json`.

### Recovery laptop verification

The official emulator 1.22.0 JAR matched the retained SHA-256 and is
reused with the existing Temurin 21 JRE. Run
`Scripts/TestHosts/Run-CosmicFirestoreCqlNative.ps1 -RecoveryLaptop`
for an independent local fixture. The runner selects the parent Core pin
under the shared lease and keeps build output, native logs and the test
report in a unique run directory. This laptop lacks the earlier checkpoint.
The independent native test passed on Core
`2abc7100acbdf2e4e6d8249c24f7cf5746daba34` without skips. Native results
were three full, two selected and zero absent, with unchanged document
revisions. The run wrote a hash-verified local checkpoint and stopped the
emulator. The native TRX SHA-256 is
`3406eb73a49157cb5ddb1cfc5d3554e30dcbc3b66f53efa5c9a1de97647243cc`.

The recovery `-Federated` run imported that checkpoint and passed the
native restart and the paired manager and HTTP tests with zero skips.
The paired flow returned five full and four selected records. Both seed
and checkpoint hashes matched the first run. The paired test removed its
owned temporary Mongo database, and Mongo reported native storage closure
and shutdown. All fixture ports closed. Native restart and federation
TRX SHA-256 values are
`a10024e9b3cfd3c5997e55baade57b2d824ab90a750f6ed280c1da8edf475e60` and
`df1af7635d2da54f62c4073a7596d5e75588f46a097ad8735802298147c218e5`.
Recovery-laptop browser and hosted Firestore verification remain open.

Verified30 October2026. GoogleCloudOASIS generic CQL adapter against retained Firestore emulator1.22.0. This is not hosted Google Cloud.

Compiler supports authorized Id equality/membership, constants/AND/OR and bounded paging into at most100 native document references. No store scan or managed business predicate. Native documents are validated/authorized before paging; unsupported shapes, foreign/corrupt payloads and transport failures fail without partial publication. General metadata/text queries, explicit ordering/aggregates, hosted acceptance and wider parity remain open.

7controls+2nativecases pass/no skips. Original checkpoint/seed unchanged:3records/subset2. Shared manager SDK/HTTP/browser federation5(Google3+Mongo2)/subset4(2+2) pass with provider provenance. Invalid/forged input rejects, unavailable source fails whole query. Exact owned Mongo fixture dropped/pending journal absent;18084/18085/18086/27029/5100listeners absent and runner exit0. Original hash-validated checkpoint/seed preserved; no download/install/restore/reseed/new environment.

```powershell
# From C:/Source/OASIS-cosmic; runner owns leases.
pwsh -File Scripts/TestHosts/Run-CosmicFirestoreCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5100/ . Use page stop button and wait for cleanup. Preserve/hash-archive old reports before rerunning. Retained checkpoint/runtime required. Native failure may deliberately preserve the emulator for reconciliation; do not blindly restart/reset it.

Raw evidence under TestResults/FirestoreCqlCurrent (not committed):

| File | SHA256 |
|---|---|
|firestore-scoped-controls.trx|2E136C6A8C6FE480167DE5424A0C0DDBBEDA9B8701518FC0F0C0BCD21AE22EBB|
|firestore-cql-native.trx|4176E59E5F5A56E8D7AC3D0C06C7ED877E3BE93D7E119479F6DBC9A5FD5954E1|
|firestore-mongo-federation.trx|9D4C09C7F355D80AA2EB63CCF00AFD726C512C2E483CAF1FF79C67F99088A06B|
|firestore-browser-scoped-proof.json|0ECFCDFF62D383E5A6CD20C79E39D6A555AFCCD0EBDD5FD7C17FEEE4793258AF|

Screenshot firestore-browser-scoped.png. Previous native reports hash-preserved in Archive. Shared resource ledger records retention/cleanup. Next catalogue enrollment, then Cosmos blockchain/Polkadot/NEAR breadth per user order. Full roadmap remains active.
