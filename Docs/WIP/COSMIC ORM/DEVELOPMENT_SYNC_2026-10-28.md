# Development integration — 28 October 2026

## Latest sandbox/DNA integration

Upstream parent583bb5aa7/Core0d5e1e2 merged into task:Core mergec194d536 and parent merge d8c2db83a. Native route/snapshot implementation preserved. DNA/Core build both frameworks;affected provider readers,Shared,BootLoader andWEB4 build. DNA19/Core4411tests pass. Missing DNA.UnitTests assets recreated from local NuGet/runtime caches only,no downloads.

Evidence:TestResults/OwnedRoutingCurrent/dna-sync-dna.trx (19passed,SHA256 9A9A76546966D99D428262153A276FDC59D0942ED4B6CB74ABC804D67235A982);dna-sync-core.trx (4411passed,SHA256 F0219AAF1E68018355610F930D8B936024F38CD9FCE5046727111CED38E72D12). Adjacent dna-sync-* build/restore logs. No native runtime fixtures started for this compatibility gate;earlier native proof is not recertified by these counts.

V2 deployment gate:old type-only replica-head source IDs differ from new native-bound source IDs. Native lineage guards reject mismatched source lineage. An explicit audited migration/repair path is still required;do not rename heads automatically or claim seamless rollout over existing replicated data. This is V2 protocol/data evolution,not implementation work on legacy HyperDriveV1. Current work stays on work/cosmic-cql.

---

Merged parent Development 953cb7968 and Core Development 261fce1 into work/cosmic-cql in C:/Source/OASIS-cosmic. Core merge c1b8bd8a531de1e1812a678684cd335876d16779 was pushed and verified with git ls-remote. STAR dependency f005d8d is fetched and checked out locally. Original checkout untouched.

Preserved CQL SOLID record-key-set configuration alongside incoming NativeImportContainers. Retained native-import walkthroughs, replaced obsolete missing-reader/CLI claims with incoming current gaps. Core gitlink and deployment manifest advance together. The per-avatar HyperDrive guide is now present.

Verification (existing SDK10.0.401/cache, no restore/download/service startup):

- Core both target frameworks build; full Core suite:4377 passed,0failed,0skipped.
- DNA, SOLID, Solana, Cosmos, IPFS, ActivityPub, BootLoader and WEB4 build successfully with bounded workers and one build lease per sequential batch.
- GraphQL shipping schema:1passed,0failed,0skipped.
- First BootLoader attempt failed against stale Solana/Cosmos assemblies; rebuilt native producers then BootLoader successfully. Failed log retained as bootloader-stale-dependencies.log. No code workaround.
- Existing warnings remain; no new runtime/native replication certification is claimed by compatibility builds.

Raw evidence: C:/Source/OASIS-cosmic/TestResults/DevelopmentSyncCurrent (regenerable, not committed).
Core TRX SHA256:B58CC8F58782E439CE2A2BBFEC5797C7F65D454E6A772BAE9C6B0B0F06175F8E.
Schema TRX SHA256:5E63F100F7F13D8B06A9413CE35AF123B72CDB8077C214759A8F47945047B16A.

Whitespace check treats CRLF as line endings, preserving incoming byte-pinned native reports. No raw evidence was rewritten.
Next:resume immutable endpoint/store/policy route binding; hosted Azure remains pending, LINQ depth paused. This merge is on the task branch, not a new Development deployment.
