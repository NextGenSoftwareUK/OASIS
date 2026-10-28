> Superseded by [current status](../CURRENT_STATUS.md); retained historical and technical reference.

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

## Required deployment mode

Set `CQL_REQUIRE_AUTHORITATIVE_READ_POLICY=true` when the deployment must use authoritative scopes. Production Startup captures this flag and registers readiness before storage initialization hosts. Malformed values fail service registration. A required host cannot start with a missing policy or a factory returning null. Readiness resolves policy composition in a disposable scope, supporting scoped implementations without calling `Authorize` or selecting/activating storage.

Custom hosts should also call:

```csharp
services.AddConfiguredCqlReadAccessPolicy(configuration);
services.AddScoped<ICqlReadAccessPolicy, ApplicationCqlReadPolicy>();
```

The shared request boundary independently enforces the immutable requirement: omitting startup readiness cannot turn a required deployment into owner/public access. Missing registration returns `503 / CQL_ACCESS_POLICY_REQUIRED` before provider lookup or manager acquisition. Configuration changes do not alter the captured requirement in a running host. An absent/false flag preserves compatibility for unenrolled hosts; it does not disable an already registered policy. Query DTOs cannot override this deployment setting.

The catalogue limit is 128 unique nonempty IDs, with whole-set rejection and valid empty deny-all sets. Policy is evaluated afresh per request. Revocation becomes visible on the next request; an in-flight query retains its admitted immutable observation. No cross-request policy cache is introduced. Larger datasets require the still-open scalable authorization contract rather than truncation or independent paging batches.

Core/native publication and WEB4's final detached leaf DTO all use the same read rule. This matters for authorized private rows and public rows outside a tenant partition. Mandatory native predicates apply before paging, graph traversal, grouping and joins. Leaf verification remains an independent rejection guard, never a substitute for native filtering.

Initial hosted-enrollment checkpoint: shipping WEB4 builds; full Core **3,530 passed / zero skipped**, including 16 new hosted controls; WEB4 **313 passed / one existing Stripe sandbox skip**. The three new private-row controls first reproduced a remaining hardcoded owner/public leaf check, then passed after that publication owner used the generic policy. Three Core/WEB4 unit producer/consumer pairs match.

Signed HTTP tests exercise the actual shipping controller binary on MongoDB, SQLite and Neo4j. The 30 requests cover every query family, private rows before paging, shared graph scope, explicit denial, an empty next-request catalogue and anonymous rejection. They produce 27 settled operations and 24 charged query attempts; the three anonymous requests start no usage operation. Denials record affirmative no-execution evidence and zero attempt cost. Each admitted request resolves its provider and authorizes exactly once.

The complete native cohort passes **492 settlements / 541 attempts**, including the existing 138 SDK / 138 signed CQL examples, 78 REST requests, 48 GraphQL/gRPC requests and all three actual process-interruption recovery cases. Sources stayed unchanged and seven producer/consumer pairs matched. Owned services stopped and fixture/SQLite files were removed. No new environment or dependency download was required.

[Core report](../Evidence/Verification/hosted-read-policy-core.trx.gz), [WEB4 report](../Evidence/Verification/hosted-read-policy-web4.trx.gz), [native report](../Evidence/Verification/hosted-read-policy-native.trx.gz), [result](../Evidence/Verification/hosted-read-policy-result.json), [source identities](../Evidence/Verification/hosted-read-policy-source.json.gz), [native binaries](../Evidence/Verification/hosted-read-policy-binaries.json), [unit binaries](../Evidence/Verification/hosted-read-policy-unit-identities.json), [process originals](../Evidence/Verification/hosted-read-policy-process-originals.json).

The retained fixture failures were test-contract issues: a canonical projection type override, followed by an old authentication assertion assuming no previous usage operations. Before cleanup, reviewed reconciliation verified 27 terminal read operations, their 24 attempts and $0.048 test cost, all 21 committed conditional receipts from 48 original intents, the exact seven Mongo operation fences, and all 27 owned records. No original mutation was replayed. [Reconciliation report](../Evidence/Verification/hosted-read-policy-prefix-reconciliation.trx.gz), [original intent](../Evidence/Verification/hosted-read-policy-prefix-original.json.gz), [receipt/record observations](../Evidence/Verification/hosted-read-policy-prefix-reconciliation.json.gz), [leaf rejection regression](../Evidence/Verification/hosted-read-policy-leaf-red.trx.gz).

Required-mode verification: **3,546 Core tests passed**, including 16 new configuration/readiness/runtime-guard controls. Shipping WEB4 has **316 passed / one existing Stripe skip**, including actual host startup rejection before later initialization and scoped-policy disposal without authorization. The signed three-provider cohort uses the production configuration extension with required mode enabled and still passes **492 settlements / 541 attempts**. Sources stayed unchanged; seven native and three unit producer/consumer pairs match. Fixtures and services were cleaned.

[Required-mode Core report](../Evidence/Verification/read-policy-readiness-core.trx.gz), [shipping host report](../Evidence/Verification/read-policy-readiness-web4.trx.gz), [native report](../Evidence/Verification/read-policy-readiness-native.trx.gz), [result](../Evidence/Verification/read-policy-readiness-result.json), [source identities](../Evidence/Verification/read-policy-readiness-source.json.gz), [native binaries](../Evidence/Verification/read-policy-readiness-binaries.json), [unit binaries](../Evidence/Verification/read-policy-readiness-unit-identities.json).

Remaining: application-specific authoritative recipient/tenant discovery, scalable catalogues with explicit authorization I/O/cancellation and billing, federation and operator certification. This boundary does not automatically convert metadata sharing indexes into grants or complete the full CQL/ORM roadmap.
