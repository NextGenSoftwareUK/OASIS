using System;

namespace NextGenSoftware.OASIS.API.Contracts.Interfaces
{
    /// <summary>
    /// OASIS Map Provider Interface
    /// Defines the contract for map providers in the OASIS ecosystem
    /// This interface can be referenced by both OASIS.API.Core and Unity projects
    /// </summary>
    public interface IOASISMapProvider
    {
        #region Core Properties
        MapProviderType MapProviderType { get; set; }
        string MapProviderName { get; set; }
        string MapProviderDescription { get; set; }
        bool IsInitialized { get; set; }
        #endregion

        #region Core Mapping Functions
        bool Draw3DObjectOnMap(object obj, float x, float y);
        bool Draw2DSpriteOnMap(object sprite, float x, float y);
        bool Draw2DSpriteOnHUD(object sprite, float x, float y);
        bool DrawRouteOnMap(float startX, float startY, float endX, float endY);
        #endregion

        #region Navigation Functions
        bool PanMapUp(float value);
        bool PanMapDown(float value);
        bool PanMapLeft(float value);
        bool PanMapRight(float value);
        bool ZoomMapIn(float value);
        bool ZoomMapOut(float value);
        #endregion

        #region Unity-Specific Functions (from IMapAPIProvider)
        IDirectionsAPIProvider DirectionsAPI { get; }
        IForwardGeocodingProvider GeocodingProvider { get; }
        #endregion

        #region OASIS-Specific Functions
        bool CreateAndDrawRouteOnMapBetweenHolons(object fromHolon, object toHolon);
        bool CreateAndDrawRouteOnMapBeweenPoints(object points);
        bool PlaceGeoNFTOnMap(object geoNFT, double latitude, double longitude);
        bool PlaceGeoHotSpotOnMap(object geoHotSpot, double latitude, double longitude);
        bool PlaceQuestOnMap(object quest, double latitude, double longitude);
        bool PlaceOAPPOnMap(object oapp, double latitude, double longitude);
        bool SelectHolonOnMap(object holon);
        bool HighlightBuildingOnMap(object building);
        bool ZoomToHolonOnMap(object holon);
        #endregion

        #region Location Management
        Geolocation GetCurrentLocation();
        System.Threading.Tasks.Task WaitForOriginSet();
        #endregion

        #region Pin Management
        bool DropPin(Geolocation coordinates, object gameObject);
        bool RemovePin(object gameObject);
        #endregion

        #region Distance Calculation
        float CalculateDistance(Geolocation location1, Geolocation location2);
        float CalculateDistance(double lat1, double lon1, double lat2, double lon2);
        #endregion

        #region Camera/Orbit Control
        bool UpdateOrbitValue(float value);
        #endregion

        #region Coordinate Conversion
        object ConvertLatLongToWorldPosition(double latitude, double longitude);
        object ConvertWorldPositionToLatLong(object worldPosition);
        #endregion

        #region Initialization
        void Initialize(object mapInstance);
        #endregion
    }
}
