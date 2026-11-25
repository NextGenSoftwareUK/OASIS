# BlueSkyOASIS

OASIS provider for the AT Protocol (BlueSky).

## Storage

`new BlueSkyOASIS(pdsUrl, identifier, appPassword, encryptionKey32)` stores avatars, avatar details and holons as
records of the `one.oasis.record` collection in the account's PDS repository (`com.atproto.repo.putRecord`,
`getRecord`, `deleteRecord`, cursor-paginated `listRecords`). Repository records are public, so every key and value
is compressed and encrypted (`PrivateKeyValueBackend`) before it reaches the PDS; each record also carries its
original key, verified on every read. Record keys that are not valid AT Protocol rkeys are mapped to `~` + SHA-256.
putRecord bodies above 1,000,000 bytes (the PDS limit) are rejected explicitly.

Sessions use `com.atproto.server.createSession`; `ExpiredToken` triggers `refreshSession` (and a new session when
the refresh token has expired). HTTP 429 waits for `RateLimit-Reset`, at most 3 times and 90 s per wait.

## Publishing

`PublishPostAsync(text)` / `DeletePostAsync(atUri)` create and delete `app.bsky.feed.post` records.
`new BlueSkyOASIS(identifier, appPassword)` (or `BLUESKY_IDENTIFIER` / `BLUESKY_APP_PASSWORD`) is a publishing-only
instance whose storage operations fail explicitly.

## Tests

The shared core storage suite runs against a real, unfederated self-hosted PDS:
`TestProjects/NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS.IntegrationTests/start-pds-fixture.sh <password>`
starts Postgres + a local PLC directory + the official PDS (all pinned by digest) and creates the
`oasis-async.test` / `oasis-sync.test` accounts; then run the project with `-p:NativeStorageConformance=true`.
