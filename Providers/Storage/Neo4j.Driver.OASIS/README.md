# OASIS Neo4j driver connection-creation patch

Pinned upstream: Neo4j driver 4.3.2, commit
`5a40b591107331131875fbcd3d7f28fe60504d95`. See UPSTREAM.json for original file
hashes and the archive identity. LICENSE and NOTICE are retained.

The signed cached driver can close an established connection but misses a
connection negotiating its initial handshake. This source build registers that
transport before initialization and includes it in pool shutdown. Creation keeps
ownership of pool-size accounting. Socket close also marks an unopened client
closed so initialization cannot silently reopen that owner.

The public assembly identity remains compatible with existing signed driver
consumers. The supplied **public** identity key is used for public signing; no
private signing key is included. Informational/file versions identify this as
an OASIS patch, not an official upstream release. No new NuGet dependency is
required on net10.0. The assembly is built from source, never patched in place.

The ten cold/warm native relay controls pass against the real server, including
caller-only cancellation before held response release. The consumer driver hash
is verified by the reusable runner. All four CQL query shapes are covered;
the full 259 native CQL and 465 provider unit tests pass without skips. Further
acceptance requires other connection phases, deadlines and non-CQL regressions.
