using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects.Game
{
    /// <summary>
    /// Represents an active game session with state and settings
    /// </summary>
    public class GameSession : IHolon
    {
        public Guid Id { get; set; }
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
        
        // IHolon implementation
        public int Version { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime DeletedDate { get; set; }
        public int VersionId { get; set; }
        public bool IsActive1 { get; set; }
        public string PreviousVersionId { get; set; }
        public string ProviderMetaData { get; set; }
        public string MetaData { get; set; }
        public string MetaData2 { get; set; }
        public HolonType HolonType { get; set; }
    }
}

