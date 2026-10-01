using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geo;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS
{
    /// <summary>
    /// Anchoring OASIS entities to real-world coordinates, plus selection and
    /// highlighting. Placements are held authoritatively here, so a client that
    /// reconnects can rebuild the whole map from <see cref="Placements"/>.
    /// </summary>
    public partial class GOMapOASIS
    {
        /// <summary>The entity currently selected on the map, if any.</summary>
        public object SelectedHolon { get; private set; }

        /// <summary>The building currently highlighted on the map, if any.</summary>
        public object HighlightedBuilding { get; private set; }

        public bool PlaceGeoNFTOnMap(object geoNFT, double latitude, double longitude)
            => Place(GOMapPlacementKind.GeoNFT, geoNFT, latitude, longitude, "placeGeoNFT");

        public bool PlaceGeoHotSpotOnMap(object geoHotSpot, double latitude, double longitude)
            => Place(GOMapPlacementKind.GeoHotSpot, geoHotSpot, latitude, longitude, "placeGeoHotSpot");

        public bool PlaceQuestOnMap(object quest, double latitude, double longitude)
            => Place(GOMapPlacementKind.Quest, quest, latitude, longitude, "placeQuest");

        public bool PlaceOAPPOnMap(object oapp, double latitude, double longitude)
            => Place(GOMapPlacementKind.OAPP, oapp, latitude, longitude, "placeOAPP");

        /// <summary>Anchors any holon to a coordinate.</summary>
        public bool PlaceHolonOnMap(object holon, double latitude, double longitude)
            => Place(GOMapPlacementKind.Holon, holon, latitude, longitude, "placeHolon");

        /// <summary>Removes a placement, so the client stops rendering it.</summary>
        public bool RemovePlacement(Guid placementId)
        {
            if (!IsInitialized) return false;
            return _placements.TryRemove(placementId, out _);
        }

        /// <summary>Everything of one kind currently on the map.</summary>
        public IReadOnlyCollection<GOMapPlacement> GetPlacements(GOMapPlacementKind kind)
            => _placements.Values.Where(p => p.Kind == kind).ToList();

        /// <summary>
        /// Everything placed within a radius of a coordinate. This is what drives
        /// Our World's "what is near me" queries, and it uses true great-circle
        /// distance rather than a projected-metre approximation.
        /// </summary>
        public IReadOnlyCollection<GOMapPlacement> GetPlacementsNear(
            Geolocation location, double radiusMetres)
        {
            if (!GeoMath.IsValid(location) || radiusMetres <= 0.0)
                return new List<GOMapPlacement>();

            return _placements.Values
                .Where(p => p.Location != null
                            && GeoMath.HaversineMetres(location, p.Location) <= radiusMetres)
                .OrderBy(p => GeoMath.HaversineMetres(location, p.Location))
                .ToList();
        }

        public bool SelectHolonOnMap(object holon)
        {
            if (!IsInitialized || holon == null) return false;

            SelectedHolon = holon;

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.SelectHolon)
            {
                Target = holon,
                Location = ExtractLocation(holon)
            });

            command.AppliedToLiveMap = Bridge.TryInvoke("selectObject", new object[] { holon }, out _);
            return true;
        }

        public bool HighlightBuildingOnMap(object building)
        {
            if (!IsInitialized || building == null) return false;

            HighlightedBuilding = building;

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.HighlightBuilding)
            {
                Target = building,
                Location = ExtractLocation(building)
            });

            command.AppliedToLiveMap = Bridge.TryInvoke("highlightBuilding", new object[] { building }, out _);
            return true;
        }

        public bool ZoomToHolonOnMap(object holon)
        {
            if (!IsInitialized || holon == null) return false;

            Geolocation location = ExtractLocation(holon);
            if (location == null) return false;

            lock (_cameraLock)
            {
                Camera.Centre = location;
            }

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.ZoomToLocation)
            {
                Target = holon,
                Location = location
            }.With("zoom", Camera.Zoom));

            object coordinate = Bridge.CreateCoordinate(location.Latitude, location.Longitude);
            if (coordinate != null)
                command.AppliedToLiveMap = Bridge.TryInvoke("centerMap", new object[] { coordinate }, out _);

            return true;
        }

        private bool Place(GOMapPlacementKind kind, object entity, double latitude, double longitude, string liveMethod)
        {
            if (!IsInitialized || entity == null) return false;

            Geolocation location = new Geolocation(latitude, longitude);
            if (!GeoMath.IsValid(location)) return false;

            GOMapPlacement placement = new GOMapPlacement(kind, entity, location);
            _placements[placement.Id] = placement;

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.PlaceEntity)
            {
                Target = entity,
                Location = location
            }
            .With("kind", kind)
            .With("placementId", placement.Id));

            object coordinate = Bridge.CreateCoordinate(latitude, longitude);
            if (coordinate != null)
                command.AppliedToLiveMap = Bridge.TryInvoke(liveMethod, new object[] { entity, coordinate }, out _);

            return true;
        }
    }
}
