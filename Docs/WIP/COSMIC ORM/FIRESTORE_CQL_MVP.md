# Google Firestore CQL demo MVP

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
