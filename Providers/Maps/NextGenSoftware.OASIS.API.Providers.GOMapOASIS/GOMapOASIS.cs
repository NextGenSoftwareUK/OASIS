using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Directions;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geocoding;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS
{
    /// <summary>
    /// GO Map OASIS Provider - places OASIS avatars, holons, quests, OAPPs, GeoNFTs
    /// and geo hot spots on real-world maps for Our World and One World.
    ///
    /// GO Map is a Unity Asset Store package, so its runtime types only exist inside
    /// a Unity player. This provider therefore does all the work it can do without
    /// Unity - the projection and geodesy maths, and the authoritative record of
    /// what is placed where - and expresses everything that must happen in the scene
    /// as a queue of <see cref="GOMapCommand"/> for a Unity client to drain.
    ///
    /// When it IS hosted inside Unity, <see cref="Initialize"/> is handed the real
    /// GOMap component and the same operations are additionally forwarded to it
    /// through <see cref="GOMapUnityBridge"/>, so one assembly serves both hosts.
    ///
    /// Directions and geocoding are genuine HTTP services (OSRM and Nominatim by
    /// default, both keyless), configurable to any compatible endpoint.
    /// </summary>
    public partial class GOMapOASIS : IOASISMapProvider
    {
        private readonly ConcurrentDictionary<Guid, GOMapPin> _pins =
            new ConcurrentDictionary<Guid, GOMapPin>();

        private readonly ConcurrentDictionary<Guid, GOMapPlacement> _placements =
            new ConcurrentDictionary<Guid, GOMapPlacement>();

        private readonly ConcurrentQueue<GOMapCommand> _commands =
            new ConcurrentQueue<GOMapCommand>();

        private readonly object _cameraLock = new object();

        private readonly OriginSignal _originSet = new OriginSignal();

        #region Core Properties

        public MapProviderType MapProviderType { get; set; }
        public string MapProviderName { get; set; }
        public string MapProviderDescription { get; set; }
        public bool IsInitialized { get; set; }

        #endregion

        #region Unity-Specific Functions

        public IDirectionsAPIProvider DirectionsAPI { get; set; }
        public IForwardGeocodingProvider GeocodingProvider { get; set; }

        #endregion

        #region GO Map State

        /// <summary>
        /// The live GO Map component, when hosted inside Unity. Null when headless -
        /// which is not an error, it is the server-side mode of operation.
        /// </summary>
        public object GOMapInstance => Bridge.Instance;

        /// <summary>Reflective dispatcher onto the live GO Map instance.</summary>
        public GOMapUnityBridge Bridge { get; }

        /// <summary>Where the map is currently centred, and at what zoom and orbit.</summary>
        public GOMapCameraState Camera { get; }

        /// <summary>
        /// The coordinate the Unity world origin corresponds to. GO Map measures its
        /// world positions from this point, so conversions are meaningless until it
        /// is set - which is what <see cref="WaitForOriginSet"/> waits for.
        /// </summary>
        public Geolocation Origin { get; private set; }

        /// <summary>Unity world units per projected metre. GO Map's default is 1:1.</summary>
        public double WorldUnitsPerMetre { get; set; } = 1.0;

        /// <summary>The device location last reported by the client or the GO Map location manager.</summary>
        public Geolocation CurrentLocation { get; private set; }

        public IReadOnlyCollection<GOMapPin> Pins => _pins.Values.ToList();

        public IReadOnlyCollection<GOMapPlacement> Placements => _placements.Values.ToList();

        /// <summary>Commands queued for a GO Map client, oldest first.</summary>
        public IReadOnlyCollection<GOMapCommand> PendingCommands => _commands.ToList();

        #endregion

        public GOMapOASIS() : this(null)
        {
        }

        /// <summary>
        /// Creates the provider and immediately binds it to a live GO Map instance.
        /// </summary>
        /// <param name="goMapInstance">The Unity GOMap component, or null for headless use.</param>
        public GOMapOASIS(object goMapInstance)
        {
            MapProviderType = MapProviderType.GoMap;
            MapProviderName = "GO Map";
            MapProviderDescription =
                "GO Map OASIS Provider - real-world geo-spatial and AR mapping for OASIS avatars, "
                + "holons, quests, OAPPs, GeoNFTs and geo hot spots.";

            Bridge = new GOMapUnityBridge();
            Camera = new GOMapCameraState();
            DirectionsAPI = new OSRMDirectionsProvider();
            GeocodingProvider = new NominatimGeocodingProvider();

            if (goMapInstance != null)
                Initialize(goMapInstance);
        }

        #region Initialization

        /// <summary>
        /// Binds the provider to a GO Map instance. Passing null initialises it in
        /// headless mode, where every operation still records state and queues
        /// commands but nothing is forwarded to Unity.
        /// </summary>
        public void Initialize(object mapInstance)
        {
            if (mapInstance != null)
            {
                Bridge.Bind(mapInstance);
                ReadOriginFromLiveMap();
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Sets the coordinate the Unity world origin corresponds to and releases
        /// anything awaiting <see cref="WaitForOriginSet"/>.
        /// </summary>
        public void SetOrigin(Geolocation origin)
        {
            if (origin == null) throw new ArgumentNullException(nameof(origin));

            Origin = origin;

            lock (_cameraLock)
            {
                if (Camera.Centre == null || (Camera.Centre.Latitude == 0.0 && Camera.Centre.Longitude == 0.0))
                    Camera.Centre = new Geolocation(origin.Latitude, origin.Longitude);
            }

            _originSet.Complete();
        }

        /// <summary>Records the device location reported by the client.</summary>
        public void SetCurrentLocation(Geolocation location)
        {
            CurrentLocation = location;

            if (Origin == null && location != null)
                SetOrigin(new Geolocation(location.Latitude, location.Longitude));
        }

        /// <summary>Releases the GO Map instance and returns the provider to headless mode.</summary>
        public void Shutdown()
        {
            Bridge.Unbind();
            IsInitialized = false;
        }

        private void ReadOriginFromLiveMap()
        {
            double latitude = Bridge.GetValueOrDefault("locationManager.currentLocation.latitude", double.NaN);
            double longitude = Bridge.GetValueOrDefault("locationManager.currentLocation.longitude", double.NaN);

            if (double.IsNaN(latitude) || double.IsNaN(longitude)) return;

            Geolocation location = new Geolocation(latitude, longitude);
            CurrentLocation = location;
            SetOrigin(new Geolocation(latitude, longitude));
        }

        #endregion

        #region Command Queue

        /// <summary>
        /// Removes and returns everything queued. A Unity client calls this each
        /// frame and applies the commands on its main thread.
        /// </summary>
        public IList<GOMapCommand> DrainCommands()
        {
            List<GOMapCommand> drained = new List<GOMapCommand>();

            while (_commands.TryDequeue(out GOMapCommand command))
                drained.Add(command);

            return drained;
        }

        /// <summary>Discards every queued command without applying it.</summary>
        public void ClearCommands()
        {
            while (_commands.TryDequeue(out _)) { }
        }

        private GOMapCommand Enqueue(GOMapCommand command)
        {
            _commands.Enqueue(command);
            return command;
        }

        #endregion

    }
}
