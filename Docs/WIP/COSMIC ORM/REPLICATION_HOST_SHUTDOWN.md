# Replication host shutdown ownership

## Reproduced failure — 21 October 2026

The final Shared regression for the Neo4j narrow-integer batch passes942/943. `SharedInitializationIsAwaitedBeforeAnyRegistryLookup` leaks `TaskCanceledException` from the worker's additional `ExecuteTask.WaitAsync` join during `StopAsync`. The original report is retained at `TestResults/SharedCurrent/holon-host-scheduling-original-failure.trx`, SHA256 `21169ABD337DB70B8C49E34B896AD9B2DDE4536B534F06B8FB7BC4EA42673521`. Shared hosting production source was unchanged during that run.

The [.NET 10 BackgroundService implementation](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/Microsoft.Extensions.Hosting.Abstractions/src/BackgroundService.cs) schedules `ExecuteAsync` with the linked stopping token and immediately completes `StartAsync`. Cancellation before execution can therefore complete the scheduled task as cancelled without entering any worker route. Its base stop suppresses task completion exceptions; our deliberate additional join surfaces that expected pre-execution cancellation as a fault.

## Required invariant and repair

Stop must join every started route and preserve real failures and expired deadlines. A framework task cancelled before the worker ever executes owns no running route and must finish as stopped, rather than faulted.

Track actual entry into `ExecuteAsync`. Accept a cancelled execute task only when entry never occurred, the task is already cancelled and the caller's shutdown deadline has not expired. Preserve cancellation/faults after entry and any failure from the base stop. This is a narrow lifecycle distinction, not blanket cancellation suppression or a retry.

Three deterministic controls cover cancellation before execution with no receiver calls or claim completion, an expired stop deadline, and unexpected observer cancellation after entry. Against the original implementation the cohort reports one failure and two passing negative controls (`TestResults/SharedCurrent/host-pre-execution-cancellation-red.trx`). After the lifecycle repair, the complete Shared suite passes 946/946 with zero skips (`host-pre-execution-full.trx`); the producer build has zero errors.

The rebuilt native Neo4j suite also passes 66/66 with zero skips (`TestResults/Neo4jCurrent/neo4j-cql-hosting-regression-current.trx`). This includes both synchronous and asynchronous configured replication-host delivery to SQLite alongside the existing CQL and transaction-ownership regression cohort. The native container is stopped after successful teardown. These results establish the scoped shutdown repair, not completion of the wider replication roadmap.
