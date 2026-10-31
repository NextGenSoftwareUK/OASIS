# Current COSMIC ORM / CQL status

Updated 30 October 2026 UTC. Full programme remains incomplete.

## Current phase: demo breadth and missing provider integrations

- Worktree C:/Source/OASIS-cosmic; parent/Core branch work/cosmic-cql-sandbox-integration. Never build/write in the original C:/Source/OASIS checkout.
- Development parent51f3c4c3a1f70ccd4c30a09533c1cdc4861115b3 / Coreff670ec65eb2d6e98ec0858af146eba704752b20; ONODE6b5d9d00c6ec6e2ed4e9d04015032a0394973f06 preserved. Pins/gitlinks/remote pushes verified.
- Exact Railway WEB4 deployment1c73a0b5-3d76-4cd6-9aab-1fb178ebd45b SUCCESS, health200, running process commit/deployment checked. One consolidated repair batch; no replacement build.
- Hosted acceptance35/35 HTTP steps, no failures:3 acknowledged saves;9 exact point reads;5 CQL forms per provider (name, description, metadata AND/OR, paging, count); Mongo/SQLite/Neo federation6 physical coffee copies;3 cross-user isolation checks. Two free-plan/pay-as-you-go-disabled sessions.
- Evidence TestResults/Web4SandboxCqlCurrent/hosted-demo-verification.json SHA2563B3D58D8754395F2EDB1A29015D52651B1294155FCB97517FBBC6B8900DC2DEA. This proves this demo flow, not full EF parity or all failure/placement semantics.

## Hosted replication blocker repaired

- Mongo queue sequences6/version4 and8/version17 were missing initial SQLite enrollment. Native read-only inspection proved no target current/head/history/matching receipt for either root.
- Both explicit trusted baseline applications succeeded and durably source-completed, preserving original failure evidence. Ordinary worker drained subsequent obligations; original cohort has3 SQLite +3 Neo Completed obligations and no unfinished SQLite head. Actual original SQLite3 identities/names/owners/version1 checked.
- Root6:874fe30b-7702-4905-9e5b-ffa93a8621f8; initd1779b77-77bb-4dd4-b558-65985ae62db0. Root8:715eb09e-ce75-4ab3-9f4e-e76c0493281a; initb1c16263-bec5-4bc7-9c7d-5b05ec8da7ee. Never reset/skip queues or replay business saves.
- Remote protected requests/journals /data/COSMIC/ReplicaRepair/request.json and request-8-reviewed.json (+.application.json/.lock) remain required recovery evidence. Local matching requests under LocalState/COSMIC/HostedReplicaRepairCurrent. Original denied request-8.json preserved separately.
- Root8 review initially confused record creator with queued writer. Policy denied before journal/submission. Diagnostic now reports payload ActorId separately from CreatedByAvatarId; native regression with differing creator/writer passes. Corrected request retained the SAME initialization operation, no new write identity.
- Retained-copy/repair evidence TestResults/Web4SandboxCqlCurrent/hosted-retained-copy-verification.json SHA25647B157DE38B3BB1FAD2F17F47FD5DEE2C5838D3D64F63D40CEDDF942C03C8789.

## Verified repair components

- Initial baseline primitives on SQLite/Mongo/Neo: native absence/history/receipt fences, atomic graph/receipt, separate fingerprint domain, exact replay and sequential-next-version protection. Mongo receipt-membership migration implemented/tested; Neo receipt audit bounded10000.
- Authorized source preparation and read-only target outcome inspection implemented; durable metadata journal before submission, exact receipt recovery after lost response, current policy reauthorization and no resubmission after absent uncertain outcome.
- Mongo source completion compares original operation/payload/attempt/head atomically, preserves original failure evidence and records exact initialization acknowledgement. Shell-only sandbox command --cosmic-repair-baseline uses configured Mongo-to-SQLite route; no public repair endpoint.
-23 shared controls +8 Mongo native cases +1 real Mongo-to-SQLite command scenario pass/no skips.71 API/schema/admission controls pass. Latest command regression TRX SHA2564C35B531535ED2D4768E8EC44815437AA0C25BE64D0EF772526EF0C1781DD33A. Owned native fixtures removed/services stopped.
- Point-load explicit-provider routing and durable worker barrier diagnostics are included in deployed batch. Legacy HyperDrive untouched.

## Fixture and credential ownership

