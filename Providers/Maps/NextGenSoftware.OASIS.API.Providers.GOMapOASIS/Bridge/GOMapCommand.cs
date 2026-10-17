using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge
{
    /// <summary>
    /// One instruction for a GO Map client to apply to its scene.
    ///
    /// The provider runs on the server (or in a headless test), while GO Map itself
    /// only exists inside Unity. Rather than pretend a draw call succeeded, every
    /// scene-affecting operation is recorded as a command and queued. A Unity client
    /// drains the queue on its main thread and applies each one; when the provider
    /// is bound directly to a live GO Map instance the same command is also applied
    /// immediately through the reflective bridge.
    /// </summary>
    public class GOMapCommand
    {
        public Guid Id { get; set; }
        public GOMapCommandType CommandType { get; set; }
        public Geolocation Location { get; set; }

        /// <summary>The Unity-side object this command acts on, when there is one.</summary>
        public object Target { get; set; }

        public IDictionary<string, object> Parameters { get; }
        public DateTime CreatedAt { get; set; }

        /// <summary>True when the command was also forwarded to a live GO Map instance.</summary>
        public bool AppliedToLiveMap { get; set; }

        public GOMapCommand()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            Parameters = new Dictionary<string, object>();
        }

        public GOMapCommand(GOMapCommandType commandType) : this()
        {
            CommandType = commandType;
        }

        public GOMapCommand With(string key, object value)
        {
            Parameters[key] = value;
            return this;
        }

        public override string ToString()
        {
            return Location != null
                ? $"{CommandType} @ {Location}"
                : CommandType.ToString();
        }
    }
}
