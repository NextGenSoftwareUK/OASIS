using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class GameManager : STARNETManagerBase<Game, DownloadedGame, InstalledGame, STARNETDNA>, IGameManager
    {
        private NFTManager _nftManager = null;
        private KarmaManager _karmaManager = null;
        private InventoryItemManager _inventoryManager = null;

        public GameManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(GameType),
            HolonType.Game,
            HolonType.InstalledGame,
            "Game",
            "STARNETHolonId",
            "GameName",
            "GameType",
            "ogame",
            "oasis_games",
            "GameDNA.json",
            "GameDNAJSON")
        { }

        public GameManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(GameType),
            HolonType.Game,
            HolonType.InstalledGame,
            "Game",
            "STARNETHolonId",
            "GameName",
            "GameType",
            "ogame",
            "oasis_games",
            "GameDNA.json",
            "GameDNAJSON")
        { }

        private QuestManager QuestManager
        {
            get
            {
                if (_questManager == null && AvatarId != Guid.Empty)
                    _questManager = new QuestManager(OASISStorageProvider, AvatarId, STARDNA, OASISDNA);
                return _questManager;
            }
        }

        private NFTManager NFTManager
        {
            get
            {
                if (_nftManager == null && AvatarId != Guid.Empty)
                    _nftManager = new NFTManager(AvatarId, OASISDNA);
                return _nftManager;
            }
        }

        private KarmaManager KarmaManager
        {
            get
            {
                if (_karmaManager == null)
                    _karmaManager = new KarmaManager(OASISStorageProvider, OASISDNA);
                return _karmaManager;
            }
        }

        private InventoryItemManager InventoryManager
        {
            get
            {
                if (_inventoryManager == null && AvatarId != Guid.Empty)
                    _inventoryManager = new InventoryItemManager(OASISStorageProvider, AvatarId, STARDNA, OASISDNA);
                return _inventoryManager;
            }
        }


        /// <summary>
        /// Starts a new game session
        /// </summary>
        public async Task<OASISResult<GameSession>> StartGameAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<GameSession>();
            try
            {
                // Check if game is already running
                var existingSession = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId && s.State == GameState.Running);
                if (existingSession != null)
                {
                    result.Result = existingSession;
                    result.Message = "Game session already running";
                    return result;
                }

                // Load the game holon to verify it exists
                var gameResult = await LoadAsync(avatarId, gameId);
                if (gameResult.IsError || gameResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"Game not found: {gameResult.Message}";
                    return result;
                }

                // Create new game session
                var session = new GameSession
                {
                    Id = Guid.NewGuid(),
                    GameId = gameId,
                    AvatarId = avatarId,
                    State = GameState.Starting,
                    StartedAt = DateTime.UtcNow,
                    MasterVolume = 1.0,
                    VoiceVolume = 1.0,
                    SoundVolume = 1.0,
                    VideoSetting = VideoSetting.Medium
                };

                // Load avatar's game settings if available
                var settingsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "game");
                if (!settingsResult.IsError && settingsResult.Result != null)
                {
                    var settings = settingsResult.Result;
                    if (settings.ContainsKey("masterVolume"))
                        session.MasterVolume = Convert.ToDouble(settings["masterVolume"]);
                    if (settings.ContainsKey("voiceVolume"))
                        session.VoiceVolume = Convert.ToDouble(settings["voiceVolume"]);
                    if (settings.ContainsKey("soundVolume"))
                        session.SoundVolume = Convert.ToDouble(settings["soundVolume"]);
                    if (settings.ContainsKey("videoSetting"))
                        Enum.TryParse<VideoSetting>(settings["videoSetting"].ToString(), out var videoSetting);
                }

                // Save session to storage
                var holon = new Holon
                {
                    Id = session.Id,
                    HolonType = HolonType.GameSession, //TODO: Fix everywhere, all holons ALWAYS NEED A HolonType set, otherwise it causes all sorts of issues with loading, searching, etc. as it defaults to HolonType.Default which is not correct for any holon
                    Name = $"Game Session {gameId}",
                    Description = $"Active game session for avatar {avatarId}",
                    CreatedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>
                    {
                        { "GameId", gameId.ToString() },
                        { "AvatarId", avatarId.ToString() },
                        { "State", GameState.Starting.ToString() },
                        { "StartedAt", session.StartedAt.ToString() }
                    }
                };

                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to save game session: {saveResult.Message}";
                    return result;
                }

                session.State = GameState.Running;
                _activeSessions[session.Id] = session;

                result.Result = session;
                result.Message = "Game started successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error starting game: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Ends a game session
        /// </summary>
        public async Task<OASISResult<bool>> EndGameAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId && s.State == GameState.Running);
                if (session == null)
                {
                    result.IsError = true;
                    result.Message = "No active game session found";
                    return result;
                }

                session.State = GameState.Ended;
                session.EndedAt = DateTime.UtcNow;

                // Save final session state
                var holonResult = await HolonManager.Instance.LoadHolonAsync(session.Id);
                if (!holonResult.IsError && holonResult.Result != null)
                {
                    var holon = holonResult.Result;
                    holon.MetaData["State"] = GameState.Ended.ToString();
                    holon.MetaData["EndedAt"] = session.EndedAt.ToString();
                    await HolonManager.Instance.SaveHolonAsync(holon);
                }

                // Unload all areas and levels
                var areasToUnload = _loadedAreas.Values.Where(a => a.GameId == gameId && a.AvatarId == avatarId).ToList();
                foreach (var area in areasToUnload)
                {
                    await UnloadAreaAsync(gameId, area.Id);
                }

                if (_loadedLevels.ContainsKey(gameId))
                    _loadedLevels[gameId].Clear();

                _activeSessions.Remove(session.Id);

                result.Result = true;
                result.Message = "Game ended successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error ending game: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Loads a game into memory
        /// </summary>
        public async Task<OASISResult<bool>> LoadGameAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Check if game session exists
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session == null)
                {
                    // Start game if not already started
                    var startResult = await StartGameAsync(gameId, avatarId);
                    if (startResult.IsError)
                    {
                        result.IsError = true;
                        result.Message = $"Failed to start game: {startResult.Message}";
                        return result;
                    }
                    session = startResult.Result;
                }

                session.State = GameState.Loading;

                // Load game data, assets, etc.
                // This would typically load game configuration, assets, etc.

                session.State = GameState.Running;

                result.Result = true;
                result.Message = "Game loaded successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error loading game: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Unloads a game from memory
        /// </summary>
        public async Task<OASISResult<bool>> UnloadGameAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session == null)
                {
                    result.IsError = true;
                    result.Message = "No active game session found";
                    return result;
                }

                session.State = GameState.Unloading;

                // Unload all areas
                var areasToUnload = _loadedAreas.Values.Where(a => a.GameId == gameId && a.AvatarId == avatarId).ToList();
                foreach (var area in areasToUnload)
                {
                    await UnloadAreaAsync(gameId, area.Id);
                }

                // Unload all levels
                if (_loadedLevels.ContainsKey(gameId))
                    _loadedLevels[gameId].Clear();

                session.State = GameState.NotStarted;

                result.Result = true;
                result.Message = "Game unloaded successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error unloading game: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }



        /// <summary>
        /// Loads a specific level in a game
        /// </summary>
        public async Task<OASISResult<bool>> LoadLevelAsync(Guid gameId, string level, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_loadedLevels.ContainsKey(gameId))
                    _loadedLevels[gameId] = new Dictionary<string, bool>();

                _loadedLevels[gameId][level] = true;

                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.CurrentLevel = level;
                }

                result.Result = true;
                result.Message = $"Level '{level}' loaded successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error loading level: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Unloads a specific level
        /// </summary>
        public async Task<OASISResult<bool>> UnloadLevelAsync(Guid gameId, string level)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_loadedLevels.ContainsKey(gameId) && _loadedLevels[gameId].ContainsKey(level))
                {
                    _loadedLevels[gameId].Remove(level);
                }

                result.Result = true;
                result.Message = $"Level '{level}' unloaded successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error unloading level: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Jumps to a specific level
        /// </summary>
        public async Task<OASISResult<bool>> JumpToLevelAsync(Guid gameId, string level, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Unload current level if different
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null && session.CurrentLevel != level)
                {
                    if (!string.IsNullOrEmpty(session.CurrentLevel))
                        await UnloadLevelAsync(gameId, session.CurrentLevel);
                }

                // Load new level
                var loadResult = await LoadLevelAsync(gameId, level, avatarId);
                if (loadResult.IsError)
                {
                    result.IsError = true;
                    result.Message = loadResult.Message;
                    return result;
                }

                result.Result = true;
                result.Message = $"Jumped to level '{level}' successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error jumping to level: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Jumps to a specific point in a level
        /// </summary>
        public async Task<OASISResult<bool>> JumpToPointInLevelAsync(Guid gameId, string level, double x, double y, double z, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // First jump to the level
                var jumpResult = await JumpToLevelAsync(gameId, level, avatarId);
                if (jumpResult.IsError)
                {
                    result.IsError = true;
                    result.Message = jumpResult.Message;
                    return result;
                }

                // Save position to session
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.GameData["PositionX"] = x;
                    session.GameData["PositionY"] = y;
                    session.GameData["PositionZ"] = z;
                }

                result.Result = true;
                result.Message = $"Jumped to point ({x}, {y}, {z}) in level '{level}' successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error jumping to point: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

    }
}
