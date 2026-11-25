# PLANOASIS and LoomOASIS: storage classification (2026-11-25)

Session: cloud-hyperdrive-A. Both providers were reviewed for the native core-storage suite.

## LoomOASIS: non-storage

- The provider targets the Loom video-messaging REST API (`https://api.loom.com/v1/`): workspace users map to avatars and
  videos map to holons.
- Loom stores recorded videos with metadata. It has no keyed, replaceable record store, so OASIS records cannot be saved,
  overwritten, hard-deleted or looked up by key within the platform's intended use. Encoding records as videos would be
  abuse of the service, not storage.
- Classification: non-storage (social/media capability provider), like FarcasterOASIS. It needs the capability-v1 review,
  not the storage suite. Its `ProviderCategory` should be reviewed in the provider-classification work.

## PLANOASIS: unknown backend, decision needed

- Every storage method calls `https://api.plan.network/avatars/{id}`, `/holons/...` and similar paths. The README is the
  generic OASIS provider template and does not say what PLAN is.
- No public documentation for that API was found from this session, and `api.plan.network` could not be resolved here
  (this sandbox's network policy would also block an unknown host, so that alone is not proof the service is gone).
- No local or self-hostable PLAN backend exists to test against, and the provider's request shapes cannot be verified.
- Decision needed from David (see ACTIONS-FOR-DAVID): what PLAN is (a link to its API docs or a test account), or
  whether to retire PLANOASIS.
