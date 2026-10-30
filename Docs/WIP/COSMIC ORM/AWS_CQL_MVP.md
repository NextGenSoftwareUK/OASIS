# AWS CQL demo MVP

Verified30 October2026 on work/cosmic-cql-sandbox-integration. AWSOASIS inherits the generic DynamoDBOASIS CQL adapter with its own provider authority. This is DynamoDB Local, not hosted AWS.

## Supported scope

Canonical Id equality/membership, constants, AND/OR and bounded paging. Compiler emits at most100 native BatchGetItem keys; no full-store scan/managed business predicate. Exact server-owned authority/read catalogue required. Strongly consistent per-item reads do not imply a multi-record snapshot. Unprocessed keys, malformed/duplicate/unexpected responses and unsupported shapes fail without retry/partial publication. Native tombstone visibility and existing canonical validation reused. General metadata/content predicates, explicit ordering, aggregates and broader parity remain deferred.

## Current evidence

13controlled compiler/response tests and2native cases passed/no skips. Retained AWS seed unchanged:3native rows,2-row subset. SDK/HTTP/browser federation returns5physical copies(AWS3+Mongo2), subset4(AWS2+Mongo2); unknown presets/forged actor reject, unavailable AWS fails whole query. Actual browser all/subset/shutdown clicks verified. Exact owned Mongo fixture dropped/pending journal absent;18002/27029/5096 listeners absent and runner exit0. Retained seed/Dynamo data/container/runtime remain reusable.

Raw evidence under TestResults/AwsCqlCurrent (not committed):

| File | SHA256 |
|---|---|
|dynamo-cql-scoped.trx|4043DE93AAF092EE5080171C3CCA3D694B8A90D756270CA89E4EA231608808F9|
|aws-cql-native.trx|3CFB04EF1E5F0FE884E9CDEA32C25CC14EC0EF7FBCEBAE64A9BB14033E3C7DC8|
|aws-mongo-federation.trx|CB147E66C538C7EE2EC1CCFEE3F7A928BAB4353291156610DCFBA90CB06F2A2A|
|aws-browser-scoped-proof.json|A1FC7B760D475EAF4A4D72C07F2D25888EC422900C044C43CB304C6536F5A10B|

Browser screenshot: aws-mongo-browser-scoped.png. Previous passed native TRX hash-preserved in Archive before running.

## Launch

From C:/Source/OASIS-cosmic:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicAwsCqlNative.ps1 -Federated -Serve
```

Runner owns build/environment leases; do not nest. Open http://127.0.0.1:5096/ . Stop with page button and wait for teardown. Preserve/hash-archive previous reports before rerunning. Reuse LocalState/COSMIC/AwsCql and existing configured container/runtime; never reset/reseed unknown outcomes. The read-only shared JAR path is recorded in ENVIRONMENT_AND_CLEANUP_LEDGER.md. No packages downloaded/restored or new environment created this verification.

Next: catalogue enrollment, then remaining red demo breadth rows. Hosted AWS and general metadata/text querying remain open; this milestone is not full parity.
