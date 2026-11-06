# pgvector exact-key CQL

The teaching profile uses the existing Npgsql client and
`oasis_holons` table. Activation verifies that the table's
`embedding` column belongs to the native vector extension and has the
provider's 1,536 dimensions, then reads a configured public probe.
The restricted reader executes parameterized exact UUID reads. The
shared bounded query engine handles membership, paging, tombstones,
public authority and child projection.

Native qualification uses the official PostgreSQL 17 / pgvector 0.8.7
image pinned by digest on a disposable Linux runner. It builds the
shipping schema, creates a reader with column-level access, and checks
3/2/0 rows, private denial and rejection of a matching non-vector table.
[pgvector upstream](https://github.com/pgvector/pgvector) ·
[Native qualification workflow](../../../.github/workflows/qualify-pgvector-native.yml)

The first Linux run started the pinned PostgreSQL image, then failed
before fixture creation. Npgsql could not materialize the catalogue's
`regclass` result as an untyped scalar. The fixture now checks existence
inside PostgreSQL and reads the resulting Boolean.
[First native run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42241366067/job/127972185063)

The revised test passed on PostgreSQL 17.11 with pgvector 0.8.7.
The native TRX SHA-256 is
`e221b3a094c34c55c733e13ade73046e4bed6cbc4c42fdc2c432cfb17f9b3e4c`.
[Passing native run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42241735749/job/127973473203)

WEB4 enrollment, federation, browser acceptance and vector similarity
queries remain open. LocalFile remains first for live rollout.

The hosted preparation adds native grant checks. Activation verifies
SELECT on the exact ID and document columns, and rejects write or
schema-creation privileges. The next isolated run checks the restricted
reader, an overprivileged owner and a non-vector table containing the
same public record. [PostgreSQL privilege functions](https://www.postgresql.org/docs/17/functions-info.html)
