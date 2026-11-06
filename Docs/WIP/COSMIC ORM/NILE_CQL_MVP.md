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
and no write privileges, native CQL results, and WEB4 admission are open.
