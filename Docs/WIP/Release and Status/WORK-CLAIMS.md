# Work claims (who is working on what)

Every session, local or cloud, that works in the OASIS repo claims its work here first, so that no two sessions do the same thing.

## How to claim

1. `git pull` Development and read this file and the status docs:
   - `Docs/WIP/HyperDrive-Providers/CURRENT_STATUS.md`
   - `Docs/WIP/COSMIC ORM/` (the checklist and per-provider `*_CQL_MVP.md` files)
   - `Docs/WIP/Release and Status/ACTIONS-FOR-DAVID.md`
2. Pick an item that is not claimed below (or whose claim is older than 24 hours with no commits since; mark that one `STALE` and take it over).
3. Add one row to the table, commit only this file with the message `claim: <item>`, and push to Development **before** writing any code. If the push is rejected, pull, re-check, and pick again if someone else claimed it.
4. Work on your own branch (`work/<session>-<item>`), and merge to Development as usual.
5. When the item is done or blocked, change its status (`DONE <commit>` or `BLOCKED: see ACTIONS-FOR-DAVID`) and take the next one.

Keep claims small (one provider, one checklist row or one feature), so that work spreads across sessions.

## Claims

| Item | Area | Session (local/cloud + name) | Branch | Claimed (UTC) | Status |
| --- | --- | --- | --- | --- | --- |
| Cardano native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 988b4bf4 (cardano-native-storage.yml green) |
| Starknet native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 988b4bf4 (starknet-native-storage.yml green) |
| Marqo native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 720c268f (matrix green) |
| Druid native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 988b4bf4 (druid-native-storage.yml green) |
| Pinot native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | IN PROGRESS |
| IPFS catalogue root pointer (dev replication-source fault) | HyperDrive replication | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | IN PROGRESS |
| ChainLinkOASIS / Connext / read-only chain adapters classification | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | IN PROGRESS |
