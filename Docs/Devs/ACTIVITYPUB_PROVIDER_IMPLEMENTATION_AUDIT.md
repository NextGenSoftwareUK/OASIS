# ActivityPub provider implementation audit

2026-10-18. Incomplete; not release certification.

## Explicit Mastodon v2 instance contract (2026-10-18)

Activation now uses ONLY GET/api/v2/instance, not deprecated v1. Requires object with nonblank domain/title/version, parseable major>=4 (prerelease suffix permitted), configuration object; invalid/malformed/unsupported response cannot publish active state. Original HTTP status/exception retained through EnsureSuccessStatusCode. No silent old-endpoint fallback. Success wording explicitly distinguishes public server contract from authenticated authorization. This intentionally supports a Mastodon4+ REST shape, NOT every ActivityPub implementation; other account/status mappings remain unfinished. Source: [official instance API](https://docs.joinmastodon.org/methods/instance/), v2 added4.0.0, v1 deprecated.

Six genuine before-change failures15ms after correcting an initially overstrict test-handler path assertion that caused four false-green invalid-payload cases; final before report activitypub-instance-before.trx SHA2566A4CD60AF35C8364CBC1EAE5E91A97337D493C423633D173B3F2A620B224FB32. Existing lifecycle fixture now serves actual required fields at v2 path instead of arbitrary{}200. Full31/31 tests passed145ms/zero skipped including real loopback two-request reactivation; current report activitypub-unit-current.trx SHA2567547E3EB4D9FED602BB08304AD54FE67592609ABE7B061078E6B996719C8F27D. Cached bounded provider build passed2warnings.

Separate read-only public GET https://mastodon.social/api/v2/instance returned200, domain mastodon.social, version4.8.0-alpha.3+pr-41085-a68fedb, configuration present. This was PowerShell HTTP inspection, NOT provider end-to-end/authentication/federation verification. No token or remote mutation. No new install/runtime/environment. Stable provider/native ID mapping, correct public projections, safe relevant operations, OAuth scopes, pagination/errors/reconciliation, authenticated live evidence and all-provider release gates remain OPEN. Prior audit statements about v1 activation superseded by this section.

## Private record boundary correction

SaveAvatar/SaveAvatarDetail (sync+async) now reject private record storage before HTTP/activation/serialization. Removed their obsolete complete-record serializers and lossy fallback JSON. Mastodon POST/accounts creates an account with explicit terms consent and app OAuth authority; an OASIS save is not authority to register a remote account. Four genuine controlled-before cases previously claimed success on arbitrary2xx; now all verify explicit NotSupportedException, null result and ZERO HTTP calls. Full unit project25/25passes104ms; exact hashes in current release gates. This changes direct-call behavior intentionally and does not count as completed relevant provider functionality. Other holon/account/delete/query mappings remain unaudited/unfinished and must not be treated as safe publication or storage.

Core IOASISNETProvider inspection confirms only GetAvatarsNearMe/GetHolonsNearMe, no existing social publication method. Implement protocol-specific network behavior through reviewed existing call-site contracts; do not silently overload private persistence or add a hidden storage backend. Primary [Mastodon account API](https://docs.joinmastodon.org/methods/accounts/) specifies registration/consent/token response.

## Verified change

Provider deactivation retains its one HTTP client for later activation. Final disposal is idempotent, clears activation state and disposes its owned handler. Public activation flags now match ordinary sequential lifecycle calls. Five new cases genuinely failed before lifecycle edits; complete unit project21/21 passes, including two real loopback HTTP requests on one provider instance. Exact reports/hashes in HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md. This is not authenticated federation proof. Concurrent lifecycle/disposal coordination remains unverified.

## Protocol and privacy findings

- The adapter calls `/api/v1/accounts`, `/accounts/lookup`, `/accounts/search` and `/statuses`. These are Mastodon-style REST surfaces, not universal ActivityPub client-to-server endpoints. ActivityPub C2S discovers an actor's outbox and posts ActivityStreams there. Do not claim Mastodon transport tests cover Pleroma, Lemmy, Pixelfed or every federated implementation.
- Activation uses `/api/v1/instance`, now deprecated in Mastodon documentation; accepts any successful status without validating a server capability document. Need an explicit supported API contract/version and capability checks, not a silent legacy/new fallback.
- Original `SaveAvatarAsync`/detail paths serialized whole OASIS records and POSTed to account-creation endpoints; now rejected as described above. Account provisioning is NOT generic avatar-record storage; delete-by-email/username still attempts DELETE against search/lookup URLs. Remaining mappings need replacement with truthful relevant social/network operations, not just tests accepting arbitrary JSON or200 status.
- Removed Avatar/AvatarDetail serialization helpers could serialize private fields and substitute reduced JSON after serialization failure. Other conversion helpers are still unaudited. No live save/account/status mutation was attempted. A future social publication implementation must use an explicit public projection and authorization; never publish complete credential-bearing records. A metadata encryption exemption is not publication permission.
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
