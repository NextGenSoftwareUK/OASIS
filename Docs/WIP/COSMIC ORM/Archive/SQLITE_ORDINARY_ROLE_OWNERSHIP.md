> Superseded by [current status](../CURRENT_STATUS.md); retained historical and technical reference.

# SQLite ordinary-save role ownership

25 October 2026. Work in progress; do not deploy this batch yet.

The refreshed live demo correctly rejected its hand-written stored role metadata.
Exact reconciliation verified nine acknowledged Mongo records, three SQLite roots,
no Neo4j records and no usage initialization before retiring only the owned failed
fixture. The fixture must use logical child collections after the owner is fixed.

## Verified local changes

`HolonRepository.OrdinaryWrite.cs` now resolves role membership inside its immediate
transaction from the captured selection or protected existing record. A root-only
write preserves stored membership. The acknowledgement uses the committed models.

`HolonRepository.Graph.cs` publishes parent identities before role acknowledgement,
only after commit. Full native loads record complete observation only when their
registered physical role and loaded members match the stored manifest. Depth
frontiers and unknown typed roles retain incomplete-selection protection.

Eight focused native cases pass without skips: sync/async role order, root-only
preservation, acknowledged source observation, rollback nonpublication, full-load
replacement and rejection of replacement from a depth-limited view.

Evidence: `TestResults/SQLiteCurrent/ordinary-role-current.trx`, SHA256
`A4E08EB85AEBC480CA679E44DF037A8726B19ABBAC04BCAC89D48FABF5E2ABA8`.
Producer/consumer SQLite DLL hash:
`7987C37179201D17587E6E7040220B76B09CD262E246F7A4EE42CF29213B1C09`.
Owned fixture DB and pending intent are absent after terminal.

## Broader regression: not green

The initial unfiltered run executed 1,578 cases: 1,560 passed and 18 failed.
Two are historical reconciliation cases requiring an original retained fixture;
normal regression must exclude `SQLiteScopedGraphReconciliation`. Two CQL fixture
cases still assume an ordinary save creates no persisted role manifest and need
explicit native legacy-representation setup. Preserve their assertions and scope.

The remaining failures expose the manager/provider handoff:

- Eight graph-tracking cases fail on a later save, across V2 and legacy callers.
- Two V2 pending-value cases fail native ordinary-save admission.
- Four V2 typed-child cases fail acknowledgement membership validation.

Legacy caller failures are recorded here; no legacy Hyperdrive routing repair is
authorized. Shared storage/Core fixes required by V2 may affect both callers.

The 16-case focused manager/read follow-up passed the six ordinary-role controls
and retained ten manager failures. Its exact failure shows the **staged clone**
being rejected in `HolonGraphWritePlan.CreateForSave`, after the original caller
already passed `HolonSavePlan.Create` admission. Hydrating the native read alone
does not solve this transport-ownership loss.

Full-run TRX SHA256:
`1D38B491ECD5549D5DB4920C4B6D310A33BDBB513B405BF0EC6BC51BD344B3CF`.
Manager/read follow-up SHA256:
`6FF0BAD28BB3EF612DFF0A726A52B57BC3099E0196889BB35F47D9C2193151E6`.

## Next root owner

`Core/Managers/HolonManager/HolonSavePlan.cs` captures original logical collections
and their role manifests, then flattens staged children into physical `Children`.
The source runtime type survives, but its private typed collections may be empty.
Native ordinary planners therefore recapture a different role shape. Persisting
that recaptured shape makes acknowledgement validation correctly reject it.

Fix the generic prepared-write boundary in Core/Shared. Carry the already-admitted
logical role intent with private process-local ownership bound to the exact staged
record; validate identities, parent edges, membership and immutable observation.
Do not trust serialized `ChildRoleManifestJson` as an authority grant or bypass
known-partial selection restrictions. Conditional submitted-relationship and replay
contracts must remain intact. Native callers still capture their own logical roles.

Then rerun manager/type regressions, normal SQLite regression (historical reconcile
cases excluded), and the fresh three-provider SDK/HTTP demo. The Mongo ordinary-save
selection guard also needs native red/green coverage: its single/batch paths still
use structural `Create` rather than `CreateForSave`. No Mongo production edit has
been made in this batch. Join/federation and all other roadmap scope remain open.

## Verified resolution — 25 October 2026

The generic Core/Shared prepared-write boundary now retains the original logical
role manifest through exact live staged-object ownership. Identity, child order,
parent edges and stored observation are validated; copies, partial observations
and disposed plans cannot acquire that authority. SQLite ordinary writes resolve
and persist roles under their existing immediate transaction, preserve stored
roles for root-only saves, and publish caller observations only after commit.
The manager remains responsible for acknowledging its own staged records.

Verification completed: Core 2,604; Shared 1,189; focused SQLite manager/native
22; normal SQLite integration 1,578, all passing with zero skips. The normal suite
excludes the two historical reconciliation cases requiring original evidence.
Fresh three-provider acceptance passed 87 SDK examples and 87 signed HTTP examples,
267 settled operations and 262 provider attempts. All six producer/consumer binary
pairs matched and the source snapshot remained unchanged during execution.
Owned SQLite/demo databases and pending fixture markers were absent afterward.
These are separate verified cohorts, not a single all-provider full regression.

The earlier red evidence above is retained as history and is superseded for this
specific boundary. MongoDB/Neo4j ordinary-save admission and native negative/positive
controls are next; joins, federation and other unchecked roadmap items remain open.

### MongoDB ordinary admission verification completed

The native runner terminated successfully: full281 native CQL cases pass, zero
skips, 3m51s. Its four existing partial-view cases now exercise all direct
sync/async single/batch saves (16 probes), both legacy/no-manifest and manifested
records, child-type and access-filtered selections. Each probe records intent
before submission and proves the complete stored Holon BSON remains unchanged.
Existing manager root-only positive controls still pass. Full unit502 passes,
including eight new controls first reproduced red against the original code.
Only ordinary single/batch planning changed to CreateForSave; conditional/replay
plans remain intact. Single-record role persistence/acknowledgement and Neo4j
ordinary guard work remain open; this is admission certification only.
