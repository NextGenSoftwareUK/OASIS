using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    /// <summary>
    /// Represents a game in the STARNET system
    /// Games can be created, published, downloaded, and installed like other STARNET holons
    /// </summary>
    public class Game : STARNETHolon, IGame, ISTARNETHolon
    {
        public Game() : base("GameDNAJSON")
        {
            this.HolonType = HolonType.Game;
        }

        [CustomOASISProperty()]
        public GameType GameType { get; set; }

        [CustomOASISProperty()]
        public string Version { get; set; }

        [CustomOASISProperty()]
        public string Developer { get; set; }

        [CustomOASISProperty()]
        public string Publisher { get; set; }

        [CustomOASISProperty()]
        public DateTime ReleaseDate { get; set; }

        [CustomOASISProperty()]
        public List<string> SupportedPlatforms { get; set; } = new List<string>();

        [CustomOASISProperty()]
        public bool SupportsCrossGameInterop { get; set; } = true;

        [CustomOASISProperty()]
        public Dictionary<string, object> GameSettings { get; set; } = new Dictionary<string, object>();

        // Game session management (stored in metadata for active sessions)
        // Note: ActiveSessions is not stored directly in the holon, but managed in-memory by GameManager
        // This property is for interface compliance but actual sessions are in GameManager
    }
}

