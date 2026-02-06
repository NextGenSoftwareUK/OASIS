using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Objects.Game
{
    /// <summary>
    /// Represents an active game session with state and settings
    /// </summary>
    public class GameSession : Holon
    {
        public Guid GameId { get; set; }
        public Guid AvatarId { get; set; }
        public GameState State { get; set; }
        public string CurrentLevel { get; set; }
        public Guid? CurrentAreaId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public double MasterVolume { get; set; } = 1.0;
        public double VoiceVolume { get; set; } = 1.0;
        public double SoundVolume { get; set; } = 1.0;
        public VideoSetting VideoSetting { get; set; } = VideoSetting.Medium;
        public Dictionary<string, string> KeyBindings { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, object> GameData { get; set; } = new Dictionary<string, object>();
    }
}
