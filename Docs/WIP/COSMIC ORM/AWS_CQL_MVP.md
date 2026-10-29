# AWS CQL MVP admission

Updated 29 October 2026. **MVP incomplete: native service proof passes; Mongo-paired SDK/HTTP/browser integration pending.**

## Implemented and verified

- DynamoDBOASIS implements the generic ICosmicQueryStorageProvider contract; AWSOASIS inherits it with its actual AWS provider authority.
- Compiler admits canonical ID equality/membership, AND/OR/constants and bounded paging, with a trusted complete record catalogue for the exact authority.
- ID algebra compiles before I/O into at most 100 native keys. One BatchGetItem selects those keys; no full-store scan or managed business predicate.
- Strongly consistent per-item reads; **not** a multi-record transaction/snapshot. Stable key order supplies the bounded page; explicit ordering is unsupported.
- Incomplete/unprocessed keys fail without retries or partial publication. Duplicate/unexpected IDs, malformed payloads/projections and unauthorized results fail.
- Current deleted records are hidden; graph expansion, metadata/content predicates, aggregates and deeper parity are not advertised.
- Canonical response budget: 4 MiB; native missing records are absent results. Existing canonical identity/projection validation is reused.

## Evidence

Focused compiler/response tests **13/13** pass: frozen authorization intersection, unsupported shapes/bounds, native response validation, deletion visibility and cancellation. AWS consumer builds successfully. These are controlled-client tests. Separate real DynamoDB Local acceptance now passes **1/1**, using the actual AWSOASIS wrapper: three retained records selected natively, two-record filtered subset, no canonical record changes. No hosted AWS claim.

Evidence: `C:/Source/OASIS-cosmic/TestResults/AwsCqlCurrent/dynamo-cql.trx`.
SHA256: `539F6EAEDBBB3CB41DA45A1F9E86FF15C76A3B7C89DF504AAF96950A8D627A17`.

## Exact next steps

1. Add the Mongo-paired C# walkthrough, HTTP preset and browser view using existing identity-federation demo patterns. Expected 3 AWS + 2 Mongo copies = 5 rows; subset 2 + 2 = 4. Keep public discovery unavailable until verified.
2. Reuse `Scripts/TestHosts/Run-CosmicAwsCqlNative.ps1` and its stopped container `oasis-cosmic-cql-dynamo`, port18002. Shared runtime remains mounted read-only; original retained container/data are untouched.
3. Seed is retained at `LocalState/COSMIC/AwsCql/demo-seed.json`; its three IDs/actor and 51,200-byte DynamoDB Local database are reusable. Do not reseed per run. Pending seeds require explicit review; the one activation-only failure was verified unwritten before continuing the same IDs.
4. Move to Ethereum after the AWS MVP. Advanced operators and deeper migration remain deferred.

## Retained runtime inventory

- Shared installation: `C:/Source/OASIS/TestResults/AWSProviderEvidence/runtime` (read-only reuse only; source/build/test output stays in OASIS-cosmic).
- DynamoDBLocal.jar: 5,662,751 bytes, SHA256 `67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620`.
- Existing container `oasis-dynamo-retained` is stopped; its data mount belongs to the original retained environment. Preserve it.
- Unit-test assets were missing after cleanup. Restored offline solely from the existing NuGet cache; no packages downloaded.

## Native proof and setup correction

Native evidence: `TestResults/AwsCqlCurrent/aws-cql-native.trx`, SHA256 `2497C95FE734A5F6BCD8CBA904AFC1402BA089E0E2D4D8D5F724D1C013D741A5`.
Initial activation failure is retained as `aws-cql-native.initial-activation-failure.trx`.
The service never launched: PowerShell split the unquoted Java -D option. Fixed the
argument vector, validated the exact stopped owned definition and replaced that
configuration only, preserving runtime/data/IDs. Full command validation and a
bounded native Dynamo protocol readiness check now precede tests. No alternate
runtime, download or silent query retry. Service confirmed stopped after success.
