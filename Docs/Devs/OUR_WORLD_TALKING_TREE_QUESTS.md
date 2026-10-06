# Our World talking-tree quests

## Purpose

This document defines how real-tree AR interactions should use the existing STAR/OASIS Quest and GeoHotSpot systems. The design deliberately reuses the current requirement dictionaries, quest rewards, GeoHotSpot-to-quest links, and cross-game events. It does not introduce a parallel tree quest system.

## Existing contracts to reuse

### GeoHotSpot triggers

`GeoHotSpotTriggeredType` already contains:

- `WhenArrivedAtGeoLocation`
- `WhenAtGeoLocationForXSeconds`
- `WhenLookingAtObjectOrImageForXSecondsInARMode`
- `WhenObjectOrImageIsTouchedInARMode`

The talking-tree interaction should use `WhenLookingAtObjectOrImageForXSecondsInARMode`.

The authoritative trigger route is:

```http
POST /api/geohotspots/{geoHotSpotId}/trigger
```

Its request DTO is `TriggerGeoHotSpotRequest` in `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Models/GeoHotSpotTriggerDtos.cs`:

```json
{
  "idempotencyKey": "00000000-0000-0000-0000-000000000001",
  "triggerType": "WhenLookingAtObjectOrImageForXSecondsInARMode",
  "observedAtUtc": "2026-10-04T12:00:00Z",
  "latitude": 50.123,
  "longitude": -1.234,
  "accuracyMetres": 5.0,
  "continuousDurationSeconds": 3.0,
  "gameSource": "Our World"
}
```

The API response already returns quest transitions, refreshed quests, cross-game events, inventory/GeoNFT rewards, Karma/XP awards, and updated totals.

### Recognition evidence

`GeoHotSpotTriggerService.ValidateEvidence` now validates the authored trigger type, continuous duration, geographic boundary, recognition identity, and minimum confidence for AR look/touch triggers. The existing trigger request carries portable recognition evidence rather than using a parallel trigger subsystem:

- recognition target key/class, such as `our-world-tree-v1` / `Tree`;
- optional target instance or quest-giver ID;
- model/profile version;
- confidence and evidence/attestation reference where appropriate;
- location validation for AR trigger types when the linked hotspot has a geographic boundary.

Our World submits `our-world-tree-v1` / `Tree`, its detector confidence and model version, and a session evidence reference. This makes the server the authority for whether the authored evidence contract was satisfied. The evidence reference is an audit correlation value; it is not a cryptographic proof of the camera pixels.

### Quest and objective links

Both `QuestBase` and `Objective` already expose `LinkedGeoHotSpotId`. Objectives also support:

- `NeedToGoToGeoHotSpots` / `GeoHotSpotsArrived`;
- `CrossGameEventsOnGeoHotSpotTriggered`;
- `CrossGameEventsOnActivate`;
- `CrossGameEventsOnComplete`.

GeoHotSpots can already be linked to quests. These existing relationships are the location/activation authority; do not add duplicate latitude/longitude fields to a special tree quest model.

Use the links as follows:

- quest-level `LinkedGeoHotSpotId`: where the quest is offered or available;
- objective-level `LinkedGeoHotSpotId`: where that objective happens;
- `NeedToGoToGeoHotSpots`: one or more hotspot visits required for completion;
- `CrossGameEventsOnGeoHotSpotTriggered`: tree greeting or reaction to the accepted hotspot trigger;
- `CrossGameEventsOnActivate`: instructions/effects when the objective becomes active;
- `CrossGameEventsOnComplete`: completion presentation and downstream effects.

## CrossGameEventsOnActivate lifecycle

`CrossGameEventsOnActivate` is already emitted in two places:

1. `GET /api/quests/{id}/first-objective-events` returns the first incomplete objective's activation events after a quest starts.
2. `QuestManager.Progress` emits the next objective's activation events after an in-order objective completes.

Supported event data includes narration, audio, image, video, animation, entity spawn, portal unlock, teleport and website events. A target game must have an explicit handler/binding for world-changing events. Merely authoring `SpawnEntity` does not make an arbitrary classname spawnable.

In Our World, `OurWorldCrossGameEventDispatcher` resolves `EntityClassname`, `PortalId`, and `TargetMap` through serialized scene bindings. A seed classname is not operational until a matching prefab binding exists.

## Correct objective API shape

The earlier conversational sample placed `needToCollectItems` directly on an objective. That resembles the persisted `Objective` domain object and the full quest seed payload, but it is **not** the DTO shape of the add-objective endpoint.

For:

