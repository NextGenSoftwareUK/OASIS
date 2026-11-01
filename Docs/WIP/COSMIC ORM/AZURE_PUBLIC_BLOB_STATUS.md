# Azure public Blob CQL status

Updated 1 November 2026. Real anonymous Azure properties and manager/LINQ3/2 pass. Local HTTP/federation qualification passes; exact fixture cleanup and owned Mongo shutdown are verified. Hosted sandbox publication remains open; this is not the deferred Cosmos DB hosting requirement.

## Verified

- Explicit AzureStorageOASIS read-only representation, independent of canonical Azure writes and account DNA. Existing Azure.Storage.Blobs12.23.0 reused; no new SDK/account/paid service.
- Fixed Microsoft Azure Open Datasets catalogue: one county-population Parquet file, a committed-manifest marker and SUCCESS marker. Only properties are read; no census content downloaded. Native HEAD probe200 for all3.
- 23 controlled tests pass, zero skips: anonymous HEAD-only SDK transport, selection/paging/absence, no partial failures/retries, cancellation, integer precision and strict credential-free catalogue.
- Native SDK1test and actual documented manager/LINQ1test pass, each3/2records, zero skips.
- 43 API admission/example/identity controls pass. Provider-specific512-character bound handles the real137-character filename; other profiles retain128. All 92 frontend client tests pass, including long-key private-copy/reuse controls; not published.

- Local shipping HTTP gate1passed/0failed/0skipped: native3/2, paired5/4, foreign-private0/public-pair3;2SDK-owned private saves and9provider receipts. Exact fixture ownership/count checks precede verified database deletion; source catalogue unchanged. Original fixture/save/cleanup ACKs retained; do not replay.

- Additional read-only boundary test passes: direct Save/Delete return errors and never dispatch a native mutation. Core task branch d133cc20c29441158da0e590d7f3c85b351dd826 and frontend task branch9bc8c560301f8ab4d05427723740be819003888b are pushed/remote-verified; not yet integrated into their deployment branches.

## Evidence

Raw TestResults/AzurePublicSourceCurrent:

| File | Result | SHA256 |
|---|---|---|
| controlled.trx |23passed/0failed/0skipped|88d9cf5f35231e276f87d04921fa0ae32c2b9be374353f889e58ca610346e662|
| readonly-boundary.trx |1passed/0failed/0skipped|56986c73efc508c367bc54bb504c12eb4d9b74e355632aa92f7bbdba3b374d85|
| native.trx |1passed/0failed/0skipped|daaaa4d43de78de920171580073c012ef766773f1574f2355ecbd23e363a8650|
| manager-native.trx |1passed/0failed/0skipped|33d614eec9a630c75798d99146cf8bc5b9cd30e2699354c19083119a076581f7|
| mongo-http-native.trx |1passed/0failed/0skipped|fc9999a8bd7bd40850888aba202782387b513eed53117f00f099b2b97bd5d956|
| host-controls.trx |43passed/0failed/0skipped|ea6ba16a9cbdf770863714abf8bc8a6c1bb3f3d20d6e9f7f575a7f4848b28a05|

Durable source LocalState/COSMIC/HostedAzureBlobPublicSourceCurrent/source.json451bytes/SHA256cdd1607e68e647fc2c3d932abd151658479eaa2aa0e2b1bbfb730c7fe2be1124. Preserve its owner/keys; never recreate them per run. Failed marker-only and single-Parquet selection observations are retained separately from the actual selected three-object catalogue.

## Next

1. Local HTTP/Mongo gate completed; retained fixture.cleaned.json proves exact owned database deletion. No Mongo listener remains; do not replay.
2. Actual C# console binary also passes3/2. Next prepare/review free query-only admission and four server presets without modifying canonical DNA, then publish verified code/config.
3. Complete the first hosted cohort with native3/2, paired5/4, repeat reuse, privacy and exact replicas; publish the sandbox card/local guide links only after honest evidence. No green cohort replay.

Limits: key equality/IN only, up to8configured keys, per-object properties/ETags, no shared snapshot, general scans, native mutation or full ORM claim. Browser gates remain separate. [Guide](../../Devs/AZURE_PUBLIC_BLOB_CQL_GUIDE.md) | [Current programme](CURRENT_STATUS.md) | [Resources](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
