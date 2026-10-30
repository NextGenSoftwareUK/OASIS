# Supabase CQL MVP — audit, not yet verified

Updated 29 October 2026. Supabase remains red in the provider inventory.

## Current source and root causes

Provider: `Providers/Cloud/NextGenSoftware.OASIS.API.Providers.SupabaseOASIS/SupabaseOASIS.cs`.
It uses PostgREST directly with API-key/Bearer headers; canonical holons are stored
as plaintext `data_json` beside UUID primary key `id`, `parent_holon_id`, integer
`holon_type` and boolean `is_deleted`. No Supabase SDK is required.

- No CQL adapter/capability declaration exists.
- Activation accepts404 and does not publish activated state; a missing table can
  appear healthy. Deactivation does not clear the state/auth lifecycle.
- Holon save assigns generated caller ID/key before dispatch and does not publish
  IsSaved/SavedCount. It checks status but does not verify returned row identity or
  canonical payload. Unknown dispatched outcomes have no reconciliation contract.
- Common holon reads deserialize without first enforcing HTTP success, projection
  identity, row uniqueness or complete-result count. Backend row limits can silently
  truncate a response. These invariants must be fixed at their owning boundaries.
- Existing child/depth/history/concurrency parameters are not thereby certified.
  Avatar/detail ownership and general bulk atomicity remain separate open work.

## Exact next step

Inspect reusable native resources first. No Supabase or PostgREST executable was
found on Ubuntu PATH or under `/var/tmp/oasis-runtimes` on this audit. No runtime,
service, database, identity or hosted account has been created for this provider.

Implement one canonical acknowledged-save/read path with injectable HTTP transport
and focused controls; add explicit bounded identity query compilation/execution.
Require native primary-key filter, deleted exclusion, exact complete count/range,
stable ordering, limits and cancellation; reject unsupported business predicates.
Then admit a reusable free local native REST/PostgreSQL service with persistent
identity/schema and write journals, documenting the difference from hosted Supabase.
Verify original records across restart before adding Mongo-paired C#/HTTP/browser.
Do not reuse PostgreSQL-provider proof as evidence for the Supabase REST adapter.