```http
POST /api/quests/{questId}/objectives
```

requirements must be nested under `dictionaries`, matching `AddQuestObjectiveRequest` and `QuestObjectiveDictionariesRequest`:

```json
{
  "title": "Gather the Fallen Seeds",
  "description": "Find five fallen oak seeds near this tree.",
  "gameSource": "Our World",
  "order": 0,
  "linkedGeoHotSpotId": "11111111-1111-1111-1111-111111111111",
  "rewardKarma": 15,
  "rewardXP": 30,
  "dictionaries": {
    "needToCollectItems": {
      "Our World": [
        "seed:oak",
        "seed:oak",
        "seed:oak",
        "seed:oak",
        "seed:oak"
      ]
    }
  },
  "crossGameEventsOnActivate": [
    {
      "eventType": "ShowNarration",
      "targetGame": "Our World",
      "narrationText": "Five of my seeds were scattered nearby. Please return them to me."
    },
    {
      "eventType": "PlayAudio",
      "targetGame": "Our World",
      "audioUrl": "oasis://our-world/oak-tree-seed-request",
      "audioTitle": "The oak asks for help"
    }
  ]
}
```

The repeated item key is required by the current item requirement semantics: named entries are matched by `ItemCollectedName`, while the number of entries is the required count. A numeric first entry such as `["5"]` means five generic item pickups and does not constrain the item identity.

The full quest creation/seed route accepts a `Quest` domain object. Its objective requirement dictionaries therefore appear directly on each objective, as demonstrated by `Scripts/seed_our_world_tree_quest.ps1`. Do not mix the full-domain payload with the add-objective DTO payload.

## Requirement dictionaries

Continue using existing requirements where their semantics match:

- seed/litter/GeoNFT pickup: `NeedToCollectItems`;
- visit one or more places: `NeedToGoToGeoHotSpots`;
- location labels supplied by a game: `NeedToVisitLocations`;
- XP, Karma, keys, weapons, monsters, levels and timed requirements: their existing dictionaries.

Do not add `TreeQuest`, `CollectSeedObjective`, or `CollectLitterObjective` types. Quest type remains narrative (`MainQuest`, `SideQuest`, `MagicQuest`, `EggQuest`), while requirement dictionaries describe completion.

Planting is not collection. If planting must become an authoritative completion condition, add one generic action requirement/progress pair (for example `NeedToPerformActions` / `ActionsPerformed`) that can also represent watering, talking, photographing, or restoring. Do not falsely record planting as an item pickup.

## Geographic boundaries

The current GeoHotSpot contract is a circle: `Lat`, `Long`, and `HotSpotRadiusInMetres`. Reuse it for individual trees and simple park areas.

For irregular parks, extend the same GeoHotSpot with an optional boundary definition:

- `Circle`: centre coordinate plus radius;
- `Polygon`: an ordered list of at least three latitude/longitude vertices.

A polygon must not be limited to four coordinates. Existing hotspots default to `Circle`, preserving compatibility. Quest/objective records continue to reference only the GeoHotSpot ID.

## Talking-tree flow

1. Load eligible GeoHotSpots and their existing quest relationships.
2. Confirm the avatar is inside the hotspot boundary.
3. In AR, recognise a real tree continuously for the authored duration.
4. Submit the existing idempotent GeoHotSpot trigger with location, duration, and recognition evidence.
5. Use returned `CrossGameEventsOnGeoHotSpotTriggered` for the tree greeting.
6. Offer/start the linked eligible quest through the normal quest API.
7. Play the first objective's `CrossGameEventsOnActivate`.
8. Track seeds/litter with existing item requirements and GeoHotSpot visits with existing hotspot requirements.
9. Apply existing objective/quest InventoryItem, Karma, and XP rewards and returned presentation events.

One specific tree uses a small circular GeoHotSpot. Any suitable tree within a park uses the park's circle or polygon plus the recognition target class. Both conditions must be satisfied when a quest is scoped to a park.

## Implementation rule

GeoHotSpot trigger acceptance and quest progression remain server-authoritative and idempotent. The Unity client detects and presents; it must not independently infer quest completion, grant rewards, or select an arbitrary quest merely because it has a startup sequence.

`Scripts/seed_our_world_tree_quest.ps1` owns only Anorak's original startup quest and its endangered-tree GeoNFT objectives. It deliberately has no park GeoHotSpot relationship. `Scripts/seed_our_world_talking_tree_quests.ps1` separately authors the recognised park trees and their seed/litter quests, linking each dedicated quest and objective to its own stable GeoHotSpot.
