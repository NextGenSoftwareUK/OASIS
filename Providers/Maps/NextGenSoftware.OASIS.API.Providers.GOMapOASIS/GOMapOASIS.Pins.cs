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
    /// Pin management. Pins are tracked by the object they represent as well as by
    /// id, because <see cref="RemovePin"/> is given the object rather than an id.
    /// </summary>
    public partial class GOMapOASIS
    {
        public bool DropPin(Geolocation coordinates, object gameObject)
        {
            if (!IsInitialized || !GeoMath.IsValid(coordinates)) return false;

            GOMapPin pin = new GOMapPin(coordinates, gameObject);
            _pins[pin.Id] = pin;

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.DropPin)
            {
                Target = gameObject,
                Location = coordinates
            }.With("pinId", pin.Id));

            object coordinate = Bridge.CreateCoordinate(coordinates.Latitude, coordinates.Longitude);
            if (coordinate != null)
                command.AppliedToLiveMap = Bridge.TryInvoke("dropPin", new object[] { coordinate, gameObject }, out _);

            return true;
        }

        public bool RemovePin(object gameObject)
        {
            if (!IsInitialized) return false;

            List<GOMapPin> matches = _pins.Values
                .Where(p => ReferenceEquals(p.Target, gameObject)
                            || (p.Target != null && p.Target.Equals(gameObject)))
                .ToList();

            if (matches.Count == 0) return false;

            foreach (GOMapPin pin in matches)
                _pins.TryRemove(pin.Id, out _);

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.RemovePin)
            {
                Target = gameObject,
                Location = matches[0].Location
            }.With("pinIds", matches.Select(p => p.Id).ToList()));

            command.AppliedToLiveMap = Bridge.TryInvoke("removePin", new object[] { gameObject }, out _);
            return true;
        }

        /// <summary>Removes a pin by the id handed back in the drop command.</summary>
        public bool RemovePin(Guid pinId)
        {
            if (!IsInitialized) return false;
            if (!_pins.TryRemove(pinId, out GOMapPin pin)) return false;

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.RemovePin)
            {
                Target = pin.Target,
                Location = pin.Location
            }.With("pinIds", new List<Guid> { pinId }));

            command.AppliedToLiveMap = Bridge.TryInvoke("removePin", new object[] { pin.Target }, out _);
            return true;
        }

        /// <summary>Every pin within a radius of a coordinate, nearest first.</summary>
        public IReadOnlyCollection<GOMapPin> GetPinsNear(Geolocation location, double radiusMetres)
        {
            if (!GeoMath.IsValid(location) || radiusMetres <= 0.0)
                return new List<GOMapPin>();

            return _pins.Values
                .Where(p => p.Location != null
                            && GeoMath.HaversineMetres(location, p.Location) <= radiusMetres)
                .OrderBy(p => GeoMath.HaversineMetres(location, p.Location))
                .ToList();
        }

        /// <summary>Drops every pin currently held from the map.</summary>
        public void ClearPins()
        {
            foreach (Guid id in _pins.Keys.ToList())
                RemovePin(id);
        }
    }
}
