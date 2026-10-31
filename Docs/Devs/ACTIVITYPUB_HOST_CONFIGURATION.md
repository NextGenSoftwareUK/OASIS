# Registering an ActivityPub CQL source

The Mastodon REST adapter uses an **instance origin**, not an API URL. Configure
`OASIS.StorageProviders.ActivityPubOASIS` in the node DNA:

```json
{
  "BaseUrl": "https://mastodon.social",
  "EnableCqlStatusKeySets": true
}
```

`EnableCqlStatusKeySets` explicitly enables the instance's native
`GET /api/v1/statuses?id[]=...` collection query. Its default is false; the point
profile remains available. This is not arbitrary timeline/content search.

The BootLoader constructs the provider through `ActivityPubProviderConfiguration`.
Construction does no network I/O. Explicit activation verifies the instance's v2
contract. Registration must not change the default storage provider or add this
social source to private-record replication/failover routes.

For an authenticated application, optionally set `AccessTokenEnvironmentVariable`
to the **name** of a server-owned environment variable. Missing configured tokens
are errors; they never fall back to anonymous access. Do not put a browser's WEB4
JWT into this variable or forward it to Mastodon. Public read demos can omit it.
Connection overrides must retain the configured origin. Credentials, path, query
and fragment are forbidden in the origin; HTTP is allowed only for local loopback.

Older DNA defaults ending in `/api/v1` must be corrected explicitly to the instance
origin. The factory deliberately does not guess or strip arbitrary URL paths.

Provider registration alone does **not** make a source available in the public
sandbox. Hosted query admission, read-access policy, source availability, query
presets, frontend discovery and deployed acceptance must also be configured and
verified. Do not grant private holon writes merely to enrol a public read source.

Native CQL publishes only verified public/unlisted statuses and enforces the
adapter's documented query capabilities and budgets. No account, posting rights,
full ActivityPub protocol coverage or universal ORM storage is implied.
