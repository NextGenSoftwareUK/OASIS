# COSMIC ORM / CQL — current status

Updated 31 October 2026. Programme incomplete; priority is actual hosted sandbox breadth.

## Exact next step

1. Publish the tested IPFS SDK correction, then rerun actual hosted leaf3/2 +Mongo5/4/repeat/isolation. Current deployed1b1105ba4 has the reproduced ABI failure; still4verified hosted providers. Correction now passes28shipping API/schema controls +27provider/codec controls +1retained native3/2case. Exact upstream0.55source rebuilt for shared SimpleBase5.6.4; executable/test composition explicitly selects the source SDK (library PrivateAssets alone had left the old package in the caller). API/test/package DLL hashes match. No fallback parser/global downgrade/assembly copying. Source catalogue/key/peer/config/ACK preserved; before-redeploy server state recorded. No Mongo preparation writes yet.
2. For each provider: explicitly enroll actual native read capabilities, expose ready filtered multi-record examples plus one Mongo-paired preset, verify the normal public sandbox visitor flow and isolation. Keep local demo links alongside hosted choices. Do not advertise an unavailable source as working.
3. Keep deep LINQ/POCO/migrations tooling paused; work only red checklist scopes, freeze greens/cursors unless a reproducible bug is found. HyperDrive implementation is V2 only; legacy is document-only.

## Working copies and release ownership

- Work/build/test only **C:/Source/OASIS-cosmic**, parent **work/cosmic-cql-hosted-release**; Core **work/cosmic-cql-sandbox-integration**. Never mutate/build C:/Source/OASIS.
- Parent **Development 51fc64f5e39017c99d4e6bfd9acb8343b41c9b19**, Core **Development249a223a2f169dd72dfc850fbf6a3dac345642f7** pushed/remote-verified; pins/gitlinks match. ONODE Core6b5d9d00c6ec6e2ed4e9d04015032a0394973f06 preserved.
- Railway WEB4 deployment **14b4e5e7-6646-4e20-b05f-a3025b3819d1 SUCCESS**, exact parent51fc64f5e. Existing dev service/volume, no new paid infrastructure.
- Frontend **C:/Source/Web4Site**, task work/cosmic-cql; public main 2b46dedfcfc41695ba36d232a1ff098e4ae711bc / Production 7456696386 success. Both published scripts byte-match tested source.
- Broader local-provider source is preserved/pushed on parent **work/cosmic-cql-sandbox-integration237403a75**; scoped release does not include that accumulated540-file delta. Unknown root logs preserved.
- Named staging only; no add-A, force-push or extra worktree. Batch verified Development pushes once/twice daily. Current milestone is deployed; subsequent doc-only task commits need no immediate redeployment.

## What is actually verified live

- **Four hosted query providers: MongoDB, SQLite, Neo4j, ActivityPub.** Local-only catalogue entries are not hosted coverage. Azure remains pending: free-only, no existing subscription; do not re-ask signup/payment.
- Original three-provider baseline35/35:3 acknowledged saves,9 exact copies,5 query forms per provider,6 physical federated copies and3 isolation checks. Retained SQLite lineage barrier was explicitly repaired with fenced/acknowledged application; no queue reset or blind replay.
- ActivityPub native public leaf queries return3 posts and2-post subset in HTTP/browser. Membership request-capture defect was reproduced/fixed/deployed; original failed evidence preserved. Response envelope is result.rows, not result array.
- **New live visitor preparation:** sandbox-60b2bb72 saved2 private Mongo snapshots using Core-native owner IDs and normal create-only writes. Repeat setup0 saved / 2 existing. No edited/foreign records overwritten; uncertain outcomes remain journalled without automatic retry.
- **Federation:** AP3+Mongo2=5 actual rows; AP2+Mongo2=4 subset. Independent sandbox-46616f4f sees3 public AP rows only; exact foreign Mongo IDs return0.
- **Replication:** those2 new writes also appear on SQLite and Neo4j with matching ID/name/description/version/owner/private flag/metadata. No manual repair or source replay in this gate. This does not certify the full failover/load-balance/fault-injection/late-enrollment matrix.
- Server operation ID is now visible in the browser. Local developer links remain. No source AP posting or native write occurred.

