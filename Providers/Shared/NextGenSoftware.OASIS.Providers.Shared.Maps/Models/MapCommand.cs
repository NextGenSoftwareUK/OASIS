using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Models
{
    /// <summary>
    /// One instruction for the client that renders the map. Server-side providers
    /// cannot draw on a client's screen, so every visual change is expressed as a
    /// command which a renderer applies, either live through an
    /// <see cref="IMapCommandSink"/> or by draining the provider's queue.
    /// </summary>
    public sealed class MapCommand
    {
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>(StringComparer.Ordinal);

        public MapCommand(MapCommandType commandType)
        {
            CommandType = commandType;
        }

        public Guid Id { get; } = Guid.NewGuid();
        public MapCommandType CommandType { get; }
        public DateTime CreatedUtc { get; } = DateTime.UtcNow;
        public Geolocation Location { get; set; }
        public object Target { get; set; }
        public IReadOnlyDictionary<string, object> Parameters => _parameters;

        /// <summary>True once a bound sink has accepted the command.</summary>
        public bool AppliedToRenderer { get; internal set; }

        public MapCommand With(string key, object value)
        {
            _parameters[key] = value;
            return this;
        }

        public override string ToString() => Location == null ? CommandType.ToString() : CommandType + " @ " + Location;
    }
}
