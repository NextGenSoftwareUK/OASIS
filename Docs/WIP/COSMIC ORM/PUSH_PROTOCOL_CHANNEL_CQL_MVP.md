# Push Protocol public channel CQL MVP

The profile reads exact Ethereum channel addresses from the production
Get Channel route used by the official Push SDK. It validates the
returned address and exposes the reviewed public name, channel
address, chain and verification status. Channel settings, subscriber
data, message content and media stay outside the projection.

The reviewed channels are Push Protocol
`0xb88460bb2696cab9d66013a05dff29a28330689d`, ShapeShift
`0x90a48d5cf7343b08da12e067680b4c6dbfe551be` and ETH Tracker
`0xdbc5936e4daae94f415c39d284f6a69c4d553f2f`.

At Core `06a4363e1c26e616eddda34c2959acadab8dbba8`, the official
endpoint returned the exact 3/2 CQL rows. The controlled checks
rejected mismatched identity, invalid verification state and a
key outside the reviewed catalogue. All four passed with zero skips.
The 7,664-byte TRX SHA-256 is
`857babb7896c5f685e2d43a70495da5a5a34633d964fd82129d717928983cc11`.

The explicit WEB4 read profile requires the protected setting `CQL_PUSH_PUBLIC_CHANNEL_QUERY_SOURCE_OWNER`.
At Core `2324c21cfe680f465907e8dd1175b7229d7ef5d3`, all 23 admission checks
passed with zero skips. The 41,744-byte admission TRX SHA-256 is
`b51d80c1903740dddf00ad219da9a9deea541cae29630dab236f0b87e069b2b2`.
The protected publisher passed 39 settings and 21 evidence tests.
[Official SDK route](https://github.com/pushchain/push-notifications-sdk/blob/bdcdb9948c8696fc691d2cd38624d75979677f58/packages/restapi/src/lib/channels/getChannel.ts) ·
[Push Core verification documentation](https://comms.push.org/docs/notifications/push-smart-contracts/core-smart-contract/).

## Local WEB4 and Mongo federation

At Core `2324c21cfe680f465907e8dd1175b7229d7ef5d3`, WEB4 HTTP
returned native 3/2, paired 5/4, foreign-private 0 and foreign public 3.
The shared run passed three provider cases with zero skips. The
6,208-byte TRX SHA-256 is
`ce187305a3ebafd94bf5fd2fbe2734fb097153f258ebdc5231752f8c869bf6eb`.
The fixture confirmed its exact owned database was absent after cleanup.
Hosted HTTP and browser acceptance remain open.

## Scoped Development candidate

Parent 907fe855ca5e7685526b44bbc3d79da35541f052 and Core
6e053cd2807bbf068c2e1bfd75d88c8f6d766d28 are pushed to
work/cosmic-push-protocol-release-candidate in their respective repos.
The parent is based on Development d31713beb and carries only the
reviewed Push Protocol provider, WEB4 registration and Core read tariff.
The pin validator passed. The [image gate](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42321600120)
must pass before Development deployment and protected settings verification.

The image gate passed with digest
sha256:fcbb591993a19b6bbeb680edddf3ca107cf5b1e22c4d53abbce380345e354bf8.
Development fast-forwarded to the candidate. Railway queued exact code
deployment e60449de-117a-4d78-8878-d607144e2044. Four reviewed settings
are prepared in DPAPI snapshots under LocalState/COSMIC/HostedSourceRolloutCurrent
with prefix push-protocol-public. The public catalogue uses the same teaching
owner as Decentraland and derives separate IDs from Push Protocol channel keys.
Apply only after the deployment reports the reviewed commit and the full
live variables still equal the prepared baseline.

The shared verifier now supports a deployment-scoped visitor for fixed
public catalogues. After the exact settings deployment succeeds, set
COSMIC_CQL_SETTINGS_DEPLOYMENT_ID, COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT
and COSMIC_CQL_REVIEWED_RELEASE_REF. Run Verify-HostedQuerySource.py
with PushProtocolOASIS, --reviewed-exact-key-source pointing to the
retained descriptor, and --reviewed-deployment-visitor. Use --readiness
first, then --prepare to reuse the same verified deployment visitor.
All 24 response-evidence tests passed, including provider scope and
stale-deployment refusal.

Use the guarded publisher to reproduce the exact four values from the
protected snapshots. Run without --apply to verify deployment and baseline,
then with --apply to use the existing per-key acknowledgement journal.

```powershell
$env:COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT = '907fe855ca5e7685526b44bbc3d79da35541f052'
python -B Scripts/Sandbox/Publish-HostedFixedPublicCatalogue.py PushProtocolOASIS --prefix push-protocol-public --release-ref work/cosmic-push-protocol-release-candidate --deployment-id e60449de-117a-4d78-8878-d607144e2044
```

Four focused publisher tests passed. The live preflight confirmed the
release is still queued and stopped before changing settings.

Code deployment e60449de succeeded. The guarded publisher applied and
read back all four settings. The live candidate matched the protected
snapshot before the same-release redeploy. Observe settings deployment
ecdabdbb-09ed-4240-9c89-2fe437525f0d at commit 907fe855c before running
the deployment-scoped visitor checks. The redeploy intent and acknowledgement
are retained alongside the protected settings.

## Development HTTP acceptance

Settings deployment ecdabdbb-09ed-4240-9c89-2fe437525f0d succeeded.
Initial discovery saw CQL_SOURCE_NOT_READY while startup activation was
in progress. The same visitor later observed activation generation 1
with all four presets available. No recovery operation was needed.

Native returned 3/2 and MongoDB federation 5/4. The first preparation
saved two private copies. Repeat saved zero and reused both. The second
visitor saw three public channels and zero private copies on MongoDB,
SQLite and Neo4j. Both replicas held the two owned copies.

Evidence under TestResults/HostedCqlIntegrationCurrent, SHA-256:

- PushProtocolOASIS-native-http.json: 9cd918c09f32c8e075badc0467431323ead8e19dadc1ded46c40c2a0618e7cc2
- PushProtocolOASIS-client-http-flow.json: d6555fb8629fe318123652a3440ab6945568f6d137d856941dae9a0bca8eaa2e
- PushProtocolOASIS-isolation-http.json: 53eb1262fcca001b2772d9b74af03a997b4cf2f2ba0b2b440c33041de790b2c5

The sandbox card and browser interaction remain to be verified.

## Sandbox card publication

The [Push Protocol sandbox card](https://web4.oasisomniverse.one/web4-sandbox.html#cql-push-protocol-catalogue)
is live at Web4Site fac8274 with native and MongoDB instructions and a
local C# example. All 114 website tests passed. The live 228,645-byte HTML
matched the exact Git blob at SHA-256
00fa5ae7020cb0aac6a50dd3b4a0eb771908c236e187b6175424275b8d1c5c58.
Interactive browser acceptance remains open.

11 November UTC: the live Development WEB4 CQL Playground rendered three PushProtocolOASIS public teaching rows and two filtered rows from the native provider. The existing demo visitor was reused. Private paired browser verification remains open.
