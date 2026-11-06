# Nile CQL MVP

The dedicated read profile uses the existing Npgsql query engine and the
shared `public.oasis_holons` table. It admits bounded authorized identity
reads and requires an existing live public probe. The ordinary Nile storage
adapter continues to use its existing PostgreSQL storage contract.

The read profile requires one official Nile host on port 5432 and
`VerifyFull` TLS. It takes the database and reader from reviewed settings
and uses the public schema. PostgreSQL TLS negotiation against
`db.thenile.dev` and `us-west-2.db.thenile.dev` returned TLS 1.3 with
hostname and certificate-chain verification. No credentials were sent.
[Nile connection addresses and credentials](https://www.thenile.dev/blog/announcing-nile-apis).

Seven Nile controls and 18 PostgreSQL family regression checks passed
without skips at Core `2abc7100acbdf2e4e6d8249c24f7cf5746daba34`.
The TRX SHA-256 is
`93f0e3e5cc35422b14800ed0038df8882794b63891bf7835d566cbb88e1e9670`.
A reviewed Nile database, a dedicated reader with verified table access
and no write privileges, native CQL results, and live visitor verification
are open.

The WEB4 candidate uses `CQL_NILE_QUERY_HOST`, `CQL_NILE_QUERY_DATABASE`,
`CQL_NILE_QUERY_READER_USERNAME`, `CQL_NILE_QUERY_READER_PASSWORD` and
`CQL_NILE_QUERY_PROBE_ID`. The username accepts Nile's issued credential
identifier. Keep both reader settings private. The settings reviewer binds
the approved host, database and first public UUID. All 19 settings checks
passed, including rejection of a mismatched database. Nine Nile WEB4
admission and 25 PostgreSQL-family regression tests passed without skips at
Core `9e38eba55954ad9a3e4d6b1cc85c428d76a3ff2a`. The admission TRX
SHA-256 is
`d8b6925c1990f3db450ac65ab2dc4bdbfd182de6b413b464021fc5edf4a0f751`.

The native verifier draft is
`LocalState/COSMIC/HostedNileCqlQualification/verify-native.ps1`.
It takes the reviewed descriptor and protected reader settings. The
permission check uses read-only SQL to verify table SELECT and reject
table write or public-schema create grants. It then checks the exact
three live records, deleted record and missing key, exercises CQL 3/2/0,
and compares native row hashes before and after. The missing-credentials
preflight passed. The first probe build found a mismatched stream method
parameter name. The corrected probe compiled at the exact WEB4 Core pin.
Live execution requires the approved Nile teaching database and reader.
