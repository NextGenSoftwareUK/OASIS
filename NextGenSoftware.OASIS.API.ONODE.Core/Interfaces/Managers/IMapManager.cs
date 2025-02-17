using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface IMapManager
    {
        IOASISMapProvider CurrentMapProvider { get; set; }
        MapProviderType CurrentMapProviderType { get; set; }

        bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId);
        bool CreateAndDrawRouteOnMapBetweenBuildings(IBuilding fromBuilding, IBuilding toBuilding);
        bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId);
        bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(IGeoHotSpot fromGeoHotSpot, IGeoHotSpot toGeoHotSpot);
        bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId);
        bool CreateAndDrawRouteOnMapBetweenGeoNFTs(IOASISGeoSpatialNFT fromGeoNFT, IOASISGeoSpatialNFT toGeoNFT);
        bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId);
        bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon);
        bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId);
        bool CreateAndDrawRouteOnMapBetweenOAPPs(IOAPP fromOAPP, IOAPP toOAPP);
        bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId);
        bool CreateAndDrawRouteOnMapBetweenQuests(IQuest fromQuest, IQuest toQuest);
        bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points);
        bool Draw2DSpriteOnHUD(object sprite, float x, float y);
        bool Draw2DSpriteOnMap(object sprite, float x, float y);
        bool Draw3DObjectOnMap(object obj, float x, float y);
        bool DrawRouteOnMap(float startX, float startY, float endX, float endY);
        bool PanMapDown(float value);
        bool PanMapLeft(float value);
        bool PanMapRight(float value);
        bool PanMapUp(float value);
        bool SelectBuildingOnMap(Building building);
        bool SelectGeoHotSpotOnMap(Guid geoHotSpotId);
        bool SelectGeoHotSpotOnMap(IGeoHotSpot geoHotSpot);
        bool SelectGeoNFTOnMap(Guid geoNFTId);
        bool SelectGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT);
        bool SelectHolonOnMap(Guid holonId);
        bool SelectHolonOnMap(IHolon holon);
        bool SelectOAPPOnMap(Guid oappId);
        bool SelectOAPPOnMap(IOAPP oapp);
        bool SelectQuestOnMap(Guid questId);
        bool SelectQuestOnMap(IQuest quest);
        void SetCurrentMapProvider(IOASISMapProvider mapProvider);
        void SetCurrentMapProvider(MapProviderType mapProviderType);
        bool ZoomMapIn(float value);
        bool ZoomMapOut(float value);
        bool ZoomToBuildingOnMap(Guid buildingId);
        bool ZoomToBuildingOnMap(IBuilding building);
        bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId);
        bool ZoomToGeoHotSpotOnMap(IGeoHotSpot geoHotSpot);
        bool ZoomToGeoNFTOnMap(Guid geoNFTId);
        bool ZoomToGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT);
        bool ZoomToHolonOnMap(Guid holonId);
        bool ZoomToHolonOnMap(IHolon holon);
        bool ZoomToOAPPOnMap(Guid oappId);
        bool ZoomToOAPPOnMap(IOAPP oapp);
        bool ZoomToQuestOnMap(Guid questId);
        bool ZoomToQuestOnMap(IQuest quest);
    }
}