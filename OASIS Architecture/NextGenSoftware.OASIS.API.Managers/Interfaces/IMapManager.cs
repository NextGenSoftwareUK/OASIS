using System;
using System.Drawing;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Managers.Interfaces
{
    /// <summary>
    /// Map Manager Interface for OASIS - Adapted from ONODE.Core for .NET Standard
    /// Provides high-level map management functionality
    /// </summary>
    public interface IMapManager
    {
        #region Properties
        IOASISMapProvider CurrentMapProvider { get; set; }
        MapProviderType CurrentMapProviderType { get; set; }
        #endregion

        #region Provider Management
        void SetCurrentMapProvider(MapProviderType mapProviderType);
        void SetCurrentMapProvider(IOASISMapProvider mapProvider);
        #endregion

        #region Basic Map Operations
        bool Draw3DObjectOnMap(object obj, float x, float y);
        bool Draw2DSpriteOnMap(object sprite, float x, float y);
        bool Draw2DSpriteOnHUD(object sprite, float x, float y);
        bool DrawRouteOnMap(float startX, float startY, float endX, float endY, Color colour);
        #endregion

        #region Navigation
        bool PanMapUp(float value);
        bool PanMapDown(float value);
        bool PanMapLeft(float value);
        bool PanMapRight(float value);
        bool ZoomMapIn(float value);
        bool ZoomMapOut(float value);
        #endregion

        #region Object Placement
        bool PlaceHolonOnMap(object holon, float x, float y);
        bool PlaceBuildingOnMap(object building, float x, float y);
        bool PlaceQuestOnMap(object quest, float x, float y);
        bool PlaceGeoNFTOnMap(object geoNFT, float x, float y);
        bool PlaceGeoHotSpotOnMap(object geoHotSpot, float x, float y);
        bool PlaceOAPPOnMap(object oapp, float x, float y);
        #endregion

        #region Route Creation
        bool CreateAndDrawRouteOnMapBetweenPoints(object points);
        bool CreateAndDrawRouteOnMapBetweenHolons(object fromHolon, object toHolon);
        bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId);
        bool CreateAndDrawRouteOnMapBetweenQuests(object fromQuest, object toQuest);
        bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId);
        bool CreateAndDrawRouteOnMapBetweenGeoNFTs(object fromGeoNFT, object toGeoNFT);
        bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId);
        bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(object fromGeoHotSpot, object toGeoHotSpot);
        bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId);
        bool CreateAndDrawRouteOnMapBetweenBuildings(object fromBuilding, object toBuilding);
        bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId);
        bool CreateAndDrawRouteOnMapBetweenOAPPs(object fromOAPP, object toOAPP);
        bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId);
        #endregion

        #region Zoom Operations
        bool ZoomToHolonOnMap(object holon);
        bool ZoomToHolonOnMap(Guid holonId);
        bool ZoomToQuestOnMap(object quest);
        bool ZoomToQuestOnMap(Guid questId);
        bool ZoomToGeoNFTOnMap(object geoNFT);
        bool ZoomToGeoNFTOnMap(Guid geoNFTId);
        bool ZoomToGeoHotSpotOnMap(object geoHotSpot);
        bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId);
        bool ZoomToOAPPOnMap(object oapp);
        bool ZoomToOAPPOnMap(Guid oappId);
        bool ZoomToBuildingOnMap(object building);
        bool ZoomToBuildingOnMap(Guid buildingId);
        bool ZoomToCoOrdsOnMap(float x, float y);
        #endregion

        #region Location Management (Added for Unity integration)
        Geolocation GetCurrentLocation();
        System.Threading.Tasks.Task WaitForOriginSet();
        #endregion

        #region Pin Management (Added for Unity integration)
        bool DropPin(Geolocation coordinates, object gameObject);
        bool RemovePin(object gameObject);
        #endregion

        #region Distance Calculation (Added for Unity integration)
        float CalculateDistance(Geolocation location1, Geolocation location2);
        float CalculateDistance(double lat1, double lon1, double lat2, double lon2);
        #endregion

        #region Camera/Orbit Control (Added for Unity integration)
        bool UpdateOrbitValue(float value);
        #endregion
    }
}

