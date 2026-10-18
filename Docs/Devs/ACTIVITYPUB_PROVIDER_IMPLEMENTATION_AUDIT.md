# ActivityPub provider implementation audit

2026-10-18. Incomplete; not release certification.

## Verified change

Provider deactivation retains its one HTTP client for later activation. Final disposal is idempotent, clears activation state and disposes its owned handler. Public activation flags now match ordinary sequential lifecycle calls. Five new cases genuinely failed before lifecycle edits; complete unit project21/21 passes, including two real loopback HTTP requests on one provider instance. Exact reports/hashes in HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md. This is not authenticated federation proof. Concurrent lifecycle/disposal coordination remains unverified.

## Protocol and privacy findings

- The adapter calls `/api/v1/accounts`, `/accounts/lookup`, `/accounts/search` and `/statuses`. These are Mastodon-style REST surfaces, not universal ActivityPub client-to-server endpoints. ActivityPub C2S discovers an actor's outbox and posts ActivityStreams there. Do not claim Mastodon transport tests cover Pleroma, Lemmy, Pixelfed or every federated implementation.
- Activation uses `/api/v1/instance`, now deprecated in Mastodon documentation; accepts any successful status without validating a server capability document. Need an explicit supported API contract/version and capability checks, not a silent legacy/new fallback.
- Current `SaveAvatarAsync`/detail paths serialize whole OASIS records and POST to account-creation endpoints. Account provisioning is NOT generic avatar-record storage; delete-by-email/username attempts DELETE against search/lookup URLs. These mappings need replacement with truthful relevant social/network operations, not just tests accepting arbitrary JSON or200 status.
- Conversion helpers can serialize private Avatar/AvatarDetail fields and substitute reduced JSON after serialization failure. No live save/account/status mutation was attempted. A future social publication implementation must use an explicit public projection and authorization; never publish complete credential-bearing records. A metadata encryption exemption is not publication permission.
- OASIS GUIDs are not automatically Mastodon account/status IDs. Stable provider ID mappings, account ownership, OAuth scopes, pagination, errors, mutation acknowledgements and original-outcome reconciliation need explicit implementation and real test-account proof. Current generic deserialization is not native account/status mapping.
- Existing bootloader exclusion from generic failover/replication storage selection remains. This does not complete the provider: relevant social/network interfaces still require real API operations and authenticated federation tests. Do not emulate unavailable record storage in posts or add a hidden backend.
- SOLID source inspection began, but no new SOLID implementation/test claim is made here.

## Next implementation gates

1. Inventory IOASISNETProvider and existing social call sites; define truthful protocol-specific operations and mappings while preserving the full requested provider scope.
2. Replace unsafe account/storage mappings and silent conversion fallbacks with specified protocol behavior and explicit result errors. Define private/public boundary before sending any records.
3. Reuse existing HttpClient transport with correct response lifetime, bounded deadlines, documented OAuth/authentication and capability checks; serialize lifecycle ownership where needed.
4. Controlled HTTP contract tests first, then existing or approved reusable authenticated federation test environment. No personal/live account provisioning, deletion or posting merely to test a guessed endpoint.
5. Verify supported operations end-to-end and document unsupported protocol capabilities truthfully; do not label protocol-inapplicable generic storage as implemented. Hosted API/release checks remain separate gates.

## Primary references

- [W3C ActivityPub](https://www.w3.org/TR/activitypub/): actor/outbox discovery, client-to-server protocol, addressing and delivery.
- [Mastodon instance methods](https://docs.joinmastodon.org/methods/instance/): versioned server-information endpoints; v1 deprecation.
- [Mastodon API methods](https://docs.joinmastodon.org/methods/): account/status REST operations and required authentication.

Removed the unsupported full-interface completion claim from package release notes. Other broad package descriptions are not certification. Removing NotImplementedException text alone is not functional proof.
