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
