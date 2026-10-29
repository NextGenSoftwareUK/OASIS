# AWS CQL MVP admission

Updated 29 October 2026. **Local query-demo MVP verified: native AWS wrapper, Mongo pairing, C#, HTTP and browser. Hosted AWS and deeper parity remain unverified.**

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

## Run the demo

From `C:/Source/OASIS-cosmic`:

```powershell
# Start the interactive teaching page at http://127.0.0.1:5096/.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider AWS -Federated

# Bounded native + C# + HTTP acceptance, then stop both owned services.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider AWS -Federated -VerifyOnly
```

The full preset returns **AWS 3 + Mongo 2 = 5 physical rows**; subset returns
**2 + 2 = 4**. Copies retain provider provenance. Identity membership is the MVP
filter, not arbitrary metadata/content search. Read the spaced generic source in
`Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs`; its HTTP/browser host is
`IdentityFederationApiHost.cs`. HolonManager dispatches both native providers.

Actor/readable catalogues are server-owned. HTTP accepts only named presets;
forged actor fields and unknown presets reject. An unavailable AWS source fails
the complete query instead of returning Mongo as a partial substitute.

AWS seed/data stay reusable in LocalState/COSMIC/AwsCql. The temporary exact Mongo
database is ownership-checked and dropped when the page stops. Use the page's
shutdown button or POST /demo/shutdown. The runner stops both owned services.
There is no cloud account, payment, shared snapshot or full-parity claim.

## Verified scope and next provider

- Query controls13/13 and native DynamoDB Local1/1 pass.
- Actual AWS + Mongo SDK/HTTP1/1 passes, including5/4 participation/provenance,
  input rejection, unavailable-source failure and exact Mongo cleanup.
- Browser buttons clicked: visible full5 and subset4 verified; screenshot retained
  at TestResults/AwsCqlCurrent/aws-mongo-browser.jpg (24,158 bytes).
- Hub discovery now7 paired profiles/14 plans; no listener changes on ShowPlan.
- Legacy garbled tab-title text fixed at the shared template; final HTTP test
  verifies the ASCII AWS title. Existing Oracle label/URL behavior is preserved.
- **Next: Ethereum MVP**, then Polygon, Arbitrum, Google, Cosmos blockchain,
  Polkadot and NEAR. Deep operators/migration remain deferred.

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

Final paired evidence: `TestResults/AwsCqlCurrent/aws-mongo-federation.trx`.
- Evidence `TestResults/AwsCqlCurrent/aws-cql-native.trx`, SHA256 `540CB7219E1E02E5472C3E13473B0EAE17A5B2E9C6D5DF3DF41BDAC17508F814`.
- Evidence `TestResults/AwsCqlCurrent/aws-mongo-federation.trx`, SHA256 `8399C304BBE7838D02086488F75A9EE0B0E2EA7E01270605EA724E9C814D53E3`.
- Evidence `TestResults/AwsCqlCurrent/aws-mongo-browser.jpg`, SHA256 `5850ED5A629D6557FFAEDD6D8390C595B796A6EBF255995D5309F7291F32EB6A`.
- Evidence `TestResults/ProtocolCqlCurrent/database-hub-routing.json`, SHA256 `421D897AC12202637326E967D014C7590F52EE477677DEF2E4C8E205D790E296`.
