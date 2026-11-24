using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps
{
    public abstract partial class MapProviderBase
    {
        public object SelectedEntity { get; private set; }
        public object HighlightedBuilding { get; private set; }

        public bool PlaceGeoNFTOnMap(object geoNFT, double latitude, double longitude) => Place(MapPlacementKind.GeoNFT, geoNFT, latitude, longitude);
        public bool PlaceGeoHotSpotOnMap(object geoHotSpot, double latitude, double longitude) => Place(MapPlacementKind.GeoHotSpot, geoHotSpot, latitude, longitude);
        public bool PlaceQuestOnMap(object quest, double latitude, double longitude) => Place(MapPlacementKind.Quest, quest, latitude, longitude);
        public bool PlaceOAPPOnMap(object oapp, double latitude, double longitude) => Place(MapPlacementKind.OAPP, oapp, latitude, longitude);
        public bool PlaceHolonOnMap(object holon, double latitude, double longitude) => Place(MapPlacementKind.Holon, holon, latitude, longitude);
        public bool PlaceBuildingOnMap(object building, double latitude, double longitude) => Place(MapPlacementKind.Building, building, latitude, longitude);

        public IReadOnlyCollection<MapPlacement> GetPlacements(MapPlacementKind kind) =>
            _placements.Values.Where(p => p.Kind == kind).ToList();

        public IReadOnlyCollection<MapPlacement> GetPlacementsNear(Geolocation location, double radiusMetres)
        {
            if (!GeoMath.IsValid(location)) throw new ArgumentException("A valid WGS84 coordinate is required.", nameof(location));
            if (radiusMetres <= 0) throw new ArgumentOutOfRangeException(nameof(radiusMetres));

            return _placements.Values
                .Select(p => new { Placement = p, Distance = GeoMath.HaversineMetres(location, p.Location) })
                .Where(x => x.Distance <= radiusMetres)
                .OrderBy(x => x.Distance)
                .Select(x => x.Placement)
                .ToList();
        }

        public bool RemovePlacement(Guid placementId)
        {
            if (!Ready()) return false;
            if (!_placements.TryGetValue(placementId, out MapPlacement placement)) return Fail("No placement has id " + placementId + ".");

            var command = new MapCommand(MapCommandType.RemovePlacement) { Target = placement.Entity, Location = placement.Location }
                .With("placementId", placementId);
            if (!Dispatch(command)) return false;

            _placements.TryRemove(placementId, out _);
            return true;
        }

        public bool SelectHolonOnMap(object holon)
        {
            if (!Ready()) return false;
            if (holon == null) return Fail("A holon is required.");

            if (!Dispatch(new MapCommand(MapCommandType.SelectEntity) { Target = holon, Location = MapEntityLocator.Locate(holon) }))
                return false;
            SelectedEntity = holon;
            return true;
        }

        public bool HighlightBuildingOnMap(object building)
        {
            if (!Ready()) return false;
            if (building == null) return Fail("A building is required.");

            if (!Dispatch(new MapCommand(MapCommandType.HighlightBuilding) { Target = building, Location = MapEntityLocator.Locate(building) }))
                return false;
            HighlightedBuilding = building;
            return true;
        }

        /// <summary>Centres the map on the entity's own coordinate.</summary>
        public bool ZoomToHolonOnMap(object holon)
        {
            if (!Ready()) return false;
            Geolocation location = MapEntityLocator.Locate(holon);
            if (location == null) return Fail("The entity does not carry a valid latitude and longitude.");
            return CentreMapOn(location);
        }

        public bool DropPin(Geolocation coordinates, object gameObject)
        {
            if (!Ready()) return false;
            if (!GeoMath.IsValid(coordinates)) return Fail("A pin needs a valid WGS84 coordinate.");

            var pin = new MapPin(new Geolocation(coordinates.Latitude, coordinates.Longitude), gameObject);
            if (!Dispatch(new MapCommand(MapCommandType.DropPin) { Target = gameObject, Location = pin.Location }.With("pinId", pin.Id)))
                return false;

            _pins[pin.Id] = pin;
            return true;
        }

        /// <summary>Removes every pin tied to the object.</summary>
        public bool RemovePin(object gameObject)
        {
            if (!Ready()) return false;
            if (gameObject == null) return Fail("The pinned object is required.");

            List<MapPin> matches = _pins.Values.Where(p => Equals(p.Target, gameObject)).ToList();
            if (matches.Count == 0) return Fail("No pin is attached to that object.");

            var command = new MapCommand(MapCommandType.RemovePin) { Target = gameObject, Location = matches[0].Location }
                .With("pinIds", matches.Select(p => p.Id).ToList());
            if (!Dispatch(command)) return false;

            foreach (MapPin pin in matches) _pins.TryRemove(pin.Id, out _);
            return true;
        }

        private bool Place(MapPlacementKind kind, object entity, double latitude, double longitude)
        {
            if (!Ready()) return false;
            if (entity == null) return Fail("Something to place is required.");

            var location = new Geolocation(latitude, longitude);
            if (!GeoMath.IsValid(location)) return Fail("Placements need a valid WGS84 coordinate.");

            var placement = new MapPlacement(kind, entity, location);
            var command = new MapCommand(MapCommandType.PlaceEntity) { Target = entity, Location = location }
                .With("kind", kind)
                .With("placementId", placement.Id);
            if (!Dispatch(command)) return false;

            _placements[placement.Id] = placement;
            return true;
        }
    }
}
