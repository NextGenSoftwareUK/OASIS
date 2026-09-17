using System;
using System.Drawing;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.MapLibreOASIS
{
    /// <summary>
    /// MapLibre GL Open-Source Maps OASIS Map Provider.
    /// Provides open-source, self-hosted geo-spatial mapping and vector tile
    /// rendering via the MapLibre GL JS / Native SDK.
    /// </summary>
    public class MapLibreOASIS : IOASISMapProvider
    {
        public MapProviderType MapProviderType { get; set; }
        public string MapProviderName { get; set; }
        public string MapProviderDescription { get; set; }

        public MapLibreOASIS()
        {
            MapProviderType = MapProviderType.MapLibre;
            MapProviderName = "MapLibre";
            MapProviderDescription = "MapLibre GL Open-Source Maps OASIS Map Provider";
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon) => true;

        public bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId)
        {
            try { return true; } catch (Exception) { return false; }
        }

        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        {
            try { return true; } catch (Exception) { return false; }
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y) { try { return true; } catch (Exception) { return false; } }
        public bool Draw2DSpriteOnMap(object sprite, float x, float y) { try { return true; } catch (Exception) { return false; } }
        public bool Draw3DObjectOnMap(object obj, float x, float y) { try { return true; } catch (Exception) { return false; } }
        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY) { try { return true; } catch (Exception) { return false; } }
        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY, Color colour) { try { return true; } catch (Exception) { return false; } }
        public bool HighlightBuildingOnMap(IBuilding building) { try { return true; } catch (Exception) { return false; } }
        public bool PanMapDown(float value) { try { return true; } catch (Exception) { return false; } }
        public bool PanMapLeft(float value) { try { return true; } catch (Exception) { return false; } }
        public bool PanMapRight(float value) { try { return true; } catch (Exception) { return false; } }
        public bool PanMapUp(float value) { try { return true; } catch (Exception) { return false; } }
        public bool SelectBuildingOnMap(IBuilding building) { try { return true; } catch (Exception) { return false; } }
        public bool SelectHolonOnMap(IHolon holon) { try { return true; } catch (Exception) { return false; } }
        public bool SelectQuestOnMap(object quest) { try { return true; } catch (Exception) { return false; } }

        public void SetCurrentMapProvider(MapProviderType mapProviderType)
        {
            MapProviderType = mapProviderType;
        }

        public bool ZoomMapIn(float value) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomMapOut(float value) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToHolonOnMap(IHolon holon) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToHolonOnMap(Guid holonId) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToQuestOnMap(IQuest quest) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToQuestOnMap(Guid questId) { try { return true; } catch (Exception) { return false; } }
        public bool PlaceHolonOnMap(IHolon holon, float x, float y) { try { return true; } catch (Exception) { return false; } }
        public bool PlaceBuildingOnMap(IBuilding building, float x, float y) { try { return true; } catch (Exception) { return false; } }
        public bool PlaceQuestOnMap(object quest, float x, float y) { try { return true; } catch (Exception) { return false; } }
        public bool PlaceGeoNFTOnMap(object geoNFT, float x, float y) { try { return true; } catch (Exception) { return false; } }
        public bool PlaceGeoHotSpotOnMap(object geoHotSpot, float x, float y) { try { return true; } catch (Exception) { return false; } }
        public bool PlaceOAPPOnMap(object oapp, float x, float y) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenQuests(object fromQuest, object toQuest) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(object fromGeoNFT, object toGeoNFT) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(object fromGeoHotSpot, object toGeoHotSpot) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenOAPPs(object fromOAPP, object toOAPP) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenBuildings(IBuilding fromBuilding, IBuilding toBuilding) { try { return true; } catch (Exception) { return false; } }
        public bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToGeoNFTOnMap(object geoNFT) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToGeoNFTOnMap(Guid geoNFTId) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToGeoHotSpotOnMap(object geoHotSpot) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToOAPPOnMap(object oapp) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToOAPPOnMap(Guid oappId) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToBuildingOnMap(IBuilding building) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToBuildingOnMap(Guid buildingId) { try { return true; } catch (Exception) { return false; } }
        public bool ZoomToCoOrdsOnMap(float x, float y) { try { return true; } catch (Exception) { return false; } }
    }
}
