# Our World quest and GeoNFT test fixtures

Run `Scripts/seed_our_world_quest_test_matrix.ps1` to create a stable development dataset beside the Anorak quest. The script uses the normal WEB4 mint/place APIs and WEB5 quest API. It stores IDs in `%LOCALAPPDATA%/OASIS/our-world-quest-test-matrix.json`, so interrupted and repeated runs reuse the same records.

Preview without changing API data:

```powershell
./Scripts/seed_our_world_quest_test_matrix.ps1 -PlanOnly
```

Seed or reconcile the development fixtures:

```powershell
./Scripts/seed_our_world_quest_test_matrix.ps1
```

## Quest modes

| Quest | Mode | Expected portal behavior |
| --- | --- | --- |
| Chromatic Canopy: Any Path | `AnyOrder` | All three eligible portals appear; collecting any one completes only its matching objective. |
| Celestial Garden: Follow the Sequence | `InOrder` | Only the current objective portal appears; the next becomes active after the current one completes. |
| Renewal Cycle: Living Echoes | `AnyOrder` | Three repeatable/cooldown portals may appear together; each objective progresses independently. |
| Custodians of Scarcity | `InOrder` | Global precedence, globally unlimited and exclusive fixtures activate sequentially. |

## GeoNFT rules and display metadata

| Item | Rarity | Permanent | Shared | Global | Per player | Cooldown | Purpose |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| Aurora Fern | Uncommon | No | Yes | 0 | 1 | 0 | Once per avatar; alphabetic/search fixture |
| Cobalt Mushroom | Rare | No | Yes | 0 | 1 | 0 | Description and rarity fixture |
| Ember Orchid | Epic | No | Yes | 0 | 1 | 0 | Any-order final authored row |
| Moonlit Reed | Common | Yes | Yes | 0 | 1 | 30 s | Unlimited permanent spawn with cooldown |
| Prism Bloom | Legendary | No | No | 0 | 2 | 0 | Exclusive claim and finite per-player quantity |
| Verdant Starfruit | Mythic | No | Yes | 5 | 1 | 0 | Global limit overrides per-player value |
| Solar Lotus | Celestial | Yes | Yes | 0 | 1 | 0 | Permanent, unlimited, immediate respawn |
| Tideglass Moss | Uncommon | Yes | Yes | 0 | 1 | 20 s | Permanent, unlimited, timed respawn |
| Echo Seed | Rare | No | Yes | 0 | -1 | 10 s | Unlimited per-avatar supply with cooldown |
| Crystal Thistle | Epic | No | Yes | 2 | 5 | 0 | Finite global limit overrides player limit |
| Obsidian Pod | Legendary | No | Yes | -1 | 0 | 15 s | Unlimited global value overrides zero player limit |
| Silver Lichen | Mythic | No | No | 0 | 2 | 5 s | Exclusive claim with two collections for the claimant |
| Dormant Bulb | Dormant | No | Yes | 0 | 0 | 0 | Intentionally ineligible; portal must remain hidden |
| Exhausted Cone | Exhausted | No | No | 0 | 0 | 60 s | Intentionally ineligible exclusive fixture |

The unique names, descriptions, images and rarity metadata are intentional. Use them to verify inventory live search, name/source/rarity ordering, identity filtering, image loading and selected-row highlighting. Use the four quest titles and descriptions to verify quest search and selected-row highlighting.

## Coverage boundaries

The hosted records cover every behaviorally distinct rule class: permanent unlimited
with zero/nonzero cooldown, per-avatar zero/finite/unlimited limits, global
finite/unlimited precedence, shared and exclusive placement claims, and both quest
objective ordering modes. The automated 1,536-case Cartesian policy suite remains
the exhaustive verification for redundant numeric combinations and boundary times.
The two zero-limit fixtures are intentionally excluded from quests because including
an ineligible objective would make that quest impossible to complete.

This suite does not edit or reset Anorak. Collection history is persistent, so use isolated avatars when testing multiplayer and global/exclusive limits. The complete policy and manual acceptance matrix remains in [GEONFT_COLLECTION_TEST_MATRIX.md](GEONFT_COLLECTION_TEST_MATRIX.md).
