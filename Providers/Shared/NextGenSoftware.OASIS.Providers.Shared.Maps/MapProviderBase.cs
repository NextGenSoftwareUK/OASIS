using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Routing;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps
{
    /// <summary>
    /// Complete <see cref="IOASISMapProvider"/> over a real routing service and a real
    /// place-search service. A server cannot draw on a client's screen, so the provider
    /// owns the authoritative map session - camera, pins, placements and routes - and
    /// expresses every visual change as a <see cref="MapCommand"/>. Commands go to a
    /// live renderer bound through <see cref="Initialize"/>, or are queued for a client
    /// to drain. State changes only once its command has been dispatched, and a failed
    /// operation returns false with the cause in <see cref="LastError"/>.
    /// </summary>
    public abstract partial class MapProviderBase : IOASISMapProvider, IOASISRoutingProvider
    {
        private readonly object _sync = new object();
        private readonly ConcurrentDictionary<Guid, MapPin> _pins = new ConcurrentDictionary<Guid, MapPin>();
        private readonly ConcurrentDictionary<Guid, MapPlacement> _placements = new ConcurrentDictionary<Guid, MapPlacement>();
        private readonly ConcurrentDictionary<Guid, MapRoute> _routes = new ConcurrentDictionary<Guid, MapRoute>();
        private readonly ConcurrentQueue<MapCommand> _queue = new ConcurrentQueue<MapCommand>();
        private readonly TaskCompletionSource<bool> _originSet =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly MapCameraState _camera = new MapCameraState();
        private IMapCommandSink _sink;

        protected MapProviderBase(MapProviderType mapProviderType, string name, string description,
            IMapRoutingService routing, IMapPlaceSearchService placeSearch)
        {
            MapProviderType = mapProviderType;
            MapProviderName = name ?? throw new ArgumentNullException(nameof(name));
            MapProviderDescription = description ?? throw new ArgumentNullException(nameof(description));
            Routing = routing ?? throw new ArgumentNullException(nameof(routing));
            PlaceSearch = placeSearch ?? throw new ArgumentNullException(nameof(placeSearch));
        }

        public MapProviderType MapProviderType { get; set; }
        public string MapProviderName { get; set; }
        public string MapProviderDescription { get; set; }
        public bool IsInitialized { get; set; }

        public IMapRoutingService Routing { get; }
        public IMapPlaceSearchService PlaceSearch { get; }
        public IDirectionsAPIProvider DirectionsAPI => Routing;
        public IForwardGeocodingProvider GeocodingProvider => PlaceSearch;

        /// <summary>Routing mode used by the contract's route methods, which take none.</summary>
        public RoutingType DefaultRoutingType { get; set; } = RoutingType.Walking;

        /// <summary>The cause of the most recent failed operation, cleared by the next success.</summary>
        public string LastError { get; private set; }

        /// <summary>The coordinate the client's world origin corresponds to.</summary>
        public Geolocation Origin { get; private set; }

        public Geolocation CurrentLocation { get; private set; }

        public bool IsRendererBound => _sink != null;

        public MapCameraState Camera { get { lock (_sync) return _camera.Clone(); } }
        public IReadOnlyCollection<MapPin> Pins => _pins.Values.ToList();
        public IReadOnlyCollection<MapPlacement> Placements => _placements.Values.ToList();
        public IReadOnlyCollection<MapRoute> Routes => _routes.Values.ToList();
        public int PendingCommandCount => _queue.Count;

        /// <summary>
        /// Starts the session. Pass null to run headless (commands are queued for
        /// <see cref="DrainCommands"/>), or an <see cref="IMapCommandSink"/> to apply
        /// commands to a live renderer as they are produced.
        /// </summary>
        public void Initialize(object mapInstance)
        {
            if (mapInstance != null && !(mapInstance is IMapCommandSink))
                throw new ArgumentException("The map instance must implement " + nameof(IMapCommandSink) + ".", nameof(mapInstance));

            _sink = (IMapCommandSink)mapInstance;
            IsInitialized = true;
            LastError = null;
        }

        public void Shutdown()
        {
            _sink = null;
            IsInitialized = false;
            OnShutdown();
        }

        /// <summary>Called after the session ends, so a provider can release its own renderer binding.</summary>
        protected virtual void OnShutdown()
        {
        }

        public void SetOrigin(Geolocation origin)
        {
            if (!Geo.GeoMath.IsValid(origin)) throw new ArgumentException("The origin must be a valid WGS84 coordinate.", nameof(origin));

            lock (_sync)
            {
                Origin = new Geolocation(origin.Latitude, origin.Longitude);
                if (_camera.Centre.Latitude == 0.0 && _camera.Centre.Longitude == 0.0)
                    _camera.Centre = new Geolocation(origin.Latitude, origin.Longitude);
            }

            _originSet.TrySetResult(true);
        }

        /// <summary>Records the device location reported by the client; the first one also sets the origin.</summary>
        public void SetCurrentLocation(Geolocation location)
        {
            if (!Geo.GeoMath.IsValid(location)) throw new ArgumentException("The location must be a valid WGS84 coordinate.", nameof(location));
            CurrentLocation = new Geolocation(location.Latitude, location.Longitude);
            if (Origin == null) SetOrigin(location);
        }

        /// <summary>Removes and returns every queued command, oldest first.</summary>
        public IList<MapCommand> DrainCommands()
        {
            var drained = new List<MapCommand>();
            while (_queue.TryDequeue(out MapCommand command)) drained.Add(command);
            return drained;
        }

        protected bool Fail(string message)
        {
            LastError = message;
            return false;
        }

        private bool Ready()
        {
            if (IsInitialized) return true;
            return Fail(MapProviderName + " has not been initialised; call Initialize first.");
        }

        /// <summary>Hands the command to the live renderer, or queues it when headless.</summary>
        private bool Dispatch(MapCommand command)
        {
            IMapCommandSink sink = _sink;

            if (sink == null)
            {
                _queue.Enqueue(command);
            }
            else
            {
                if (!sink.Apply(command)) return Fail("The renderer rejected the " + command.CommandType + " command.");
                command.AppliedToRenderer = true;
            }

            LastError = null;
            return true;
        }
    }
}
