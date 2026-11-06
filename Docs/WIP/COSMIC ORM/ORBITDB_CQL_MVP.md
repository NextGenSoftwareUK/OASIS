# OrbitDB CQL MVP

The native qualification used OrbitDB 4.0.0, Helia 6.0.22 and Node
24.22.1 on Windows. One indexed teaching database retained its rows
across independent process starts. The companion verified read token
authorization, public-only projection, tombstones, absent keys and
unchanged native rows after restart.

Eleven provider controls and one real sidecar CQL integration test passed
at Core `472018f5c5df368b14ce4541045cbb6c0b95c790`, with zero skips.
The test returned 3/2/0, denied the private row and excluded embedded
children. The controlled and native TRX SHA-256 values are
`926bbe2cd47fdfa8385cd6462fc3593f85b2fe0c1e02fd48486b702873708d2a`
and `bb787252b08e2367dc53c6dda91bd9985ca61c0c145587e159c7e48920aa5f4a`.
[Companion and verification](../../../Providers/Other/NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS/CqlTeachingCompanion/README.md).

WEB4 admission, placement of the co-located reader and live federation
remain open. General OrbitDB document storage and multi-peer convergence
are separate scope. LocalFile remains first for the live rollout.
