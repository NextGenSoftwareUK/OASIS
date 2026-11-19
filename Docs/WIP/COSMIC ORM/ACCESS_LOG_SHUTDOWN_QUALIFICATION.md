# Access-log shutdown qualification

19 November 2026. Implementation prepared; isolated CI qualification pending.

Invariant: shutdown drains already accepted events even if the .NET 10 background worker's scheduled ExecuteAsync never starts. The final drain belongs to StopAsync, after the periodic reader stops, and respects the caller cancellation token. Logging-disabled empty queues do not contact the store.

Observed regression: Development run 44616698531, job 135887189474, artifact 14576130967: 153/154 protocol cases passed. The app-opt-in immediate-shutdown native case failed in 28.5 ms with no stored event. .NET 10 BackgroundService schedules ExecuteAsync with its stopping token, so immediate cancellation can prevent the delegate (including its old final drain) from executing.

Change: move the final drain into StopAsync; remove the duplicate lifecycle responsibility from ExecuteAsync. Add deterministic never-started, disabled-empty and caller-cancellation controls. Retain previous 153 passing cases; qualify only the three new controls and the failed native parameter case using an isolated CI MongoDB replica set. No shared Atlas calls, DNA changes, PIPER data changes, or deployment in this publication.

Related accepted performance evidence: Neo Single grouped-distinct diagnostic run 44623704472, source f95c690ad1f7daa10c0620a71bab24d8057cbccc, artifact 14577909244: one native case passed; cold 2909 ms, warm 55/44 ms, native PROFILE 654 ms, five owned records cleaned up. This is diagnostic evidence, not a shipping optimization or full ORM parity credit.
