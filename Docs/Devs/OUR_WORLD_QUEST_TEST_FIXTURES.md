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

## GeoNFT rules and display metadata

| Item | Rarity | Permanent | Shared | Global | Per player | Cooldown | Purpose |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| Aurora Fern | Uncommon | No | Yes | 0 | 1 | 0 | Once per avatar; alphabetic/search fixture |
| Cobalt Mushroom | Rare | No | Yes | 0 | 1 | 0 | Description and rarity fixture |
| Ember Orchid | Epic | No | Yes | 0 | 1 | 0 | Any-order final authored row |
| Moonlit Reed | Common | Yes | Yes | 0 | 1 | 30 s | Unlimited permanent spawn with cooldown |
| Prism Bloom | Legendary | No | No | 0 | 2 | 0 | Exclusive claim and finite per-player quantity |
| Verdant Starfruit | Mythic | No | Yes | 5 | 1 | 0 | Global limit overrides per-player value |

The unique names, descriptions, images and rarity metadata are intentional. Use them to verify inventory live search, name/source/rarity ordering, identity filtering, image loading and selected-row highlighting. Use the two quest titles and descriptions to verify quest search and selected-row highlighting.

This suite does not edit or reset Anorak. Collection history is persistent, so use isolated avatars when testing multiplayer and global/exclusive limits. The complete policy and manual acceptance matrix remains in [GEONFT_COLLECTION_TEST_MATRIX.md](GEONFT_COLLECTION_TEST_MATRIX.md).
