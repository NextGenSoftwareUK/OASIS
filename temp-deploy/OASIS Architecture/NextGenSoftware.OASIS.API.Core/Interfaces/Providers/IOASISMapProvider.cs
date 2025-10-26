using System;
using System.Drawing;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISMapProvider
    {
        MapProviderType MapProviderType { get; set; }
        string MapProviderName { get; set; }
        string MapProviderDescription { get; set; }
        void SetCurrentMapProvider(MapProviderType mapProviderType);
        bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon);
        bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId);
        bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points);
        bool Draw2DSpriteOnHUD(object sprite, float x, float y);
        bool Draw2DSpriteOnMap(object sprite, float x, float y);
        bool Draw3DObjectOnMap(object obj, float x, float y);
		bool DrawRouteOnMap(float startX, float startY, float endX, float endY);
		bool DrawRouteOnMap(float startX, float startY, float endX, float endY, Color colour);
        bool HighlightBuildingOnMap(IBuilding building);
        bool PanMapDown(float value);
        bool PanMapLeft(float value);
        bool PanMapRight(float value);
        bool PanMapUp(float value);
        bool SelectBuildingOnMap(IBuilding building);
        bool SelectHolonOnMap(IHolon holon);
		bool SelectQuestOnMap(object quest);
        bool ZoomMapIn(float value);
        bool ZoomMapOut(float value);
        bool ZoomToHolonOnMap(IHolon holon);
        bool ZoomToHolonOnMap(Guid holonId);
        bool ZoomToQuestOnMap(IQuest quest);
        bool ZoomToQuestOnMap(Guid questId);

        // Placement helpers
        bool PlaceHolonOnMap(IHolon holon, float x, float y);
		bool PlaceBuildingOnMap(IBuilding building, float x, float y);
		bool PlaceQuestOnMap(object quest, float x, float y);
		bool PlaceGeoNFTOnMap(object geoNFT, float x, float y);
		bool PlaceGeoHotSpotOnMap(object geoHotSpot, float x, float y);
		bool PlaceOAPPOnMap(object oapp, float x, float y);

		// Route helpers between other entity types
		bool CreateAndDrawRouteOnMapBetweenQuests(object fromQuest, object toQuest);
		bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId);
		bool CreateAndDrawRouteOnMapBetweenGeoNFTs(object fromGeoNFT, object toGeoNFT);
		bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId);
		bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(object fromGeoHotSpot, object toGeoHotSpot);
		bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId);
		bool CreateAndDrawRouteOnMapBetweenOAPPs(object fromOAPP, object toOAPP);
		bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId);
		bool CreateAndDrawRouteOnMapBetweenBuildings(IBuilding fromBuilding, IBuilding toBuilding);
		bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId);

		// Zoom helpers for additional entities
		bool ZoomToGeoNFTOnMap(object geoNFT);
		bool ZoomToGeoNFTOnMap(Guid geoNFTId);
		bool ZoomToGeoHotSpotOnMap(object geoHotSpot);
		bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId);
		bool ZoomToOAPPOnMap(object oapp);
		bool ZoomToOAPPOnMap(Guid oappId);
		bool ZoomToBuildingOnMap(IBuilding building);
		bool ZoomToBuildingOnMap(Guid buildingId);
		bool ZoomToCoOrdsOnMap(float x, float y);
    }
}