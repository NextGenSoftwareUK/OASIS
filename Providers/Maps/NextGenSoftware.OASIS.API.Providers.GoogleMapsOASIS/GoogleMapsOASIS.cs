using System;
using System.Drawing;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS
{
    /// <summary>
    /// Google Maps Platform OASIS Map Provider.
    /// Map operations report failure until a rendering client is connected.
    /// </summary>
    public partial class GoogleMapsOASIS : IOASISMapProvider
    {
        public MapProviderType MapProviderType { get; set; }
        public string MapProviderName { get; set; }
        public string MapProviderDescription { get; set; }

        public GoogleMapsOASIS()
        {
            MapProviderType = MapProviderType.GoogleMaps;
            MapProviderName = "GoogleMaps";
            MapProviderDescription = "Google Maps Platform OASIS Map Provider";
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon) => false;

        public bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId)
        => false;

        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        => false;

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y) => false;
        public bool Draw2DSpriteOnMap(object sprite, float x, float y) => false;
        public bool Draw3DObjectOnMap(object obj, float x, float y) => false;
        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY) => false;
        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY, Color colour) => false;
        public bool HighlightBuildingOnMap(IBuilding building) => false;
        public bool PanMapDown(float value) => false;
        public bool PanMapLeft(float value) => false;
        public bool PanMapRight(float value) => false;
        public bool PanMapUp(float value) => false;
        public bool SelectBuildingOnMap(IBuilding building) => false;
        public bool SelectHolonOnMap(IHolon holon) => false;
        public bool SelectQuestOnMap(object quest) => false;

        public void SetCurrentMapProvider(MapProviderType mapProviderType)
        {
            MapProviderType = mapProviderType;
        }

        public bool ZoomMapIn(float value) => false;
        public bool ZoomMapOut(float value) => false;
        public bool ZoomToHolonOnMap(IHolon holon) => false;
        public bool ZoomToHolonOnMap(Guid holonId) => false;
        public bool ZoomToQuestOnMap(IQuest quest) => false;
        public bool ZoomToQuestOnMap(Guid questId) => false;
        public bool PlaceHolonOnMap(IHolon holon, float x, float y) => false;
        public bool PlaceBuildingOnMap(IBuilding building, float x, float y) => false;
        public bool PlaceQuestOnMap(object quest, float x, float y) => false;
        public bool PlaceGeoNFTOnMap(object geoNFT, float x, float y) => false;
        public bool PlaceGeoHotSpotOnMap(object geoHotSpot, float x, float y) => false;
        public bool PlaceOAPPOnMap(object oapp, float x, float y) => false;
        public bool CreateAndDrawRouteOnMapBetweenQuests(object fromQuest, object toQuest) => false;
        public bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId) => false;
        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(object fromGeoNFT, object toGeoNFT) => false;
        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId) => false;
        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(object fromGeoHotSpot, object toGeoHotSpot) => false;
        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId) => false;
        public bool CreateAndDrawRouteOnMapBetweenOAPPs(object fromOAPP, object toOAPP) => false;
        public bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId) => false;
        public bool CreateAndDrawRouteOnMapBetweenBuildings(IBuilding fromBuilding, IBuilding toBuilding) => false;
        public bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId) => false;
        public bool ZoomToGeoNFTOnMap(object geoNFT) => false;
        public bool ZoomToGeoNFTOnMap(Guid geoNFTId) => false;
        public bool ZoomToGeoHotSpotOnMap(object geoHotSpot) => false;
        public bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId) => false;
        public bool ZoomToOAPPOnMap(object oapp) => false;
        public bool ZoomToOAPPOnMap(Guid oappId) => false;
        public bool ZoomToBuildingOnMap(IBuilding building) => false;
        public bool ZoomToBuildingOnMap(Guid buildingId) => false;
        public bool ZoomToCoOrdsOnMap(float x, float y) => false;
    }
}
