# Hosted CQL authoritative read policy

26 October 2026. WEB4 supports application-owned authoritative record scopes on row, scalar, projection, grouped, join and selected-result composition endpoints. This is an enrollment boundary; automatic tenant/recipient grant discovery and scalable catalogues remain roadmap work.

Register the application's `ICqlReadAccessPolicy` in dependency injection. `Authorize(authenticatedActor, authority)` must return a complete `CosmicQueryReadAccess.AuthoritativeRecords(...)` result for the exact captured actor and concrete selected provider. It must obtain IDs from trusted authorization state. Request tenant IDs, role labels, metadata indexes and supplied record IDs do not establish permission. Any storage used by the policy implementation must itself follow the host's admission/accounting rules.

```csharp
services.AddScoped<ICqlReadAccessPolicy, ApplicationCqlReadPolicy>();

// Inside the application's Authorize implementation, after authorization:
return CosmicQueryReadAccess.AuthoritativeRecords(
    authenticatedActor,
    authority,
    completeAuthorizedRecordIds);
```

The request DTO gains no actor, tenant, role, catalogue or policy override. The host captures authenticated identity and existing prepared query admission first. An enrolled policy selects an eligible provider, captures that storage instance, and authorizes once. Compilation binds this immutable policy before any source window, including both join inputs. Execution reuses the captured provider instead of resolving a mutable default again. No manager is acquired or native query dispatched on denial.

An explicit policy denial returns `403 / CQL_ACCESS_FORBIDDEN`. A null result, warning, missing returned policy, mismatched actor/provider or owner/public policy returned by an enrolled implementation is invalid and returns `503 / CQL_ACCESS_POLICY_INVALID`; an authorization exception returns `503 / CQL_ACCESS_POLICY_FAILED`. A registered DI factory returning null also fails, instead of becoming an unenrolled host. The response does not disclose private authorization details. An invalid or failed enrolled policy never falls back to owner/public visibility. Hosts without this optional service retain the existing owner/public contract; they do not claim tenant enrollment.

The catalogue limit is 128 unique nonempty IDs, with whole-set rejection and valid empty deny-all sets. Policy is evaluated afresh per request. Revocation becomes visible on the next request; an in-flight query retains its admitted immutable observation. No cross-request policy cache is introduced. Larger datasets require the still-open scalable authorization contract rather than truncation or independent paging batches.

Core/native publication and WEB4's final detached leaf DTO all use the same read rule. This matters for authorized private rows and public rows outside a tenant partition. Mandatory native predicates apply before paging, graph traversal, grouping and joins. Leaf verification remains an independent rejection guard, never a substitute for native filtering.

Current verification: shipping WEB4 builds; full Core **3,530 passed / zero skipped**, including 16 new hosted controls; WEB4 **313 passed / one existing Stripe sandbox skip**. The three new private-row controls first reproduced a remaining hardcoded owner/public leaf check, then passed after that publication owner used the generic policy. Three Core/WEB4 unit producer/consumer pairs match.

Signed HTTP tests exercise the actual shipping controller binary on MongoDB, SQLite and Neo4j. The 30 requests cover every query family, private rows before paging, shared graph scope, explicit denial, an empty next-request catalogue and anonymous rejection. They produce 27 settled operations and 24 charged query attempts; the three anonymous requests start no usage operation. Denials record affirmative no-execution evidence and zero attempt cost. Each admitted request resolves its provider and authorizes exactly once.

The complete native cohort passes **492 settlements / 541 attempts**, including the existing 138 SDK / 138 signed CQL examples, 78 REST requests, 48 GraphQL/gRPC requests and all three actual process-interruption recovery cases. Sources stayed unchanged and seven producer/consumer pairs matched. Owned services stopped and fixture/SQLite files were removed. No new environment or dependency download was required.

[Core report](Verification/hosted-read-policy-core.trx.gz), [WEB4 report](Verification/hosted-read-policy-web4.trx.gz), [native report](Verification/hosted-read-policy-native.trx.gz), [result](Verification/hosted-read-policy-result.json), [source identities](Verification/hosted-read-policy-source.json.gz), [native binaries](Verification/hosted-read-policy-binaries.json), [unit binaries](Verification/hosted-read-policy-unit-identities.json), [process originals](Verification/hosted-read-policy-process-originals.json).

The retained fixture failures were test-contract issues: a canonical projection type override, followed by an old authentication assertion assuming no previous usage operations. Before cleanup, reviewed reconciliation verified 27 terminal read operations, their 24 attempts and $0.048 test cost, all 21 committed conditional receipts from 48 original intents, the exact seven Mongo operation fences, and all 27 owned records. No original mutation was replayed. [Reconciliation report](Verification/hosted-read-policy-prefix-reconciliation.trx.gz), [original intent](Verification/hosted-read-policy-prefix-original.json.gz), [receipt/record observations](Verification/hosted-read-policy-prefix-reconciliation.json.gz), [leaf rejection regression](Verification/hosted-read-policy-leaf-red.trx.gz).

Remaining: application-specific authoritative recipient/tenant discovery, scalable catalogues with explicit authorization I/O/cancellation and billing, tenant deployment/readiness requirements, federation and operator certification. This boundary does not automatically convert metadata sharing indexes into grants or complete the full CQL/ORM roadmap.
