# Clan mutations and the authenticated writer

This describes the Development implementation. A source merge, CI qualification and a running hosted deployment are distinct states.

Clan ownership and the person making a change are separate identities. The REST host takes the acting avatar from its validated request avatar. gRPC and GraphQL retain their existing Wizard admission and use that same validated avatar; MCP uses McpCallContext.Caller.AvatarId. A requested owner/member identity never becomes the audit writer. No extra authentication/database lookup is added.

| REST operation | Behavior |
|---|---|
| POST /api/clan | JSON name/description; authenticated avatar becomes owner and first member. |
| PUT /api/clan/{clanId} | JSON name/description; owner or Wizard. |
| DELETE /api/clan/{clanId} | Owner or Wizard; current soft-delete behavior retained. |
| POST /api/clan/{clanId}/members/{avatarId} | Owner or Wizard adds a member. |
| DELETE /api/clan/{clanId}/members/{avatarId} | Owner/Wizard removes a member, or a member leaves themselves. The owner cannot be removed. |

Existing list, by-name, detail and member-read routes are unchanged. This change adds no HTTP/protobuf/GraphQL/MCP argument and changes no DNA setting. API clients continue to send the normal bearer and stable operation headers; they do not supply an acting-avatar override.

Trusted direct Core callers must supply a nonempty actingAvatarId on clan create/update/delete, membership and legacy treasury mutations. Existing optional parameter positions remain; omission produces OASISResult error CLAN_ACTOR_REQUIRED. Creation also refuses an empty owner with CLAN_OWNER_REQUIRED. Pass the authenticated actor, preserving the clan OwnerAvatarId as business data. The existing atomic inventory route with operation/destination identities is retained; this actor fix does not certify legacy treasury transfer atomicity.

Verification: native46156859773uses two real isolated avatars and a real clan to prove ordinary self-leave, owner preservation, persisted ModifiedByAvatarId equal to the actual member and popup close. Actual MCP project compiler46156654176passes against paired source. Owner-removal46153889539proof is retained. These are representative scopes, not all-transport/native-provider/hosted acceptance. Historical records and protected deliveries535/543 are never rewritten or replayed by this fix.

See [current status](../../../WIP/HyperDrive-Providers/CURRENT_STATUS.md) and [clan evidence](../../../WIP/HyperDrive-Providers/OPORTAL_CLAN_MEMBERSHIP_2026-11-28.md).
