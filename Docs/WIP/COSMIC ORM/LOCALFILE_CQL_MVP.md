# LocalFile + Mongo CQL MVP

Updated30 October2026. Current-branch native/C#/HTTP/browser integration verified. Historical source proof remains below.

## Demo

```powershell
Scripts/TestHosts/Run-CosmicLocalFileCqlNative.ps1 -Federated -Serve
Scripts/TestHosts/Run-CosmicLocalFileCqlNative.ps1 -Federated
```

The browser is localhost5110. One HolonManager query targets MongoDBOASIS and
LocalFileOASIS. Three retained file records plus two Mongo copies yield five
physical rows; selecting two IDs yields four. Each row preserves provider provenance.
The shared C# walkthrough and HTTP/browser examples have spaced code and comments.

Source uses the shipping provider's canonical GUID filename/decoder and common
bounded-ID compiler. It reads only selected files, never scans the directory to
filter content. It supports authorized ID equality/membership, Boolean algebra and
paging over at most100 IDs; metadata/content/order/children remain unsupported.
The result budget is1..10000 and the aggregate actual-byte read budget is4MiB.
Strict UTF8/identity/access validation rejects bad data; failures/cancellation
publish no partial rows. Only a missing individual file means absence; missing
source directories and access errors remain failures.

Files are plaintext: no record-encryption claim. Separate file/provider reads can
observe different moments; no cross-provider snapshot is claimed. Full history,
child writes, conditional writes and EF parity are separate acceptance gates.

## Historical preserved-branch evidence

- LocalFileCqlTests:7/7, zero skips. Catalogue isolation, unsupported shapes,
  inactive provider and cancellation. `TestResults/LocalFileCqlCurrent/localfile-cql-controls.trx`,
  SHA256 `BF7CF7BF4A7C61306004AF929A6BBF34C33DB8333D98E1D31A670181EB32D2CC`.
- LocalFileCqlNativeTests:1/1 initial and1/1 second-process reuse; native3/2 and
  authorized missing-file empty result; every source byte/path unchanged.
  Latest preflight `localfile-cql-native.trx`, SHA256
  `912FB420558D54C60B60BE8E9E78A120CA1A8BC0D4118AEE1BE59073652974D1`.
- LocalFileMongoCqlNativeTests:1/1 zero skips. SDK/HTTP5/4, provenance, unrelated
  Mongo ID excluded, forged actor/unknown preset rejected; inactive file provider
  yields HTTP503 without rows. `localfile-mongo-federation.trx`, SHA256
  `232AB77E0AE683226797D087C4339117CFBE5EAA17A96144572650B06B68B7CF`.
- Browser independently checked full5/subset4 and provenance. Screenshot:
  `TestResults/LocalFileCqlCurrent/localfile-mongo-browser.jpg`.
- Launcher routing:8 single/16 plans,18 paired/36 plans,22 paired rejections,
  3 archive rejections; listeners unchanged. `hub-routing.log` in the same folder.

## Retention and cleanup

Retained source `LocalState/COSMIC/LocalFileCql/source` plus seed/locks:5874bytes.
`demo-seed.json`173bytes, SHA256
`57ABA9D9930BB29886E5CD3D5D5B920CAC05F0B8E82C2902BE4CD47460EC266D`.
Reuse requires original source; pending setup must be reconciled, never replayed.
Runner and fixture own distinct locks so producer ownership does not deadlock the
consumer's source lock. Initial duplicate-lock wiring failed before source mutation;
fixed at that ownership layer, then verified native/paired execution passed.

After browser shutdown the same owned runner exited0. Exact temporary Mongo DB
`cosmic_cql_mongo_localfile_demo` was identity/content-validated and dropped; pending
and ready journals removed. Ports27029/5110 closed. Native source retained, no
runtime installation/download. Generated outputs remain in TestResults/bin/obj and
can be regenerated; never delete the original source with output cleanup.

[Current status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Current-branch integration acceptance —30 October2026

Selective port from preserved f89cf0085, without unrelated provider changes. Producer is built before consumer; original source is required. Runner archives successful reports by SHA256 and refuses unresolved reports before another execution. Dedicated5110 avoids the TRON5108 catalogue collision. Development51f3c4c3 fetched and already an ancestor of task HEAD.

Evidence under C:/Source/OASIS-cosmic/TestResults/LocalFileCqlCurrent/:

| Report | Result | SHA256 |
|---|---|---|
| localfile-cql-current-controls.trx |7passed/0skips |35F05A499B5EF009A70FDEEBBFE43685965019695B4325C5D1C45CEFB7E0E278 |
| localfile-cql-native.trx |1passed/0skips; original3/subset2/missing0, all source bytes/paths unchanged |08E6A7C5CBDD475314EABFCC84E36382C0978557F0254580BBAAC4368904F13B |
| localfile-mongo-federation.trx |1passed/0skips; SDK/HTTP5/4, isolation/rejections/inactive503, exact cleanup |8598CA24A37B3A12DEA7902FF158E8E0007F321963C21E6698E812FAE45FE0D5 |

Independent browser full5/subset4 verified; localfile-mongo-subset-current.png retained. After stop, runner exit0, Mongo fixture identity/content verified and dropped, pending/ready gone,27029/5110 stopped. Original5874-byte source/seed retained, seed hash unchanged. No runtime install/download/newenvironment.

Public section16/expanded command/fixed5110link verified: Web4Site main7bcfd230486837f97dde185406d92a407039c4f7, Production7452447621success. Screenshot localfile-catalogue-public.png SHA2563CB7A2E4759378F52E3E4B57D85228BE1685EFD56FDD2F7B8EB0E838A47209F1. HTML-only addition; hosted selector remains three databases. Backend changes remain task-branch only, no Railway deployment.

After-demo gaps remain plaintext/encryption, metadata/content queries, child semantics, conditional writes, broader HyperDrive and full parity. This bounded profile does not turn those gates green.
