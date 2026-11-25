using System;
using System.Drawing;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.NianticLightshipOASIS
{
    /// <summary>
    /// Niantic Lightship map provider, unavailable. Niantic sunset the Lightship Maps SDK and
    /// shut its map endpoints down in October 2025; Lightship ARDK/VPS is a Unity client SDK
    /// with no server-side map API. Every map operation therefore returns false with the
    /// reason in <see cref="LastError"/>, rather than reporting success for work that is not done.
    /// </summary>
    public class NianticLightshipOASIS : IOASISMapProvider
    {
        public const string UnavailableReason =
            "Niantic Lightship is unavailable: the Lightship Maps SDK was sunset and its map endpoints shut down in October 2025.";

        /// <summary>Why the last operation failed.</summary>
        public string LastError { get; private set; }

        private bool Unavailable()
        {
            LastError = UnavailableReason;
            return false;
        }

        public MapProviderType MapProviderType { get; set; }
        public string MapProviderName { get; set; }
        public string MapProviderDescription { get; set; }

        public NianticLightshipOASIS()
        {
            MapProviderType = MapProviderType.NianticLightship;
            MapProviderName = "NianticLightship";
            MapProviderDescription = "Niantic Lightship OASIS Map Provider (unavailable: Lightship Maps SDK sunset October 2025)";
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon) => Unavailable();

        public bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId)
        => Unavailable();

        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        => Unavailable();

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        => Unavailable();

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        => Unavailable();

        public bool Draw3DObjectOnMap(object obj, float x, float y)
        => Unavailable();

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY)
        => Unavailable();

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY, Color colour)
        => Unavailable();

        public bool HighlightBuildingOnMap(IBuilding building)
        => Unavailable();

        public bool PanMapDown(float value) => Unavailable();
        public bool PanMapLeft(float value) => Unavailable();
        public bool PanMapRight(float value) => Unavailable();
        public bool PanMapUp(float value) => Unavailable();

        public bool SelectBuildingOnMap(IBuilding building)
        => Unavailable();

        public bool SelectHolonOnMap(IHolon holon)
        => Unavailable();

        public bool SelectQuestOnMap(object quest)
        => Unavailable();

        public void SetCurrentMapProvider(MapProviderType mapProviderType)
        {
            MapProviderType = mapProviderType;
        }

        public bool ZoomMapIn(float value) => Unavailable();
        public bool ZoomMapOut(float value) => Unavailable();

        public bool ZoomToHolonOnMap(IHolon holon) => Unavailable();
        public bool ZoomToHolonOnMap(Guid holonId) => Unavailable();
        public bool ZoomToQuestOnMap(IQuest quest) => Unavailable();
        public bool ZoomToQuestOnMap(Guid questId) => Unavailable();

        public bool PlaceHolonOnMap(IHolon holon, float x, float y) => Unavailable();
        public bool PlaceBuildingOnMap(IBuilding building, float x, float y) => Unavailable();
        public bool PlaceQuestOnMap(object quest, float x, float y) => Unavailable();
        public bool PlaceGeoNFTOnMap(object geoNFT, float x, float y) => Unavailable();
        public bool PlaceGeoHotSpotOnMap(object geoHotSpot, float x, float y) => Unavailable();
        public bool PlaceOAPPOnMap(object oapp, float x, float y) => Unavailable();

        public bool CreateAndDrawRouteOnMapBetweenQuests(object fromQuest, object toQuest) => Unavailable();
        public bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId) => Unavailable();
        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(object fromGeoNFT, object toGeoNFT) => Unavailable();
        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId) => Unavailable();
        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(object fromGeoHotSpot, object toGeoHotSpot) => Unavailable();
        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId) => Unavailable();
        public bool CreateAndDrawRouteOnMapBetweenOAPPs(object fromOAPP, object toOAPP) => Unavailable();
        public bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId) => Unavailable();
        public bool CreateAndDrawRouteOnMapBetweenBuildings(IBuilding fromBuilding, IBuilding toBuilding) => Unavailable();
        public bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId) => Unavailable();

        public bool ZoomToGeoNFTOnMap(object geoNFT) => Unavailable();
        public bool ZoomToGeoNFTOnMap(Guid geoNFTId) => Unavailable();
        public bool ZoomToGeoHotSpotOnMap(object geoHotSpot) => Unavailable();
        public bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId) => Unavailable();
        public bool ZoomToOAPPOnMap(object oapp) => Unavailable();
        public bool ZoomToOAPPOnMap(Guid oappId) => Unavailable();
        public bool ZoomToBuildingOnMap(IBuilding building) => Unavailable();
        public bool ZoomToBuildingOnMap(Guid buildingId) => Unavailable();
        public bool ZoomToCoOrdsOnMap(float x, float y) => Unavailable();
    }
}