## Current contracts and verification

- Query-only enrollment fences writes before provider/outbox I/O. Read-only activation, tariff capture, visibility and real native capability planning own discovery/execution. Missing dependencies disable only affected examples.
- Version1 discovery remains leaf-only; version2 includes fixed Rows/Federation operations. Explicit provider scope, bounded8 targets / 800 input rows / 8 attempts; captured server ceiling rejects over-budget fan-out before I/O. New protocol/unordered profiles have no continuation contract.
- Preparation uses server-derived NativeRecordHolonMapper identities, private snapshots, normal create-only CAS and per-operation origin-local intent/ACK. No duplicated client hash or automatic replay. Controls enabled only on sandbox configuration.
- Pending IPFS task code: bounded native CID membership restored from preserved implementation, explicit DNA/BootLoader enrollment and cancellable read-only native activation.23 controls pass; one native retained-source case returns 3/2 matching IDs/names/CIDs and repeat verifies without saves. Initial blank-version readiness defect reproduced/fixed. Full API build passes. This is preparation, not a fifth hosted provider.
- Latest changed-scope controls **86 API + 383 Core + 39 Node = 508 passed**, zero skipped. Earlier federation32 controls and full transitive API build also pass. These counts are not full ORM parity or universal-provider proof.
- Live raw evidence: **TestResults/HostedCqlIntegrationCurrent/browser-federation-acceptance.json**, browser-activitypub-mongo-{five,four}.json, browser-activitypub-mongo-four.png, isolation/replica reports. Counts/hashes and prior failures: [integration plan](HOSTED_SANDBOX_INTEGRATION_PLAN.md).

## Durable state and resources

- **LocalState/COSMIC/HostedSourceRolloutCurrent** holds DPAPI-protected original/candidate variables, configuration intents/ACK, deployment observation and browser acceptance ACK. Four AP examples applied; only example configuration changed in this batch. Never print secrets or replay an uncertain mutation.
- **LocalState/COSMIC/HostedIpfsSourceCurrent** retains one new synthetic public 3-record cohort, original-key encrypted catalogue/DNA/intent/ACK and 4 CAR exports (3,788B; total state 13,571B) on the reused IPFSCurrent node. Both corrected runs stopped the owned daemon; no listeners 18547–18549. Host runtime/repo still absent. Guide/runner: Docs/Devs/IPFS_CQL_HOST_CONFIGURATION.md and Scripts/TestHosts/Prepare-HostedIpfsSource.ps1.
- Sample avatars/2 private records / 4 replicas remain subject to normal sandbox expiry/cleanup, not claimed removed. Owned browser tabs closed; tokens lived only in page memory. Original baseline fixtures/repair journals preserved separately.
- Existing Railway project712f50f6-3b7b-46d4-b229-44b49922f16b, WEB4 service5123199a-43c4-4439-9921-e10e2aec7841. No new runtime/download/local restore/native environment.
- Cleanup already removed15,310,104,295B disposable output. Latest C free recorded in [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md); preserve shared runtimes/caches/credentials/native originals.

## Key commands and recovery

- Build via Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; /m:2 /nr:false /p:UseSharedCompilation=false. Reuse SDK/assets/cache, no unnecessary restores.
- Validate committed pins: python Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- Frontend: node --test cql-playground.test.cjs cql-demo-preparation.test.cjs sandbox-api.test.cjs.
- Logs/TRX only worktree TestResults; durable state only LocalState. Current public demo: https://web4.oasisomniverse.one/web4-sandbox.html.
- Full scope: [strict parity checklist](CQL_PARITY_CHECKLIST.md), [roadmap](CQL_COMPLETION_ROADMAP.md), [implementation](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md). No overall completion percentage is inferred from focused demos.
