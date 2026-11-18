# OPORTAL profile save audit — 18 November 2026

Status: source findings confirmed; correction and end-to-end acceptance remain open. Agent-owned work; no owner action requested. Shared Atlas stand-down remains in force and no request was sent to verify these findings.

The frontend reviewed is `C:/Source/OPORTAL-JS-performance` at `4982e73`, on `work/oportal-subscription-performance`. Backend evidence is the qualified WEB4 graph at `6e82135e20d1fccf7cfbaf4cf1a2f368f424335f`, pinned Core `30c89aaf55ff7f2eae25d1217b2736e50fd0452a`. Read-only inspection of the other checkout's Core `0f8ce6d9bab4a76d35856b96c64e759d83f21a53` also found the unconditional quest assignments; that checkout observation is not a claim about its release qualification.

## Confirmed source behavior

- `assets/js/avatar-modal.js`, `submitAvatar`: first sends `updateAvatarDetail` (or an email/username detail variant), then independently sends `updateByEmail`/`updateByUsername`. The second write runs even if the detail response reports failure. Its network exception is suppressed, so a first-write success can still produce a success message after the second write failed. The local profile merges requested values rather than proving both persisted records match.
- The form exposes title, first name, last name, username, email and address. Its second payload omits the edited email and uses truthiness checks that cannot clear existing strings. These are source findings, not observed successful/failed saves on a live account.
- `Controllers/AvatarProfileController.cs` delegates the ID detail endpoint to `AvatarManager.UpdateAvatarDetailAsync`. Its ordinary Avatar update handles identity/profile fields but not address. The two endpoints do not form a single save contract.
- Core `Managers/AvatarManager/AvatarManager-SaveUtils.cs`, private `UpdateAvatarDetailAsync`, unconditionally copies `ActiveQuestId` and `ActiveObjectiveId` from the submitted detail object. A profile-only detail payload does not supply those values. The same broad method contains other non-profile state updates, so it must not be treated as a presence-aware profile patch.
- Profile hydration uses missing optional title/address values to decide whether another detail read is needed. Legitimately empty fields therefore remain a repeat-read trigger. Portrait population also requests another portrait and explicitly carries a TODO; cache changes must preserve session ownership and upload invalidation.

## Required correction and evidence

Define one profile-edit contract with explicit field presence, ownership checks and allowed user-editable fields. Omitted game, quota, verification, security and other server-managed state must remain unchanged. Distinguish a requested empty value from an omitted field. Establish the canonical source and synchronization behavior of shared Avatar/AvatarDetail identity fields before replacing the frontend's duplicate writes; do not add another compensating client update or silently accept partial failure.

Required tests include profile-only saves preserving active quest/objective and unrelated detail state; permitted empty field clears; email/username uniqueness and ownership; first/second persistence failures producing truthful results; stale session response rejection; and reload showing persisted values through real eligible providers. Use owned isolated backends, never the shared OASIS cluster during this incident. Existing hydration tests may be retained when unchanged; add only regressions for changed behavior.

No profile-save code or frontend deployment was changed by this audit. This remains an OPORTAL completeness blocker, separate from the qualified worker/audit-maintenance release.
