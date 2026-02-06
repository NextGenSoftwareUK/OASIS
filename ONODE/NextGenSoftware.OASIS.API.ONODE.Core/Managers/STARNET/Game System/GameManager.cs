using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using Holon = NextGenSoftware.OASIS.API.Core.Holons.Holon;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    /// <summary>
    /// Manages games in the STARNET system
    /// Extends STARNETManagerBase to enable create, search, edit, publish, download, install functionality
    /// Also manages game sessions, levels, areas, UI, audio, video, and input
    /// Enables cross-game interoperability with shared assets, karma, NFTs, and quests
    /// </summary>
    public partial class GameManager : STARNETManagerBase<Game, DownloadedGame, InstalledGame, STARNETDNA>, IGameManager
    {
        // Game session management (in-memory for active sessions)
        private readonly Dictionary<Guid, GameSession> _activeSessions = new Dictionary<Guid, GameSession>();
        private readonly Dictionary<Guid, GameArea> _loadedAreas = new Dictionary<Guid, GameArea>();
        private readonly Dictionary<Guid, Dictionary<string, bool>> _loadedLevels = new Dictionary<Guid, Dictionary<string, bool>>();

        // Manager dependencies for cross-game interoperability
        private QuestManager _questManager = null;
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
                    _questManager = new QuestManager(ProviderManager.Instance.CurrentStorageProvider, AvatarId, STARDNA, OASISDNA);
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
                    _karmaManager = new KarmaManager(ProviderManager.Instance.CurrentStorageProvider, OASISDNA);
                return _karmaManager;
            }
        }

        private InventoryItemManager InventoryManager
        {
            get
            {
                if (_inventoryManager == null && AvatarId != Guid.Empty)
                    _inventoryManager = new InventoryItemManager(ProviderManager.Instance.CurrentStorageProvider, AvatarId, STARDNA, OASISDNA);
                return _inventoryManager;
            }
        }

        #region Game Lifecycle (Session Management)

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

        #endregion

        #region Level Management

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

        #endregion

        #region Area Management

        /// <summary>
        /// Loads an area around a specific point
        /// </summary>
        public async Task<OASISResult<Guid>> LoadAreaAsync(Guid gameId, double x, double y, double z, double radius, Guid avatarId)
        {
            var result = new OASISResult<Guid>();
            try
            {
                var area = new GameArea
                {
                    Id = Guid.NewGuid(),
                    GameId = gameId,
                    AvatarId = avatarId,
                    X = x,
                    Y = y,
                    Z = z,
                    Radius = radius,
                    LoadedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _loadedAreas[area.Id] = area;

                // Save area to storage
                var holon = new Holon
                {
                    Id = area.Id,
                    Name = $"Game Area {area.Id}",
                    Description = $"Area at ({x}, {y}, {z}) with radius {radius}",
                    CreatedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>
                    {
                        { "GameId", gameId.ToString() },
                        { "AvatarId", avatarId.ToString() },
                        { "X", x },
                        { "Y", y },
                        { "Z", z },
                        { "Radius", radius },
                        { "LoadedAt", area.LoadedAt.ToString() }
                    }
                };

                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to save area: {saveResult.Message}";
                    return result;
                }

                result.Result = area.Id;
                result.Message = "Area loaded successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error loading area: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Unloads an area
        /// </summary>
        public async Task<OASISResult<bool>> UnloadAreaAsync(Guid gameId, Guid areaId)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_loadedAreas.ContainsKey(areaId))
                {
                    var area = _loadedAreas[areaId];
                    area.IsActive = false;
                    _loadedAreas.Remove(areaId);
                }

                result.Result = true;
                result.Message = "Area unloaded successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error unloading area: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Jumps to a specific area
        /// </summary>
        public async Task<OASISResult<Guid>> JumpToAreaAsync(Guid gameId, double x, double y, double z, Guid avatarId, double radius = 100.0)
        {
            var result = new OASISResult<Guid>();
            try
            {
                // Load area at the specified point
                var loadResult = await LoadAreaAsync(gameId, x, y, z, radius, avatarId);
                if (loadResult.IsError)
                {
                    result.IsError = true;
                    result.Message = loadResult.Message;
                    return result;
                }

                // Update session position
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.CurrentAreaId = loadResult.Result;
                    session.GameData["PositionX"] = x;
                    session.GameData["PositionY"] = y;
                    session.GameData["PositionZ"] = z;
                }

                result.Result = loadResult.Result;
                result.Message = $"Jumped to area at ({x}, {y}, {z}) successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error jumping to area: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        #endregion

        #region UI Management

        /// <summary>
        /// Shows the title screen
        /// </summary>
        public async Task<OASISResult<bool>> ShowTitleScreenAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.GameData["CurrentScreen"] = "TitleScreen";
                }

                result.Result = true;
                result.Message = "Title screen displayed";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error showing title screen: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Shows the main menu
        /// </summary>
        public async Task<OASISResult<bool>> ShowMainMenuAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.GameData["CurrentScreen"] = "MainMenu";
                }

                result.Result = true;
                result.Message = "Main menu displayed";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error showing main menu: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Shows the options menu
        /// </summary>
        public async Task<OASISResult<bool>> ShowOptionsAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.GameData["CurrentScreen"] = "Options";
                }

                result.Result = true;
                result.Message = "Options menu displayed";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error showing options: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Shows the credits screen
        /// </summary>
        public async Task<OASISResult<bool>> ShowCreditsAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.GameData["CurrentScreen"] = "Credits";
                }

                result.Result = true;
                result.Message = "Credits displayed";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error showing credits: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        #endregion

        #region Audio Settings

        /// <summary>
        /// Sets the master volume
        /// </summary>
        public async Task<OASISResult<bool>> SetMasterVolumeAsync(Guid gameId, Guid avatarId, double volume)
        {
            var result = new OASISResult<bool>();
            try
            {
                volume = Math.Max(0.0, Math.Min(1.0, volume)); // Clamp between 0 and 1

                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.MasterVolume = volume;
                }

                // Save to avatar settings
                await HolonManager.Instance.SaveSettingsAsync(avatarId, "game", new Dictionary<string, object> { { "masterVolume", volume } });

                result.Result = true;
                result.Message = $"Master volume set to {volume * 100}%";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error setting master volume: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Sets the voice volume
        /// </summary>
        public async Task<OASISResult<bool>> SetVoiceVolumeAsync(Guid gameId, Guid avatarId, double volume)
        {
            var result = new OASISResult<bool>();
            try
            {
                volume = Math.Max(0.0, Math.Min(1.0, volume));

                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.VoiceVolume = volume;
                }

                await HolonManager.Instance.SaveSettingsAsync(avatarId, "game", new Dictionary<string, object> { { "voiceVolume", volume } });

                result.Result = true;
                result.Message = $"Voice volume set to {volume * 100}%";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error setting voice volume: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Sets the sound volume
        /// </summary>
        public async Task<OASISResult<bool>> SetSoundVolumeAsync(Guid gameId, Guid avatarId, double volume)
        {
            var result = new OASISResult<bool>();
            try
            {
                volume = Math.Max(0.0, Math.Min(1.0, volume));

                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.SoundVolume = volume;
                }

                await HolonManager.Instance.SaveSettingsAsync(avatarId, "game", new Dictionary<string, object> { { "soundVolume", volume } });

                result.Result = true;
                result.Message = $"Sound volume set to {volume * 100}%";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error setting sound volume: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets the master volume
        /// </summary>
        public async Task<OASISResult<double>> GetMasterVolumeAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<double>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    result.Result = session.MasterVolume;
                }
                else
                {
                    // Load from settings
                    var settingsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "game");
                    if (!settingsResult.IsError && settingsResult.Result != null && settingsResult.Result.ContainsKey("masterVolume"))
                    {
                        result.Result = Convert.ToDouble(settingsResult.Result["masterVolume"]);
                    }
                    else
                    {
                        result.Result = 1.0; // Default
                    }
                }

                result.Message = "Master volume retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting master volume: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets the voice volume
        /// </summary>
        public async Task<OASISResult<double>> GetVoiceVolumeAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<double>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    result.Result = session.VoiceVolume;
                }
                else
                {
                    var settingsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "game");
                    if (!settingsResult.IsError && settingsResult.Result != null && settingsResult.Result.ContainsKey("voiceVolume"))
                    {
                        result.Result = Convert.ToDouble(settingsResult.Result["voiceVolume"]);
                    }
                    else
                    {
                        result.Result = 1.0;
                    }
                }

                result.Message = "Voice volume retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting voice volume: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets the sound volume
        /// </summary>
        public async Task<OASISResult<double>> GetSoundVolumeAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<double>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    result.Result = session.SoundVolume;
                }
                else
                {
                    var settingsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "game");
                    if (!settingsResult.IsError && settingsResult.Result != null && settingsResult.Result.ContainsKey("soundVolume"))
                    {
                        result.Result = Convert.ToDouble(settingsResult.Result["soundVolume"]);
                    }
                    else
                    {
                        result.Result = 1.0;
                    }
                }

                result.Message = "Sound volume retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting sound volume: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        #endregion

        #region Video Settings

        /// <summary>
        /// Sets the video quality setting
        /// </summary>
        public async Task<OASISResult<bool>> SetVideoSettingAsync(Guid gameId, Guid avatarId, VideoSetting setting)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.VideoSetting = setting;
                }

                await HolonManager.Instance.SaveSettingsAsync(avatarId, "game", new Dictionary<string, object> { { "videoSetting", setting.ToString() } });

                result.Result = true;
                result.Message = $"Video setting set to {setting}";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error setting video setting: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets the video quality setting
        /// </summary>
        public async Task<OASISResult<VideoSetting>> GetVideoSettingAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<VideoSetting>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    result.Result = session.VideoSetting;
                }
                else
                {
                    var settingsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "game");
                    if (!settingsResult.IsError && settingsResult.Result != null && settingsResult.Result.ContainsKey("videoSetting"))
                    {
                        Enum.TryParse<VideoSetting>(settingsResult.Result["videoSetting"].ToString(), out var setting);
                        result.Result = setting;
                    }
                    else
                    {
                        result.Result = VideoSetting.Medium; // Default
                    }
                }

                result.Message = "Video setting retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting video setting: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        #endregion

        #region Input Management

        /// <summary>
        /// Binds keys to actions
        /// </summary>
        public async Task<OASISResult<bool>> BindKeysAsync(Guid gameId, Guid avatarId, Dictionary<string, string> keyBindings)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    foreach (var binding in keyBindings)
                    {
                        session.KeyBindings[binding.Key] = binding.Value;
                    }
                }

                // Save to avatar settings
                await HolonManager.Instance.SaveSettingsAsync(avatarId, "game", new Dictionary<string, object> { { "keyBindings", keyBindings } });

                result.Result = true;
                result.Message = "Keys bound successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error binding keys: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets current key bindings
        /// </summary>
        public async Task<OASISResult<Dictionary<string, string>>> GetKeyBindingsAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, string>>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null && session.KeyBindings.Count > 0)
                {
                    result.Result = session.KeyBindings;
                }
                else
                {
                    var settingsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "game");
                    if (!settingsResult.IsError && settingsResult.Result != null && settingsResult.Result.ContainsKey("keyBindings"))
                    {
                        result.Result = (Dictionary<string, string>)settingsResult.Result["keyBindings"];
                    }
                    else
                    {
                        result.Result = new Dictionary<string, string>(); // Default empty
                    }
                }

                result.Message = "Key bindings retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting key bindings: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        #endregion

        #region Cross-Game Interoperability

        /// <summary>
        /// Gets shared inventory items (keycards, items, etc.) that can be used across games, apps, websites, services
        /// Uses the AvatarDetail.Inventory property - the avatar's actual owned inventory
        /// This inventory is shared across ALL games, apps, websites, and services - enabling true cross-platform interoperability
        /// </summary>
        public async Task<OASISResult<List<IInventoryItem>>> GetSharedAssetsAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IInventoryItem>>();
            try
            {
                // Get inventory from AvatarDetail - this is the avatar's actual owned inventory
                var inventoryResult = await AvatarManager.Instance.GetAvatarInventoryAsync(avatarId);
                
                if (inventoryResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Error loading avatar inventory: {inventoryResult.Message}";
                    return result;
                }

                result.Result = inventoryResult.Result?.ToList() ?? new List<IInventoryItem>();
                result.Message = $"Retrieved {result.Result.Count} shared inventory items for avatar";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting shared assets: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Adds an item to the avatar's shared inventory (can be used across all games, apps, websites, services)
        /// Uses AvatarManager to add to AvatarDetail.Inventory
        /// </summary>
        public async Task<OASISResult<IInventoryItem>> AddItemToInventoryAsync(Guid avatarId, IInventoryItem item)
        {
            var result = new OASISResult<IInventoryItem>();
            try
            {
                var addResult = await AvatarManager.Instance.AddItemToAvatarInventoryAsync(avatarId, item);
                
                if (addResult.IsError)
                {
                    result.IsError = true;
                    result.Message = addResult.Message;
                    return result;
                }

                result.Result = addResult.Result;
                result.Message = "Item added to shared inventory successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error adding item to inventory: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Removes an item from the avatar's shared inventory
        /// Uses AvatarManager to remove from AvatarDetail.Inventory
        /// </summary>
        public async Task<OASISResult<bool>> RemoveItemFromInventoryAsync(Guid avatarId, Guid itemId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var removeResult = await AvatarManager.Instance.RemoveItemFromAvatarInventoryAsync(avatarId, itemId);
                
                if (removeResult.IsError)
                {
                    result.IsError = true;
                    result.Message = removeResult.Message;
                    return result;
                }

                result.Result = removeResult.Result;
                result.Message = "Item removed from shared inventory successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error removing item from inventory: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their shared inventory
        /// Uses AvatarManager to check AvatarDetail.Inventory
        /// </summary>
        public async Task<OASISResult<bool>> HasItemAsync(Guid avatarId, Guid itemId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var hasItemResult = await AvatarManager.Instance.AvatarHasItemAsync(avatarId, itemId);
                
                if (hasItemResult.IsError)
                {
                    result.IsError = true;
                    result.Message = hasItemResult.Message;
                    return result;
                }

                result.Result = hasItemResult.Result;
                result.Message = hasItemResult.Message;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error checking for item: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their shared inventory
        /// Uses AvatarManager to check AvatarDetail.Inventory
        /// </summary>
        public async Task<OASISResult<bool>> HasItemByNameAsync(Guid avatarId, string itemName)
        {
            var result = new OASISResult<bool>();
            try
            {
                var hasItemResult = await AvatarManager.Instance.AvatarHasItemByNameAsync(avatarId, itemName);
                
                if (hasItemResult.IsError)
                {
                    result.IsError = true;
                    result.Message = hasItemResult.Message;
                    return result;
                }

                result.Result = hasItemResult.Result;
                result.Message = hasItemResult.Message;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error checking for item by name: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets active quests that span multiple games
        /// </summary>
        public async Task<OASISResult<List<IQuestBase>>> GetCrossGameQuestsAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IQuestBase>>();
            try
            {
                if (QuestManager == null)
                {
                    result.IsError = true;
                    result.Message = "Quest manager not initialized";
                    return result;
                }

                // Get all active quests for the avatar
                var questsResult = await QuestManager.LoadAllForAvatarAsync(avatarId);
                if (questsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = questsResult.Message;
                    return result;
                }

                // Filter for cross-game quests (quests that can span multiple games)
                var filtered = questsResult.Result?.Where(q => 
                    q.MetaData != null && 
                    q.MetaData.ContainsKey("CrossGame") && 
                    Convert.ToBoolean(q.MetaData["CrossGame"])
                ).ToList();
                var crossGameQuests = filtered != null ? filtered.Cast<IQuestBase>().ToList() : new List<IQuestBase>();

                result.Result = crossGameQuests;
                result.Message = "Cross-game quests retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting cross-game quests: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets avatar's karma score (shared across all games)
        /// </summary>
        public async Task<OASISResult<int>> GetAvatarKarmaAsync(Guid avatarId)
        {
            var result = new OASISResult<int>();
            try
            {
                if (KarmaManager == null)
                {
                    result.IsError = true;
                    result.Message = "Karma manager not initialized";
                    return result;
                }

                var karmaResult = await KarmaManager.GetKarmaAsync(avatarId);
                if (karmaResult.IsError)
                {
                    result.IsError = true;
                    result.Message = karmaResult.Message;
                    return result;
                }

                result.Result = karmaResult.IsError ? 0 : (int)karmaResult.Result;
                result.Message = "Karma retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting karma: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        #endregion
    }
}
