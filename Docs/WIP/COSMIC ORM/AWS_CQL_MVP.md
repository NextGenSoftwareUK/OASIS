# AWS CQL MVP admission

Updated 29 October 2026. **MVP incomplete: native service and demo integration pending.**

## Implemented and verified

- DynamoDBOASIS implements the generic ICosmicQueryStorageProvider contract; AWSOASIS inherits it with its actual AWS provider authority.
- Compiler admits canonical ID equality/membership, AND/OR/constants and bounded paging, with a trusted complete record catalogue for the exact authority.
- ID algebra compiles before I/O into at most 100 native keys. One BatchGetItem selects those keys; no full-store scan or managed business predicate.
- Strongly consistent per-item reads; **not** a multi-record transaction/snapshot. Stable key order supplies the bounded page; explicit ordering is unsupported.
- Incomplete/unprocessed keys fail without retries or partial publication. Duplicate/unexpected IDs, malformed payloads/projections and unauthorized results fail.
- Current deleted records are hidden; graph expansion, metadata/content predicates, aggregates and deeper parity are not advertised.
- Canonical response budget: 4 MiB; native missing records are absent results. Existing canonical identity/projection validation is reused.

## Evidence

Focused compiler/response tests **13/13** pass: frozen authorization intersection, unsupported shapes/bounds, native response validation, deletion visibility and cancellation. AWS consumer builds successfully. These are controlled-client tests, not native DynamoDB/AWS evidence.

Evidence: `C:/Source/OASIS-cosmic/TestResults/AwsCqlCurrent/dynamo-cql.trx`.
SHA256: `539F6EAEDBBB3CB41DA45A1F9E86FF15C76A3B7C89DF504AAF96950A8D627A17`.

## Exact next steps

1. Native acceptance using the retained pinned DynamoDB Local runtime, with an isolated persistent CQL dataset inside this worktree. Existing stopped shared container points to the original checkout's historical data; do not start it or mutate that data.
2. Reuse the existing Java-capable image and the shared immutable JAR/library installation read-only; keep CQL data in LocalState/COSMIC, not TestResults. No download/second install or paid provisioning.
3. Prove a small AWS-native filtered multi-record query; then add the Mongo-paired C# preset, HTTP and browser view using existing demo-hub patterns. Keep unavailable/public discovery gates until those paths are verified.
4. Move to Ethereum after the AWS MVP. Advanced operators and deeper migration remain deferred.

## Retained runtime inventory

- Shared installation: `C:/Source/OASIS/TestResults/AWSProviderEvidence/runtime` (read-only reuse only; source/build/test output stays in OASIS-cosmic).
- DynamoDBLocal.jar: 5,662,751 bytes, SHA256 `67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620`.
- Existing container `oasis-dynamo-retained` is stopped; its data mount belongs to the original retained environment. Preserve it.
- Unit-test assets were missing after cleanup. Restored offline solely from the existing NuGet cache; no packages downloaded.
