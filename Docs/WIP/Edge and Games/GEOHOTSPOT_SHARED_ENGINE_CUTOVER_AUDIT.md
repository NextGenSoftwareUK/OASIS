# GeoHotSpot / GeoNFT shared-engine cutover audit

**Date:** 2026-09-19

| Runtime responsibility | Owner after cutover | Classification |
|---|---|---|
| GeoNFT public creation and collection API | WEB4/WEB5 GeoNFT controllers and managers | Retained specialised API |
| GeoHotSpot public creation and trigger API | WEB5 `GeoHotSpotsController` | Retained power-user API |
| Quantity, sharing, cooldown and eligibility | `GeoSpatialSpawnPolicy` | Shared invariant |
| GeoHotSpot map placement | `UnifiedGeoDisplaySystem` | Shared/reused placement |
| GeoHotSpot authored 2D/3D loading | `UnifiedAssetManager` | Shared/reused asset pipeline |
| GeoHotSpot observation | `GeoHotSpotTriggerManager` | Specialised trigger evidence |
| GeoNFT authoritative reappearance | `EnhancedNFTHotspotManager` eligibility reload | Migrated to shared policy result |
| `EnhancedNFTHotspot.HideGeoNFT` local timer | None | Redundant; removed |
| `GeoNFTGameObject.HideGeoNFT` local timer | None | Redundant; removed |
| Quest progress and events | shared `QuestManager` progress engine | Shared/reused |
| Inventory grants | canonical game inventory manager | Shared/reused |

The two public models and routes remain distinct. The cutover removes duplicate client-side rule decisions, not GeoNFT functionality. Scene/prefab GUID searches found no serialized reference to either private respawn method; the containing runtime components remain available and their active collection behavior is unchanged.
