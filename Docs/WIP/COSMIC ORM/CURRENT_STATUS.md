# COSMIC ORM / CQL — current status

Updated31 October2026. Programme incomplete; priority is actual hosted sandbox breadth.

## Exact next step

1. Add the ready ActivityPub + Mongo browser preset and2acknowledged owner-scoped Mongo demo records; prove actual5/4physical rows and isolation. Protocol federation admission is implemented/tested on the task branch (32Core/61API controls), not deployed. Generic server examples currently describe leaf queries only; extend their explicit operation contract before adding federation presets.
2. Reuse normal admitted saves with durable intent/ACK and stable per-avatar demo identities; avoid duplicate setup after refresh. Native import uses an avatar-scoped identity domain. Inspect effective authenticated metadata policy, not just legacy DNA flags; do not disable encryption as a workaround.
3. Bundle the tested CORS operation-header correction with the next verified backend batch, then verify the real browser sees its operation ID. It is committed on the task branch only, not deployed.
4. Continue hosted Oracle/IPFS/SOLID/Solana/AWS/Azure and remaining breadth. Azure remains pending/free-only, no existing subscription. Deep LINQ terminals/POCO/migrations tooling remain paused.

## Working copies and release ownership

- Work/build/test only **C:/Source/OASIS-cosmic**, parent **work/cosmic-cql-hosted-release**; Core **work/cosmic-cql-sandbox-integration**. Never mutate/build C:/Source/OASIS.
- Parent Development6735c4d461614be5ce9010d528b26cb870ae8fff and Core Development6963c50eaec8c4ef042b453a2431443e17ceaed0 are pushed/remote-verified; pins/gitlinks match. ONODE Core6b5d9d00c6ec6e2ed4e9d04015032a0394973f06 preserved.
- Broad local-provider programme remains on remote parent work/cosmic-cql-sandbox-integration237403a75; the scoped hosted release excludes that accumulated540-file catalogue delta.
- Web4Site **C:/Source/Web4Site**, branch work/cosmic-cql; public main16553be/Production7455754340 SUCCESS. Existing local demo links remain.
- Unknown untracked root logs preserved. Never git add -A or force-push. Batch verified Development pushes once/twice daily; second batch fixed a reproduced live query bug.

## What is actually verified live

- **MongoDB / SQLite / Neo4j**:35/35 authenticated HTTP acceptance steps on baseline deployment1c73a0b5:3 acknowledged saves,9 exact copies,5 query forms per provider,6 physical federated coffee copies and3 cross-user isolation checks.
- Initial missing SQLite replica baselines repaired through fenced atomic application and exact acknowledgement; ordinary worker drained subsequent obligations. No queue reset/skip/business replay. Broader failure-injection/failover/load-balancing/future-enrollment matrix remains incomplete.
- **ActivityPub** correction deployment94623078 SUCCESS at6735c4d46: authenticated HTTP and public sandbox browser both return3actual public posts and the2-post subset. Four providers now have verified hosted queries; ActivityPub Mongo pairing/full demo acceptance remains incomplete.
- First actual ActivityPub In query returned400 CQL_REQUEST_INVALID, operation a3e18ac8-9c55-ef15-e30c-9e7c09aa1dcc. Root cause: HTTP request capture rejected every array despite compiler membership support. Fixed and deployed in6735c4d46. Earlier ad-hoc verifier then incorrectly read result as an array; corrected verification follows the declared result.rows envelope, and identical full-response hashes reconcile that false0 report. Original evidence retained.
- **41 additional native/local/temporary-hosted demos are not hosted sandbox coverage.** Public catalogue headings39 are not a hosted-provider count. Local Solana browser/native-index and Oracle/IPFS/SOLID/ActivityPub multi-record/Mongo demos were previously verified on the preserved broad branch.

## Latest verified code and tests

- Explicit query-only admission fences storage writes before provider/outbox I/O; captured enrollment, read-only activation, configured-source discovery and capability-checked public examples. Defaults/write routes/failover/replication lists preserved.
- HTTP In produces one bounded native membership AST; no point fan-out or unbounded client scan. Captured literals remain owned after source mutation/disposal.
- Full transitive API build passes. Final API **427pass/0fail/12environment-dependent skips**, ActivityPub344pass/no skips, protected-session verifier8pass; Node client25pass. These are scoped controls, not universal provider/parity completion.
- CORS fix:3framework controls pass; current deployed browser shows operation ID not exposed. Correct host policy is prepared for next batch, no cookie/origin/method/request-header expansion.
- Native/browser evidence: TestResults/HostedCqlIntegrationCurrent/activitypub-hosted-wire-acceptance.json and activitypub-sandbox-{three,two}-posts.png. Native full/subset3/2, two independent normal free sessions; local links preserved.
- Final API evidence: TestResults/HostedCqlIntegrationCurrent/membership-capture-api.trx. Earlier original failed compile/read evidence retained. Counts/hashes in [integration plan](HOSTED_SANDBOX_INTEGRATION_PLAN.md).
- Protocol federation task Core249a223 is remote-verified/pinned: read-only captured attempt ceiling prevents caller4/server3 fan-out before I/O. New protocol scopes/AllEligible include tariff-enrolled read profiles, with bounded8targets/800rows/8attempts. New profiles/unordered sources have no continuation contract.32Core controls (12new+20existing) and61affected API controls pass; all are controlled-adapter tests, not hosted Mongo pairing proof.
- Concrete host registry stays in production-only CqlV1Providers composition; local demo native integration compiles without importing BootLoader.

## Durable state / resources

- **LocalState/COSMIC/HostedSourceRolloutCurrent**: original/candidate configuration protected with Windows DPAPI; durable deployment/configuration/session/query intents and acknowledgements. All3 variables observed; example-only newline conversion independently reconciled, no resubmission. No secrets in Docs/TestResults.
- Fresh normal free session identity is in sandbox-session.acknowledged.json; its token is session-<id>.dpapi. Original retained baseline session expired; preserve original records and credentials, never impersonate/reseed.
- Baseline fixture LocalState/COSMIC/HostedSandboxCqlCurrent/fixture.json completed=true/pending=null,3 acknowledged records. Original protected repair requests/journals remain locally and /data/COSMIC/ReplicaRepair remotely.
- Existing Railway dev project712f50f6-3b7b-46d4-b229-44b49922f16b, WEB4 service5123199a-43c4-4439-9921-e10e2aec7841. No new paid source/service provisioned.
- Cleanup removed15,310,104,295B of verified disposable test/VS staging output. Latest C free80,425,893,888B. Shared runtimes/cache/native originals preserved; full ownership/lifecycle in [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Commands and recovery

- Build through Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; /m:2 /nr:false /p:UseSharedCompilation=false. Reuse existing SDK/assets/cache; no restore unless necessary.
- Validate committed pins: python Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- Logs/TRX only worktree TestResults; durable credentials/intent/ACK only protected LocalState or retained remote volume.
- [Strict parity checklist](CQL_PARITY_CHECKLIST.md): work on red demo rows first; do not harden green rows/cursors without a reproducible bug. Legacy HyperDrive gaps are documentation-only.
- [Hosted integration plan](HOSTED_SANDBOX_INTEGRATION_PLAN.md), [after-demo backlog](AFTER_DEMO_BACKLOG.md), [baseline recovery](REPLICA_BASELINE_AND_RECOVERY.md), [previous detailed status](Archive/CURRENT_STATUS_2026-10-31_PRE_HOSTED_ACTIVITYPUB.md).
