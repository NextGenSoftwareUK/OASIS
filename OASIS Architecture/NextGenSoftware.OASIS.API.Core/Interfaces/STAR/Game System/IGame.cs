using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    /// <summary>
    /// Interface for Game holons in the STARNET system
    /// </summary>
    public interface IGame : ISTARNETHolon
    {
        GameType GameType { get; set; }
        //string Version { get; set; }
        string Developer { get; set; } //TODO: May add to OAPP also.
        string Publisher { get; set; } //TODO: May add to OAPP also.
        DateTime ReleaseDate { get; set; } //TODO: May add to OAPP also.
        List<string> SupportedPlatforms { get; set; } //TODO: May add to OAPP also.
        bool SupportsCrossGameInterop { get; set; }
        Dictionary<string, object> GameSettings { get; set; }
    }
}

