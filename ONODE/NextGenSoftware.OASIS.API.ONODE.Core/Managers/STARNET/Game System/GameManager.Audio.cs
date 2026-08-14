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

    }
}
