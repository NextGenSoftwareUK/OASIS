# GeoNFT collection and respawn verification

This is the acceptance checklist for the GeoNFT rule integration in Our World.
Policy tests, API persistence tests, and manual game tests have separate results.
Passing a policy test does not prove persistence, deployment, multiplayer safety,
or Unity presentation. Do not mark an unexecuted check as passed.

## Agreed rules

- `PermSpawn=true` allows unlimited collections, subject to cooldown and sharing.
- Otherwise, nonzero `GlobalSpawnQuantity` overrides `PlayerSpawnQuantity`.
- Global quantity `0` selects the per-player limit; `-1` means unlimited.
- Player quantity `0` permits no collections; `-1` means unlimited.
- `AllowOtherPlayersToAlsoCollect=false` prevents a different avatar collecting
  an already claimed placement.
- Cooldown is measured from the collecting avatar's last successful collection.
- Anorak trees use permanent=false, shared=true, global=0, player=1: each avatar
  may collect each tree once. Different eligible placements of the same item
  type may still contribute to inventory quantities.

## Automated policy suite

For reusable hosted fixtures covering `AnyOrder`, `InOrder`, permanent/cooldown,
per-player, exclusive and global-limit behavior, see
[Our World quest test fixtures](OUR_WORLD_QUEST_TEST_FIXTURES.md).

Run from the OASIS repository root:

```powershell
dotnet run --project Tests/GeoNFTCollectionRules/GeoNFTCollectionRules.csproj
```

The suite links the actual production policy source. Its Cartesian matrix is:

| Dimension | Values |
| --- | --- |
| Permanent | false, true |
| Sharing | false, true |
| Global limit | -1, 0, 1, 3 |
| Player limit | -1, 0, 1, 3 |
| This player's collection count | 0, 1, 3 |
| Another player has collected | false, true |
| Seconds since last collection | 0, 59, 60, 61 with a 60-second cooldown |

This produces 1,536 cases. Global count is the player's count plus one when
another player has collected; it is not an exhaustive independent global-count
matrix. Named boundary checks supplement this matrix.

## API and persistence acceptance

Use isolated test avatars and explicitly labelled test placements. Record the
deployed commit, endpoint, fixture IDs, before/after state, and actual response.
Do not consume or reset a user's quest progress as part of this suite.

| ID | Scenario | Required evidence |
| --- | --- | --- |
| API-01 | Each policy matrix configuration | Eligibility and collection agree; rejected requests do not mutate data |
| API-02 | Repeat allowed pickup | Quantity increases by exactly one; collection history increases by one |
| API-03 | Once-per-player Anorak tree | First succeeds, second rejects, another avatar can collect once |
| API-04 | Cooldown | Reject before deadline; allow at deadline; status exposes matching UTC deadline |
| API-05 | Global cap across avatars | Sum of successful collections never exceeds limit |
| API-06 | Exclusive claim | First avatar succeeds; different avatar rejects; original obeys remaining rules |
| API-07 | Simultaneous requests, same API process | No lost increments, duplicate claims, or over-limit successes |
| API-08 | Simultaneous requests, different API replicas | Same invariant as API-07; must be enforced in persistent storage |
| API-09 | Restart API and sign in again | Counts, cooldown, ownership and eligibility remain correct |
| API-10 | Spend, transfer, or remove inventory | Historical collection limits do not reset with current quantity |
| API-11 | Save failure | No success response or mutated cached inventory/history |
| API-12 | Authentication failure, missing placement, invalid metadata | Explicit error; no mutation or reward |
| API-13 | Different placements of same item type | Inventory aggregation preserves identities and independent eligibility |
| API-14 | Same placement, distinct avatars | Inventory and player cooldown are isolated; global limit remains shared |
| API-15 | Quest progression | Only confirmed matching pickups advance objectives; completion occurs once |
| API-16 | Existing inventory migration | Counts migrate as documented; unavailable historical consumption is not invented |
| API-17 | Creator settings round trip | Create each configuration through Creator Mode, reload it from WEB4, and compare all five rules with the submitted values |

## Manual Our World acceptance

Run each rule configuration with a fresh fixture, including zero, finite and
unlimited limits, sharing on/off, permanent on/off, and zero/nonzero cooldown.
For each configuration record avatar, placement, rule values, expected result,
actual result, log excerpt and screenshot/video. Repeat multiplayer cases with
two signed-in clients. Automated results cannot replace these visual checks.

1. Enter/re-enter the world: eligible portals appear above buildings and nearby
   detection opens; ineligible placements remain hidden.
2. Locate a portal: distance, steps and travel estimates update while moving.
3. Fetch: only confirmed success removes the portal, route and destination panel.
   Failure preserves a usable target and reports the actual API error.
4. Each successful pickup plays chest animation, blue particles, congratulations
   audio and the Objective Complete presentation. It increments inventory once.
5. Close inventory during refresh and change scenes: no dead-object/coroutine
   errors; reopening shows correct names, category, identity and quantity.
6. Eligible repeat pickups respawn when cooldown expires, including after scene
   reload, sign-out/sign-in and application restart. Exhausted placements do not.
7. A second avatar observes sharing/global limits correctly after a refresh.
8. Collect all five Anorak trees: Nature inventory, quest list and tracker agree;
   Quest Complete and its celebration appear once for the completion transition.
9. After collecting a target, approach another: detection and Locate still work.
10. Check duplicate coordinates, interrupted requests, and repeated Fetch clicks:
    no overlapping portals, duplicate rewards, stale routes, or error floods.
11. Open Creator Mode: the legacy-styled drawer slides from the right; Back and
    Close work. Edit all collection rules, return to placement, and verify draft
    values persist. Disabled limits follow the precedence described above.
12. Check quest authoring, objectives, prerequisites, subquests and event screens
    at desktop and narrow window sizes: readable text, no clipping, accessible
    close/back actions and retained drafts. Confirm saved API data matches input.

## Results and release gates

| Layer | Current recorded result |
| --- | --- |
| Local policy matrix | 1,536 combinations, 14 named boundary checks and invalid-limit rejection passed; rerun after any policy change |
| Hosted Anorak once-per-player path | The original four-tree path passed 2026-09-17. The canonical dataset was migrated to five named trees on 2026-09-18 and reset to 0/5; rerun the full five-pickup acceptance before release. See [run report](OUR_WORLD_ANORAK_RESET.md). |
| Other hosted policy combinations | Pending |
| Multiplayer across replicas | Pending; process-local locking alone is insufficient |
| Manual Unity matrix | Pending |

The current local collection implementation uses a process-local gate. This
does not establish atomic global limits across replicas or protect against other
inventory writers. API-08 is a release blocker until storage-level concurrency
is implemented and verified. Do not describe this integration as fully verified.

Deploy the new eligibility endpoint before enabling the matching Unity client.
Use [Railway dependency pins](RAILWAY_DEPENDENCY_PINS.md) and the
[release checklist](MERGE_DEVELOPMENT_TO_MASTER_QUICK_START.md) to keep the API
and its dependency commits aligned.