- Current LocalState/COSMIC/HostedSandboxCqlCurrent/fixture.json completed=true/pending=null,3 acknowledged records; SHA25668DCBBADB22F5CE3359DE4BE6B256599161307306FB35838C984F6ACE86699B7. Preserve current records/session credentials for demo; do not blindly rerun/reseed.
- Current sessions500d722c-335e-4c0c-86da-d6c05325ad7e and a50e51b1-220b-43c5-983e-145761a52bcd use separate630-byte Windows DPAPI files in the same protected state folder. No bearer in journals/docs/logs. Eight offline verifier controls pass.
- Old fixture archived intact at LocalState/COSMIC/HostedSandboxCqlCurrent/Archive/initial-without-session/fixture.json, completed=false: writes/delivery reconciled, owner JWT lost before protected-session support. Original3 private records/9 copies retained for later authorized teardown; never impersonate/replay old owner.
- Required evidence/resources and lifecycle in ENVIRONMENT_AND_CLEANUP_LEDGER.md. Remote SSH key cosmic-cql-demo-inspection still registered/reusable, not removed; exact paths/fingerprint in ledger. Unknown root logs untouched. No new installs/downloads/restores/environments.

## Next demo work

1. Oracle content pairing now verified:13 compiler controls,21 retained native queries,4 C# +4 HTTP +4 browser presets,2 rejected request shapes. Name/description/metadata/AND-OR counts2/4/4/2; source unchanged, owned Mongo fixture cleaned. Run-ReusableOracleMongoTests.ps1 -Content -Serve -ArchiveCompletedDemo serves5090.
2. Web4Site CQL tab now published: feature0810238/main dd90f1d. Public https://web4.oasisomniverse.one/web4-sandbox.html has Oracle local catalogue; production deployment7449727369 success/client SHA matches.17client controls and catalogue->native metadata result4copies verified; hosted Oracle remains unconnected.
3. IPFS scoped native multi-CID profile now verified:23 shared admission +17IPFS controls +1real Kubo/Mongo scenario pass/no skips;3selected native rows,2-row subset,5/4federated copies through SDK/HTTP. Core task0d41300/pin updated, not deployed; current runtime/routing remains unchanged. SOLID now also verified:31 controls +1 real Pod/Mongo scenario,3 native records/5 federated copies, exact owned cleanup confirmed. ActivityPub now verified:10 controls +1 real public Mastodon/Mongo scenario,3 native records/5 federated copies and exact Mongo cleanup. Azure remains pending/free-only; deep LINQ/POCO/migrations deferred.
4. Broader failure-injection/failover/load-balancing and gap-free future enrollment remain explicit acceptance gaps; this demo flow is not universal HyperDrive completion.

## Branch recovery and key commands

- Broad parent work/cosmic-cql f89cf0085 / published Core69bb8a4 preserve PostgreSQL/SQLServer/Oracle content work excluded from deployment. Broad Core also has a local unpublished conflict-free merge with scoped Core; parent merge attempt was aborted. Do not sweep broad changes into Development.
- Next exact step: inspect preserved Supabase REST runtime/source and selectively integrate its missing current-branch/Mongo/browser path from f89cf0085; preserve hosted-vs-local data-plane limits. PocketBase41controls + native1 + restart1 + Mongoidentity1 + paired1 pass/no skips (45), browser5/4; native4record projection/payload baseline unchanged, allnon-databaseidentity/password/ACK files unchanged. ExactMongo cleanup,18090/27039/5124 stopped/ownedservicesexit0. See POCKETBASE_CQL_MVP.md; historyarchived. Public section30 main32fba52/Production7453220231success. Priorverifiedscopes frozen; Garnetoriginalsource loss and LitestreamWindows failure remain red; Upstash expiry2026-10-31T21:15:49Z/permanent hosting red. Hosted Azure pending/free-only; full parity/HyperDrive/public-network acceptance incomplete.
- PostgreSQL16controls/17native/restart/4paired presets; SQLServer17controls/23native/restart/4paired presets verified. Oracle13controls/15read-only probes/21saved-holon queries/restart plus4paired SDK/HTTP/browser content presets verified locally; hosted OCI still unproved.
- Build through Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1, /m:2 /nr:false /p:UseSharedCompilation=false, reuse caches. No restore unless necessary. Test-CqlReplicaBaselineRepair.ps1 runs explicit reusable Windows native profile after producer builds.
- Validate pins: python Scripts/validate_railway_dependency_manifest.py --require-gitlinks. Fetch first; batch verified Development releases once/twice daily. Latest client-only verifier/probe/docs commits need no new Railway build.
- Logs/TRX only worktree TestResults; persistent credentials/intent/ACK journals only protected LocalState or retained remote volume, never TestResults/Docs.
- Recovery links: [strict checklist](CQL_PARITY_CHECKLIST.md), [baseline/recovery contract](REPLICA_BASELINE_AND_RECOVERY.md), [release evidence](CQL_SANDBOX_RELEASE_2026-10-30.md), [after-demo backlog](AFTER_DEMO_BACKLOG.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
