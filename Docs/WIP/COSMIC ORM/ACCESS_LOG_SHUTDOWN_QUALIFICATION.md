# Access-log shutdown qualification

19 November 2026. Implementation and isolated CI qualification accepted; Development deployment pending.

Invariant: shutdown drains already accepted events even if the .NET 10 background worker's scheduled ExecuteAsync never starts. The final drain belongs to StopAsync, after the periodic reader stops, and respects the caller cancellation token. Logging-disabled empty queues do not contact the store.

Observed regression: Development run 44616698531, job 135887189474, artifact 14576130967: 153/154 protocol cases passed. The app-opt-in immediate-shutdown native case failed in 28.5 ms with no stored event. .NET 10 BackgroundService schedules ExecuteAsync with its stopping token, so immediate cancellation can prevent the delegate (including its old final drain) from executing.

Change: move the final drain into StopAsync; remove the duplicate lifecycle responsibility from ExecuteAsync. Add deterministic never-started, disabled-empty and caller-cancellation controls. Retain previous 153 passing cases; qualify only the three new controls and the failed native parameter case using an isolated CI MongoDB replica set. No shared Atlas calls, DNA changes, PIPER data changes, or deployment in this publication.

Related accepted performance evidence: Neo Single grouped-distinct diagnostic run 44623704472, source f95c690ad1f7daa10c0620a71bab24d8057cbccc, artifact 14577909244: one native case passed; cold 2909 ms, warm 55/44 ms, native PROFILE 654 ms, five owned records cleaned up. This is diagnostic evidence, not a shipping optimization or full ORM parity credit.


## Accepted isolated qualification

Run44631225149 / job135938064558 succeeded on exact sourced90384b561cd00b2a1a2ad6060209d766d6194a9. Strict artifact14580988609 (3289bytes, SHA2564569f619541f30a1a1835fb25abaf989fe5f787a9158631c9f3dc508124b6460) contains exactly two TRX files and one source-bound JSON report. Three new lifecycle controls and only the previously failed appOptIn:true/shutdownDrain:true native case passed, zero failures/skips. Test-definition/result IDs and method names match. The original153 passing protocol cases are retained. The WEB4 unit test project built its real API dependency. The owned CI MongoDB removal step passed. Artifact inspection remained in RAM; no local archive/results or shared Atlas operations.

The next Development batch contains only the worker, its new controls and this qualification note. No DNA/pin/provider/data migration is required. Image/package checks must pass before the normal scoped Development push; live rollout is not yet accepted.
