# Holon permissions

*Added 29 October 2026, extended the same day with fields, groups, inheritance and avatar sharing.*

Users own their data. Every holon has an owner, and the owner decides, holon by holon and **field by field**, who can see
it, change it, delete it, build under it, or change those decisions. Access is closed by default: with nothing granted
only the owner can use a holon.

## The five permissions

| Permission | JSON name | Lets the grantee |
|---|---|---|
| Read | `read` | See the holon in loads, searches and queries |
| Write | `write` | Change the holon |
| Delete | `delete` | Delete the holon (on a field: remove that metadata key) |
| Add children | `addChildren` | Create holons underneath this one |
| Manage | `manage` | Change who has which permission on this holon |

They are separate. Read plus write does not include delete, and nothing implies manage, so someone can be allowed to edit
a record without being able to change who else may see it. On a single field only `read`, `write` and `delete` apply.

## Who a grant can name (the grantee)

* An **avatar id**.
* A **group id** (a group or role, see below): everyone in the group.
* The word **`public`**: anyone. Honoured when the request asks for public results.

## Fixed rules (cannot be removed by a grant)

1. The **creator has every permission** on a holon they created.
2. An avatar can **read a holon it is attached to** (the holon's parent is that avatar), for example a role record an
   administrator created for them. Read only.
3. A holon marked **public** is readable by everyone when public results are requested.
4. A **Wizard** may change permissions and group membership on any holon (repair and migration).

The older read-only recipient list (`SHARED_AVATAR_IDS`, written by `api/share/share-holon`) still works and grants read.

## What can be granted

Stored on the holon, in its metadata under `OASIS_PERMISSIONS`:

```json
{
  "grants":  { "<avatar|group|public>": ["read", "write"] },
  "inherit": { "<avatar|group|public>": ["read"] },
  "fields":  { "<avatar|group|public>": { "metadata.username": ["read"], "name": ["read", "write"] } },
  "inheritFields": { "<avatar|group|public>": { "metadata.note": ["read"] } }
}
```

* **grants**: permissions on this whole holon.
* **inherit**: permissions on this holon **and every holon below it** (through `ParentHolonId`, up to 16 levels). Put
  it on a folder-like holon to cover everything inside, including holons created later. An inherited grant never makes
  anyone the owner: it gives exactly the permissions listed.
* **fields**: permissions on single fields only. Field names are `name`, `description`, any property of the holon type
  (for example `username`), `metadata.<key>`, `metadata.*` (every metadata key) or `*`.
* **inheritFields**: the same field permissions, also applying to every holon below this one. Use it to share one kind of
  field across a whole tree (for example `metadata.note` on everything inside a folder) without giving whole-holon access.

A flat `{"<grantee>":["read"]}` value, written by the first version, is read as `grants`. A value that cannot be parsed
grants nothing, and changes to that holon's access fail until it is repaired.

### Sharing only some fields

To share only a username on a profile, nothing else:

```json
{ "fields": { "public": { "metadata.username": ["read"] } } }
```

A reader with only field grants gets a **projection**: the holon with its id, type and parent (so it can be identified
and placed) and only the fields they may read. Everything else, including the permissions themselves, is left out. A
reader with whole-holon read gets the holon unchanged.

Writes are checked the same way. Someone with `write` on `metadata.username` can change that key and nothing else; a save
that touches any other field is refused. Removing a metadata key needs `delete` on that key.

### Who may change access

Permissions, group membership and the "shared with" flags are stored as metadata, but **writing a holon never lets you
change them**. Only `manage` does (a save that alters them without `manage` is refused with `COSMIC_MANAGE_FORBIDDEN`),
so a `write` grant cannot be used to give yourself more.

## Groups and roles

A group is a holon of custom type `OASIS_GROUP` whose metadata lists its members. Grant permissions to the group's id
exactly as to an avatar. To change who has access you change the group, not every holon: add someone to the group and they
gain everything granted to it; remove them and it all goes at once.

The group's own permissions decide who may change its membership. A role is just a group with a role-like name
("Head of School at Lincoln High").

**Groups can contain groups.** A group's member list may hold avatar ids and other group ids, so "all staff" can be made of
"each school's staff". Membership is transitive (up to 8 levels and 64 groups per person), and a loop (two groups that
contain each other) is harmless: each group is counted once.

## What is enforced where

| Action | Rule | Error code |
|---|---|---|
| Any Core load or search | The holon is returned whole, as a projection of readable fields, or not at all | (not returned) |
| API load by id, metadata, provider key | Same projection | 403 when nothing is readable |
| Save an existing holon (owned save path) | `write` on the holon, or `write` on every field changed | `COSMIC_WRITE_FORBIDDEN` |
| Save that changes permissions or group membership | `manage` | `COSMIC_MANAGE_FORBIDDEN` |
| Delete (owned delete path) | `delete` | `COSMIC_WRITE_FORBIDDEN` |
| Create under a **restricted** holon | `addChildren` on that parent (own or inherited) | `COSMIC_ADD_CHILDREN_FORBIDDEN` |
| Change permissions, sharing or group members | `manage` (or Wizard) | `COSMIC_SHARE_FORBIDDEN` |

A holon is **restricted** when it has an `OASIS_PERMISSIONS` value, even an empty one. Creating under a parent that is an
avatar, or a holon with no permissions, is unchanged. Nothing existing is newly refused until an owner restricts it.

Loads work in two steps: the provider returns the rows, then the manager applies the rules once. It fetches what the
rules need a single time per call (the parents of the rows, and the groups the caller belongs to), so a list of 500
holons costs a handful of extra loads, not 500.

## API

All need `manage` on the holon (the creator always has it) unless stated.

### Set permissions: `POST api/share/permissions/{holonId}`

```json
{
  "grants":  { "<grantee>": ["read", "write"], "<other>": [] },
  "inherit": { "<grantee>": ["read"] },
  "fields":  { "public": { "metadata.username": ["read"] }, "<grantee>": { "name": ["read", "write"] } },
  "inheritFields": { "<grantee>": { "metadata.note": ["read"] } }
}
```

For each section a grantee (or field) with names gets exactly those permissions; an empty list removes it; anything not
listed is unchanged. An empty body marks the holon restricted. It is one conditional save of just that holon with the
same stale-write protection as sharing: a concurrent change is a conflict, never a silent overwrite.

### Read permissions: `GET api/share/permissions/{holonId}`

Returns `{ "grants": {...}, "inherit": {...}, "fields": {...}, "inheritFields": {...} }`.

### Groups

* `POST api/share/groups` with `{ "name": "Speech therapists", "members": ["<avatarId>"] }` creates a group you own and
  returns it; use its `id` as a grantee.
* `POST api/share/groups/{groupId}/members` with `{ "add": ["<avatarId>"], "remove": ["<avatarId>"] }` changes members.

### JavaScript client

`@oasisomniverse/web4-api` 2.0.17 and later: `oasis.share.setPermissions`, `getPermissions`, `createGroup`,
`setGroupMembers`, each taking `{ holonId | groupId, ...body }`, and `oasis.avatar.setSharing` / `getSharing`
(see "Avatars").

### C#

```csharp
// Share only the username on a profile, with everyone.
await HolonAccess.SetPermissionsAsync(manager, profileHolonId, new HolonAccess.PermissionChange
{
    Fields = new Dictionary<string, IReadOnlyDictionary<string, HolonPermission>>
    {
        [HolonPermissionSet.Public] = new Dictionary<string, HolonPermission> { ["metadata.username"] = HolonPermission.Read },
    },
}, authenticatedAvatar);

// A group, a grant to it, and a membership change.
var group = await HolonAccess.CreateGroupAsync(manager, "Speech therapists", new[] { slpId }, authenticatedAvatar);
await HolonAccess.SetPermissionsAsync(manager, studentId, new HolonAccess.PermissionChange
{
    Grants = new Dictionary<string, HolonPermission>
        { [HolonPermissionSet.Key(group.Result.Id)] = HolonPermission.Read | HolonPermission.Write },
}, authenticatedAvatar);

// Check access in your own code (stored holon, own grants + ancestors + groups).
if (!await manager.AllowsAsync(holon, actorId, HolonPermission.Write)) { /* refuse */ }
```

Take the `actor` from the authenticated identity, never from a request body.

## CQL (queries)

CQL's default read rule is owner or public, unchanged. A second rule,
`CosmicQueryReadAccess.OwnerPublicOrShared(actor, groupIds)`, also returns holons the owner granted **whole-holon read**
to the actor, to a group they are in, or to everyone. The rule is native: it is expressed as exact-match metadata tests on
`SHARED_WITH_<id>` lookup flags that `HolonAccess` keeps in step with the grants (so providers can use their indexes),
and every returned row is re-checked against its stored grants. A flag on its own never grants access.

It is opt-in: set `CqlV1Compiler.SharedReadGroups` at host start-up to a function returning the caller's group ids.
Limits: it serves whole-holon read only (field-limited readers use the regular load endpoints, which project), it does
not follow inherited grants (they would need a flag copied to every descendant), and joins and projection composition,
which only see an id, a public flag and a creator, keep the owner/public rule and do not return shared rows. It also
needs a provider that supports metadata-exists tests.

## Avatars

An avatar record is kept in its own storage and is not a holon in the type system (it derives from the same base, so it
carries the same metadata and fields). The same permission rules apply to it, with two differences: an avatar has no
parent, and credentials (password hash, tokens, refresh tokens) can **never** be shared, even with a `*` grant.

Avatars are private by default: only the person themselves (and a Wizard) can read an avatar. To share, the person chooses
what and with whom:

* `POST api/avatar/sharing/avatar` shares fields of the avatar itself (`username`, `firstName`, `email`...).
* `POST api/avatar/sharing/detail` shares fields of the profile details.
* `GET api/avatar/sharing/{avatar|detail}` shows what is currently shared.

The body is the same as `POST api/share/permissions`. To share only a username with everyone, as a public profile page
would show:

```json
{ "fields": { "public": { "username": ["read"] } } }
```

Anyone else who then calls `get-by-id`, `get-by-username`, `get-by-email` or `get-avatar-detail-by-*` gets a copy of the
avatar holding the username and nothing else. A person who has shared nothing, and an avatar that does not exist, both
answer "Unauthorized", so the response does not reveal who is registered. Wizards can set sharing on someone's behalf with
`?avatarId=`. List endpoints (`get-all-*`) remain Wizard-only; portraits and UMA JSON remain owner-or-Wizard.

Code: `AvatarManager.ViewAsAsync` (read), `SetSharingAsync` / `GetSharingAsync` (write) in
`AvatarManager-Access.cs`, used by `AvatarProfileController`.

## How PIPER uses it

PIPER keeps districts, schools, staff and students under the System Administrator's avatar. Before this change only the SA
could read them, which is why a signed-in SLP got "Your account is not linked to an administrator yet". piper-server sets
access in one place (`api/_access.js`) using **groups**: one for all staff, one per school, one per district. Records are
granted to groups: the SA has everything; a student is read and write for its school's staff group; a school or district is
read for its staff group; a staff record is read for that person and the staff groups of their schools.

Adding, moving or removing staff therefore edits group membership (and the one staff record), not every record. The group
ids are kept in a `PIPER_ACCESS` record under the SA, readable by all staff. The SA's **Reapply access** button creates
anything missing and sets everything again. If OASIS cannot provide groups (an older server), PIPER falls back to granting
each staff member individually, which is the same access with more bookkeeping.

## Limits and what is not done

* **CQL does not follow inherited grants.** CQL's descendant operator looks downward from a holon ("has a descendant
  matching"); an inherited grant needs the opposite ("has an ancestor granting this"). Supporting it natively means a new
  operator in every provider's query translator. Use the regular load endpoints, which follow inheritance, or grant
  directly on the holons a query must return.
* **CQL does not serve field-limited grants**, because a CQL row is a whole holon. Field-limited readers use the load
  endpoints, which project.
* **Joins and projection composition** check access with only an id, a public flag and a creator, so they cannot see a
  grant stored in metadata. They keep the owner/public rule.
* **Avatar portraits and UMA JSON** are separate records and stay owner-or-Wizard.
* The rules run after the provider returns rows (as the creator and public rules always have); only the CQL shared-read
  rule is a native predicate.

## Tests

Core: `HolonPermissionsTests` (grants, format, groups, inheritance, fields, projection, diffs),
`CosmicOwnedSavePermissionTests` (write, delete, add-children, field writes, "a writer cannot change access"),
`CosmicSharedReadAccessTests`, `HolonVisibilityGraphTests` (nested groups, inherited fields and avatar projection are in
`HolonPermissionsTests`). API helper: `HolonAccessTests`. piper-server:
`tests/access.test.js`. JavaScript package: the `share.setPermissions` request test.
