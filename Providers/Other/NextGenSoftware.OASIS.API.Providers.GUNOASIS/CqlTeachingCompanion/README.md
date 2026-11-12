# GUN CQL teaching companion

This Node service reads exact Holon souls from one reviewed local GUN
Radisk store. It opens the existing `radata` directory and checks its
teaching marker. The companion uses the native `gun` SDK and binds only
to `127.0.0.1`. The protected reader has access to the public teaching
read route.

Install the pinned npm lock with `npm ci --ignore-scripts` in one
dedicated Node 22+ workspace. Start the service with
`node serve-teaching.mjs <existing-teaching-directory> <port>` and set
`OASIS_GUN_CQL_READER_TOKEN` through protected process settings. The
`GET /oasis/cql/holon?id=<canonical-guid>` route returns a public live
Holon without embedded children. Private records return 403. Tombstones
and absent keys return JSON null. The response identifies GUNOASIS.

The native probe `node verify-teaching.mjs <empty-fixture-path>` starts
and stops its own process on port 54870 and verifies the persisted graph
across restarts. It can also run the native CQL integration test by
passing an already built test project and results directory after the
fixture path.

The hosted service needs a co-located CQL host and reviewed persistent
storage. General relay operations and peer replication remain separate
qualification work.

[GUN SDK](https://github.com/amark/gun).
