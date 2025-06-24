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
            return true;
        }

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        {
            return true;
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            return true;
        }

        //public bool HighlightBuildingOnMap(Building building)
        //{
        //    return true;
        //}

        public bool PlaceHolonOnMap(IHolon holon, float x, float y)
        {
            return true;
        }

        public bool PlaceBuildingOnMap(IBuilding building, float x, float y)
        {
            return true;
        }

        public bool PlaceQuestOnMap(IQuest quest, float x, float y)
        {
            return true;
        }

        public bool PlaceGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT, float x, float y)
        {
            return true;
        }

        public bool PlaceGeoHotSpotOnMap(IGeoHotSpot geoHotSpot, float x, float y)
        {
            return true;
        }

        public bool PlaceOAPPOnMap(IOAPP OAPP, float x, float y)
        {
            return true;
        }

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY, Color colour)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenQuests(IQuest fromQuest, IQuest toQuest)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(IOASISGeoSpatialNFT fromGeoNFT, IOASISGeoSpatialNFT toGeoNFT)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(IGeoHotSpot fromGeoHotSpot, IGeoHotSpot toGeoHotSpot)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenOAPPs(IOAPP fromOAPP, IOAPP toOAPP)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenBuildings(IBuilding fromBuilding, IBuilding toBuilding)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId)
        {
            return true;
        }

        public bool ZoomMapOut(float value)
        {
            return true;
        }

        public bool ZoomMapIn(float value)
        {
            return true;
        }

        public bool PanMapLeft(float value)
        {
            return true;
        }

        public bool PanMapRight(float value)
        {
            return true;
        }

        public bool PanMapUp(float value)
        {
            return true;
        }

        public bool PanMapDown(float value)
        {
            return true;
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
            return true;
        }

        public bool ZoomToHolonOnMap(Guid holonId)
        {
            return true;
        }

        public bool ZoomToQuestOnMap(IQuest quest)
        {
            return true;
        }

        public bool ZoomToQuestOnMap(Guid questId)
        {
            return true;
        }

        public bool ZoomToGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT)
        {
            return true;
        }

        public bool ZoomToGeoNFTOnMap(Guid geoNFTId)
        {
            return true;
        }

        public bool ZoomToGeoHotSpotOnMap(IGeoHotSpot geoHotSpot)
        {
            return true;
        }

        public bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId)
        {
            return true;
        }

        public bool ZoomToOAPPOnMap(IOAPP oapp)
        {
            return true;
        }

        public bool ZoomToOAPPOnMap(Guid oappId)
        {
            return true;
        }

        public bool ZoomToBuildingOnMap(IBuilding building)
        {
            return true;
        }

        public bool ZoomToBuildingOnMap(Guid buildingId)
        {
            return true;
        }

        public bool ZoomToCoOrdsOnMap(float x, float y)
        {
            return true;
        }
    }
}
