# Convex CQL MVP

The dedicated Convex companion reads one teaching Holon by its indexed
canonical OASIS ID. The protected reader token authorizes the HTTP action.
The action returns only explicitly public live payloads. An invalid token
returns 401, a private record returns 403 and a missing or deleted record
returns JSON `null`. The CQL adapter checks payload identity and its
authoritative read grant before paging. It sends no scan or mutation.

[Companion source and reproducible Windows probe](../../../Providers/Cloud/NextGenSoftware.OASIS.API.Providers.ConvexOASIS/CqlTeachingCompanion/README.md)

The pinned native Windows backend and Convex CLI 1.49.0 passed the companion
probe on a fresh local database. The probe covered an existing public
record, missing and invalid tokens, a private record, a tombstone, an
absent key, invalid IDs and extra query parameters. The separate .NET
run passed all 20 adapter and Function API checks at Core
`687295bdc99b85382aa5256e97ccdc0996051664` with zero skips. Its TRX
SHA-256 is `8bcec4314bed0ee03f48cdaf83ec011c97f2c1299b9ac7c746e49bbdcd673a22`.

WEB4 uses explicit `CQL_CONVEX_QUERY_SITE`, `CQL_CONVEX_QUERY_PROBE_ID`
and protected `CQL_CONVEX_QUERY_READER_TOKEN` settings. The protected
settings review binds the teaching site and probe. All 27 protected-settings
tests passed. Nine WEB4 admission checks and nine Appwrite regressions
passed with zero skips at Core `9f2f07150cb39f81a75fac0837f38d31d510e724`.
The admission TRX SHA-256 is
`5e912ae8ea69928d80009f9c2af94c8cef9703dcbc6b626d027981ae4ed04b62`.

Hosted acceptance needs a reviewed dedicated teaching deployment with
three public rows, a tombstone, an absent key and a private control.
The `ConvexHostedCqlNative` integration test compiled at the same Core
pin. Its build log SHA-256 is
`c918f63ae69705b23d4e0d54c23f894e3571854470b0fd2fee7b455191681caa`.
Use `COSMIC_CONVEX_HOSTED_CQL_SOURCE` for a reviewed descriptor with
`Site`, `Actor`, `LiveIds`, `DeletedId`, and `MissingId`. Use
`COSMIC_CONVEX_HOSTED_PRIVATE_ID` for the private control and the
protected reader setting for authorization. The test exercises CQL
3/2/0 and compares public response bytes across both passes.

General holon and avatar companion functions remain open. The LocalFile
deployment remains first for the live WEB4 rollout.
