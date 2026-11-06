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

The shared loopback reader now checks the companion's provider identity.
All 11 OrbitDB controls and the native CQL case passed again at the
same Core pin. The provider-bound native TRX SHA-256 is
`effa968cbed534d2a31ddfc553dded83f69f39ab0e811a9bd0d8a0d2816c0ae3`.
The same native companion passed in the pinned WEB4 Linux Node 24.19.0
image. [Linux qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42236158067/job/127954184713).

The packaged companion passed Linux native process identity and restart
checks. The image-stage job verified both the default and opt-in packages.
Two Windows seed attempts during the WEB4 build timed out before reaching
the revised reader. [Native job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42238165351/job/127961168369) ·
[Package job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42238165351/job/127961168833).

Opt-in WEB4 admission passed nine combined OrbitDB and GUN checks and
nine ENS regressions at Core `10e464dcf58f4624a27e19f12738978ecf0fcb07`.
Placement of the co-located reader and live federation remain open.
General OrbitDB document storage and multi-peer convergence
are separate scope. LocalFile remains first for the live rollout.

## WEB4 runtime settings

Build the reviewed WEB4 image with `OASIS_INCLUDE_TEACHING_SIDECARS=true`
when either sidecar is needed. Set the runtime flag only after the exact
image and its persistent mount have been verified.

| Setting | OrbitDB | GUN |
| --- | --- | --- |
| Runtime flag | `OASIS_SANDBOX_ORBITDB=true` | `OASIS_SANDBOX_GUN=true` |
| Existing volume directory | `/data/COSMIC/SandboxOrbitDB` | `/data/COSMIC/SandboxGUN` |
| Native identity | `OASIS_SANDBOX_ORBITDB_ADDRESS` from the reviewed manifest | Existing Radisk teaching marker |
| Reviewed loopback origin | `CQL_ORBITDB_QUERY_SIDECAR` | `CQL_GUN_QUERY_SIDECAR` |
| Protected reader | `CQL_ORBITDB_QUERY_READER_TOKEN` | `CQL_GUN_QUERY_READER_TOKEN` |
| Existing public probe | `CQL_ORBITDB_QUERY_PROBE_ID` | `CQL_GUN_QUERY_PROBE_ID` |

Place the reviewed native fixture on the attached volume with its owner
process stopped. Keep the database directory and the accepted cohort on
subsequent deploys. The supervisor checks the startup process identity
and public probe before starting WEB4. After settings and mounted
records are verified, use the existing exact-key settings and acceptance
scripts for native 3/2, Mongo 5/4, repeat reuse, visitor isolation, matching
SQLite and Neo4j replicas, and browser verification.
[Acceptance roadmap](SANDBOX_PROVIDER_DEMO_ROADMAP.md)

## Local WEB4 and MongoDB federation

On 6 November 2026 the retained OrbitDB teaching fixture passed local
WEB4 HTTP and MongoDB 8.2 at parent `369e93679` and Core
`c2a8b2303264bd159ac55d71b684c712647c6d3b`. The exact native queries
returned 3/2 and federation returned 5/4. The foreign actor saw zero
private rows and three public rows. Both SDK-acknowledged private
copies were removed after ownership and count checks. Both owned
services stopped and the original fixture descriptor was byte-identical.
TRX SHA-256
`433eb5ed60d19a5cadcf619a5867e5fed0dc7d9289ee057e894b8f52eeb03774`.
