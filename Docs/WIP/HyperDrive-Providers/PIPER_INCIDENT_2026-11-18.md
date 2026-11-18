# PIPER authentication and audit incident — 2026-11-18 UTC

Status: investigating; no incident fix or restored authenticated acceptance claimed. Hold the unrelated admin-pagination/API and OPORTAL publication while isolating this failure.

## Confirmed evidence

- Dev WEB4 deployment b1c9a99b-15d9-4f4d-8c13-830d4ac5e687 reports SUCCESS on parent 165f4a21ec9354b9bbac8e2af36ffd59bfa0472f, Core 34fb45befb37bebeeb9f24248afedd8f0b1625c9. The custom domain dev.api.web4.oasisomniverse.one belongs to that dev service. Source 165f differs from its predecessor only in solution registration; it did not introduce a new Core implementation.
- Correct /api/health returned HTTP 200 healthy in approximately 407 ms. /health is not its route and its 404 is not outage evidence.
- A read-only native Mongo ping using the configured dev connection succeeded (cold connection plus ping approximately 3,163 ms). Selected database dbStats: data 132,942,230 bytes; allocated storage 56,246,272 bytes; indexes 4,014,080 bytes; objects 15,735. This proves reachability, not total cluster quota or a successful write. No fixture or write was used.
- A bounded 200-event application-log sample covering 02:19:31–02:20:57 UTC contained zero Mongo connection, quota, MongoCommand or timeout errors; five IPFS identity errors and eleven missing-ledger-subscription errors. These counts do not exclude errors outside the window.
- PIPER Vercel request records: admin_refresh 200 at 02:11:47; admin_auth 200 at 02:14:45 with a returned session and role-load marker; admin_auth 401 at 02:24:27; admin_auth 504 at 02:32:08. The 504 nested log states a 300-second timeout and has no post-login result/session marker. This narrows that failure to awaiting authentication or the connection to WEB4, before the role lookup; it does not identify the blocked backend stage.
- Empty-model API and PIPER requests returned prompt 400 validation responses. These are not successful authentication tests. No real credentials, user identifiers, tokens, request bodies or private record contents were printed.
- Container memory was approximately 1.40 GB against an 8 GB limit; sampled CPU throttling counters were zero. No restart was performed.
- Railway httpLogs returned no records for this deployment, both with and without a time bound. Because known requests occurred, this cannot be treated as evidence that no requests reached it.

## Source findings and remaining diagnosis

AuthenticateAsync takes an instance-wide authentication semaphore without cancellation, temporarily replaces the provider manager failover list, searches username before email/public key, and uses synchronous SaveAvatar inside the async method. A stuck operation can therefore hold subsequent password logins. These are inspected risks, not a proven location of this incident's wait. Removing the gate alone would race the shared routing state; changing identifier precedence speculatively could change authentication semantics.

Audit/access-log controllers first load a holon through HyperDrive. Audit history then reads versions sequentially before pagination, and actor names are resolved sequentially. This is confirmed unbounded work, but is not yet the established cause of the login timeout.

IPFS rejects a legacy catalogue without a persisted v2 storage identity during native binding capture. Read activation is intentionally nonmutating. No fake identity, silent skip, catalogue rewrite or DNA pointer change was applied. The inspected ordinary holon-read path does not directly enroll replication; background identity errors alone do not prove reads are blocked by that worker.

Usage reconciliation reports accounts without subscriptions. No subscription grants or ledger mutations were made to suppress those errors. Protected delivery 543 remains untouched.

Next: establish privacy-safe backend stage timing and isolate whether provider activation/read, authentication queueing or session persistence is waiting. Preserve auth semantics and fix the responsible layer before claiming recovery.

## Isolation and cleanup

Read the COSMIC/CQL status: its current storage-policy persistence correction is under native qualification and is not a live publication. No changes were made in its checkout or in PIPER. Dev watch-filter updates did not change deployment IDs or restart services. No pending admin/portal release was deployed for this incident.

All incident probes and log parsing used memory and existing tools. No runtime, download, build output, database, browser profile or result archive was created locally. Retain this compact report; no additional cleanup target was introduced.

## Follow-up source and deployment observation

Serving WEB4 subsequently advanced to 02c34d35-aa41-4c60-b785-4dbcd3d20f10 on 217dcdaced1f (test-only source delta, same Core34). Revision d6984cf58b91 has one BUILDING and one QUEUED record at readback; it adds the explicit default-false sandbox preview setting to DNA templates and retains Core34. This session did not enqueue either deployment. Earlier b1 observations remain historical, not the latest runtime claim.

Added a narrow RequestTimingMiddleware candidate for login, refresh and per-holon history. Only fixed route/phase labels, a generated diagnostic operation key, status, cancellation/failure flags and elapsed milliseconds are logged. A five-second pending warning, then at most one every 30 seconds, makes waits visible even when requests never finish. Other routes bypass the timer. No bodies, URLs, query values, bearer values, user/holon IDs or exception contents are logged. Markers must be integrated into the current serving source and compiled before rollout; source alone is not live acceptance.

The isolated source-linked middleware host initially passed five compiled checks (slow live phase, status/exception preservation, private-data exclusion and unrelated-route bypass). Final correlation/empty-route cleanup is pending remote exact-source qualification. The test uses no database or substitute provider; it tests only the diagnostic middleware contract. Local outputs total 4.86 MB and their removal was rejected by automatic review; see cleanup ledger. This is an explicit exception, not a cleaned-output claim.
