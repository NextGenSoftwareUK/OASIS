using System;
using System.Drawing;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class MapManager : OASISManager, IMapManager
    {
        public MapManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public MapManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public IOASISMapProvider CurrentMapProvider { get; set; }
        public MapProviderType CurrentMapProviderType { get; set; }

        public void SetCurrentMapProvider(MapProviderType mapProviderType)
        {
            CurrentMapProviderType = mapProviderType;
        }

        public void SetCurrentMapProvider(IOASISMapProvider mapProvider)
        {
            CurrentMapProvider = mapProvider;
            CurrentMapProviderType = mapProvider.MapProviderType;
        }

        public bool Draw3DObjectOnMap(object obj, float x, float y)
        {
            CurrentMapProvider.Draw3DObjectOnMap(obj, x, y);
        }

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        {
            CurrentMapProvider.Draw2DSpriteOnMap(sprite, x, y);
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            CurrentMapProvider.Draw2DSpriteOnHUD(sprite, x, y);
        }

        //public bool HighlightBuildingOnMap(Building building)
        //{
        //    return true;
        //}

        public bool PlaceHolonOnMap(IHolon holon, float x, float y)
        {
            CurrentMapProvider.PlaceHolonOnMap(holon, x, y);
        }

        public bool PlaceBuildingOnMap(IBuilding building, float x, float y)
        {
            CurrentMapProvider.PlaceBuildingOnMap(building, x, y);
        }

        public bool PlaceQuestOnMap(IQuest quest, float x, float y)
        {
            CurrentMapProvider.PlaceQuestOnMap(quest, x, y);
        }

        public bool PlaceGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT, float x, float y)
        {
            CurrentMapProvider.PlaceGeoNFTOnMap(geoNFT, x, y);
        }

        public bool PlaceGeoHotSpotOnMap(IGeoHotSpot geoHotSpot, float x, float y)
        {
            CurrentMapProvider.PlaceGeoHotSpotOnMap(geoHotSpot, x, y);
        }

        public bool PlaceOAPPOnMap(IOAPP OAPP, float x, float y)
        {
            CurrentMapProvider.PlaceOAPPOnMap(OAPP, x, y);
        }

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY, Color colour)
        {
            CurrentMapProvider.DrawRouteOnMap(startX, startY, endX, endY, colour);
        }

        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBeweenPoints(points);
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenHolons(fromHolon, toHolon);
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenHolons(fromHolonId, toHolonId);
        }

        public bool CreateAndDrawRouteOnMapBetweenQuests(IQuest fromQuest, IQuest toQuest)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenQuests(fromQuest, toQuest);
        }

        public bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenQuests(fromQuestId, toQuestId);
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(IOASISGeoSpatialNFT fromGeoNFT, IOASISGeoSpatialNFT toGeoNFT)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenGeoNFTs(fromGeoNFT, toGeoNFT);
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenGeoNFTs(fromGeoNFTId, toGeoNFTId);
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(IGeoHotSpot fromGeoHotSpot, IGeoHotSpot toGeoHotSpot)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenGeoHotSpots(fromGeoHotSpot, toGeoHotSpot);
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenGeoHotSpots(fromGeoHotSpotId, toGeoHotSpotId);
        }

        public bool CreateAndDrawRouteOnMapBetweenOAPPs(IOAPP fromOAPP, IOAPP toOAPP)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenOAPPs(fromOAPP, toOAPP);
        }

        public bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenOAPPs(fromOAPPId, toOAPPId);
        }

        public bool CreateAndDrawRouteOnMapBetweenBuildings(IBuilding fromBuilding, IBuilding toBuilding)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenBuildings(fromBuilding, toBuilding);
        }

        public bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId)
        {
            CurrentMapProvider.CreateAndDrawRouteOnMapBetweenBuildings(fromBuildingId, toBuildingId);
        }

        public bool ZoomMapOut(float value)
        {
            CurrentMapProvider.ZoomMapOut(value);
        }

        public bool ZoomMapIn(float value)
        {
            CurrentMapProvider.ZoomMapIn(value);
        }

        public bool PanMapLeft(float value)
        {
            CurrentMapProvider.PanMapLeft(value);
        }

        public bool PanMapRight(float value)
        {
            CurrentMapProvider.PanMapRight(value);
        }

        public bool PanMapUp(float value)
        {
            CurrentMapProvider.PanMapUp(value);
        }

        public bool PanMapDown(float value)
        {
            CurrentMapProvider.PanMapDown(value);
        }

        //Select is same as Zoom so these functions are now redundant because zoom will zoom to and select the item on the map...
        //public bool SelectHolonOnMap(IHolon holon)
        //{
        //    return true;
        //}

        //public bool SelectHolonOnMap(Guid holonId)
        //{
        //    return true;
        //}

        //public bool SelectQuestOnMap(IQuest quest)
        //{
        //    return true;
        //}

        //public bool SelectQuestOnMap(Guid questId)
        //{
        //    return true;
        //}

        //public bool SelectGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT)
        //{
        //    return true;
        //}

        //public bool SelectGeoNFTOnMap(Guid geoNFTId)
        //{
        //    return true;
        //}

        //public bool SelectGeoHotSpotOnMap(IGeoHotSpot geoHotSpot)
        //{
        //    return true;
        //}

        //public bool SelectGeoHotSpotOnMap(Guid geoHotSpotId)
        //{
        //    return true;
        //}

        //public bool SelectOAPPOnMap(IOAPP oapp)
        //{
        //    return true;
        //}

        //public bool SelectOAPPOnMap(Guid oappId)
        //{
        //    return true;
        //}

        //public bool SelectBuildingOnMap(Building building)
        //{
        //    return true;
        //}

        public bool ZoomToHolonOnMap(IHolon holon)
        {
            CurrentMapProvider.ZoomToHolonOnMap(holon);
        }

        public bool ZoomToHolonOnMap(Guid holonId)
        {
            CurrentMapProvider.ZoomToHolonOnMap(holonId);
        }

        public bool ZoomToQuestOnMap(IQuest quest)
        {
            CurrentMapProvider.ZoomToQuestOnMap(quest);
        }

        public bool ZoomToQuestOnMap(Guid questId)
        {
            CurrentMapProvider.ZoomToQuestOnMap(questId);
        }

        public bool ZoomToGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT)
        {
            CurrentMapProvider.ZoomToGeoNFTOnMap(geoNFT);
        }

        public bool ZoomToGeoNFTOnMap(Guid geoNFTId)
        {
            CurrentMapProvider.ZoomToGeoNFTOnMap(geoNFTId);
        }

        public bool ZoomToGeoHotSpotOnMap(IGeoHotSpot geoHotSpot)
        {
            CurrentMapProvider.ZoomToGeoHotSpotOnMap(geoHotSpot);
        }

        public bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId)
        {
            CurrentMapProvider.ZoomToGeoHotSpotOnMap(geoHotSpotId);
        }

        public bool ZoomToOAPPOnMap(IOAPP oapp)
        {
            CurrentMapProvider.ZoomToOAPPOnMap(oapp);
        }

        public bool ZoomToOAPPOnMap(Guid oappId)
        {
            CurrentMapProvider.ZoomToOAPPOnMap(oappId);
        }

        public bool ZoomToBuildingOnMap(IBuilding building)
        {
            CurrentMapProvider.ZoomToBuildingOnMap(building);
        }

        public bool ZoomToBuildingOnMap(Guid buildingId)
        {
            CurrentMapProvider.ZoomToBuildingOnMap(buildingId);
        }

        public bool ZoomToCoOrdsOnMap(float x, float y)
        {
            CurrentMapProvider.ZoomToCoOrdsOnMap(x, y);
        }
    }
}
