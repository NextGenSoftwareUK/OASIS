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
