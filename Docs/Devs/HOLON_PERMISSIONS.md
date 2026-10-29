# Holon permissions

*Added 29 October 2026.*

Every holon has an owner, and the owner decides who else may read, write or delete it, create things under it, or change
those decisions. This replaces the older rule that only the creator could change a holon and that sharing meant read only.

## The five permissions

| Permission | Name in JSON | Lets the avatar |
|---|---|---|
| Read | `read` | See the holon in loads, searches and queries |
| Write | `write` | Change the holon (save over it) |
| Delete | `delete` | Delete the holon (soft or hard) |
| Add children | `addChildren` | Create holons underneath this one |
| Manage | `manage` | Change who has which permission on this holon |

The permissions are separate. Read plus write does not include delete. Manage is not implied by anything else, so an
SLP can be allowed to edit a student without being able to change who else may see the student.

## Fixed rules (cannot be removed by a grant)

1. **The creator has every permission** on a holon they created.
2. **An avatar can read a holon it is attached to**, meaning the holon's parent is that avatar (a role or profile record
   an administrator created for them). Read only.
3. **A public holon is readable by everyone**, when the request asks for public results.
4. **A Wizard may change permissions** on any holon (used for repair and migration).

Everything else is denied unless the owner granted it.

The older read-only recipient list (`SHARED_AVATAR_IDS`, written by `api/share/share-holon`) still works and still
grants Read.

## Where grants are stored

On the holon itself, in its metadata under `OASIS_PERMISSIONS`: a JSON object from avatar id to permission names.

```json
{
  "7f1c2a9e-0d34-4a51-9c1b-5e0a6a2d9f10": ["read", "write"],
  "b2d0c8f4-3a67-4c1e-8b29-1f4e7d6a0c33": ["read"]
}
```

Rules for the stored value: at most 1,024 avatars; every key is a non-empty GUID; names are case-insensitive and must be
one of the five. A malformed value grants nothing (it is never partly applied) and changes to that holon's access fail
until it is repaired. Saving writes the value sorted and without empty entries.

A holon with an `OASIS_PERMISSIONS` value, even an empty `{}`, is **restricted**: creating a child under it needs
`addChildren` (see below). Holons without the key behave exactly as before.

## What is enforced where

| Action | Rule | Error code |
|---|---|---|
| Load or search (every Core load path and the API `CanRead` checks) | Read | (holon is simply not returned) |
| Save an existing holon (owned save path) | Write | `COSMIC_WRITE_FORBIDDEN` |
| Delete a holon (owned delete path) | Delete | `COSMIC_WRITE_FORBIDDEN` |
| Create under a **restricted** holon | AddChildren on that parent | `COSMIC_ADD_CHILDREN_FORBIDDEN` |
| Change permissions or sharing | Manage (or Wizard) | `COSMIC_SHARE_FORBIDDEN` |

Creating under a parent that is an avatar, or a holon with no `OASIS_PERMISSIONS`, is unchanged: nothing new is
refused. The check only applies to holons their owner has chosen to restrict.

The code: `HolonPermissions` / `HolonPermission` in Core (`Holons/HolonPermissions.cs`), the read filter
`HolonVisibilityProjection.IsVisibleTo`, the write and delete checks in `HolonManager-OwnedBatchSave.cs` and
`HolonManager-OwnedDelete.cs`, and `HolonAccess` in the ONODE WebAPI.

## API

Both need `Manage` on the holon (the creator always has it) or a Wizard.

### Set permissions

`POST api/share/permissions/{holonId}`

```json
{ "grants": { "<avatarId>": ["read", "write"], "<otherAvatarId>": [] } }
```

* An avatar listed with names gets exactly those permissions (replacing what it had).
* An avatar listed with an empty list loses all its access.
* Avatars not listed are unchanged.
* An empty `grants` object marks the holon restricted without changing anyone's access.

The change is one conditional save of just that holon, with the same stale-write protection as sharing (a concurrent
change is a conflict, never a silent overwrite).

### Read permissions

`GET api/share/permissions/{holonId}` returns `{ "<avatarId>": ["read", ...] }` (stored grants only; the fixed rules
above are not listed).

### JavaScript client

`@oasisomniverse/web4-api` 2.0.17 and later: `oasis.share.setPermissions({ holonId, grants })` and
`oasis.share.getPermissions({ holonId })`.

### C#

```csharp
// Grant a Head of School read and write on a student, remove another avatar's access.
var result = await HolonAccess.SetPermissionsAsync(
    holonManager, studentHolonId,
    new Dictionary<Guid, HolonPermission>
    {
        [hosAvatarId] = HolonPermission.Read | HolonPermission.Write,
        [formerSlpId] = HolonPermission.None,
    },
    authenticatedAvatar);

// Check access in your own code.
if (!HolonPermissions.Allows(holon, actorId, HolonPermission.Write, includePublic: false)) { /* refuse */ }
```

Take the `actor` from the authenticated identity, never from a request body.

## How PIPER uses it

PIPER stores districts, schools, staff and students as holons under the System Administrator's avatar. Before this
change only the SA could read them, which is why a signed-in SLP got "Your account is not linked to an administrator
yet". piper-server now sets permissions in one place (`api/_access.js`):

* The SA has every permission on every record.
* A student: staff assigned to the student's school can read and write.
* A school: staff assigned to it can read. A district: staff assigned to it can read.
* A staff record: its own person, and staff who share a school with them, can read.

It applies them when a record is created, when the SA saves, and whenever staff are added, changed or removed. Staff
Management has an SA-only **Reapply access** button (and `POST /api/admin_data {"action":"regrant_access"}`) that sets
all of it again; run it once after this change reaches the server so existing records come under the model.

## Design notes

* Why per holon rather than per role: roles and organisations are application concepts. OASIS gives the owner a
  generic, auditable switch per holon, and applications such as PIPER decide who should hold it.
* Why `addChildren` is opt-in: before this change anyone could create a child under any holon id. Making every parent
  restricted would break existing apps, so only parents with `OASIS_PERMISSIONS` restrict their children.
* Not done yet: inheritance (a grant on a parent applying to its descendants) and groups or roles as grantees. Both
  would sit on top of the same stored value. Until then an application grants per record, as PIPER does.
* The read rule is applied after the provider returns rows, the same place as the earlier creator and public rules;
  it is not yet a native query predicate in CQL.

## Tests

`HolonPermissionsTests`, `CosmicOwnedSavePermissionTests` (Core), `HolonVisibilityGraphTests`, `HolonAccessTests`
(ONODE), `tests/access.test.js` (piper-server) and a `setPermissions` request test in the JavaScript package.
