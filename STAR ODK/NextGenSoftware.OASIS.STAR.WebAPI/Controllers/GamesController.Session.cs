using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    public partial class GamesController : STARControllerBase
    {

        /// <summary>
        /// Starts a new game session
        /// </summary>
        [HttpPost("{gameId}/start")]
        [ProducesResponseType(typeof(OASISResult<GameSession>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GameSession>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartGame(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.StartGameAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GameSession>(ex, "starting game");
            }
        }

        /// <summary>
        /// Ends a game session
        /// </summary>
        [HttpPost("{gameId}/end")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EndGame(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.EndGameAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "ending game");
            }
        }

        /// <summary>
        /// Loads a game into memory
        /// </summary>
        [HttpPost("{gameId}/load")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGame(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.LoadGameAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "loading game");
            }
        }

        /// <summary>
        /// Unloads a game from memory
        /// </summary>
        [HttpPost("{gameId}/unload")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnloadGame(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.UnloadGameAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "unloading game");
            }
        }



        /// <summary>
        /// Loads a specific level in a game
        /// </summary>
        [HttpPost("{gameId}/levels/{level}/load")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadLevel(Guid gameId, string level)
        {
            try
            {
                var result = await _starAPI.Game.LoadLevelAsync(gameId, level, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "loading level");
            }
        }

        /// <summary>
        /// Unloads a specific level
        /// </summary>
        [HttpPost("{gameId}/levels/{level}/unload")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnloadLevel(Guid gameId, string level)
        {
            try
            {
                var result = await _starAPI.Game.UnloadLevelAsync(gameId, level);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "unloading level");
            }
        }

        /// <summary>
        /// Jumps to a specific level
        /// </summary>
        [HttpPost("{gameId}/levels/{level}/jump")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> JumpToLevel(Guid gameId, string level)
        {
            try
            {
                var result = await _starAPI.Game.JumpToLevelAsync(gameId, level, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "jumping to level");
            }
        }

        /// <summary>
        /// Jumps to a specific point in a level
        /// </summary>
        [HttpPost("{gameId}/levels/{level}/jump-to-point")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> JumpToPointInLevel(Guid gameId, string level, [FromBody] Point3D point)
        {
            try
            {
                var result = await _starAPI.Game.JumpToPointInLevelAsync(gameId, level, point.X, point.Y, point.Z, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "jumping to point");
            }
        }



        /// <summary>
        /// Loads an area around a specific point
        /// </summary>
        [HttpPost("{gameId}/areas/load")]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadArea(Guid gameId, [FromBody] LoadAreaRequest request)
        {
            try
            {
                var result = await _starAPI.Game.LoadAreaAsync(gameId, request.X, request.Y, request.Z, request.Radius, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Guid>(ex, "loading area");
            }
        }

        /// <summary>
        /// Unloads an area
        /// </summary>
        [HttpPost("{gameId}/areas/{areaId}/unload")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnloadArea(Guid gameId, Guid areaId)
        {
            try
            {
                var result = await _starAPI.Game.UnloadAreaAsync(gameId, areaId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "unloading area");
            }
        }

        /// <summary>
        /// Jumps to a specific area
        /// </summary>
        [HttpPost("{gameId}/areas/jump")]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> JumpToArea(Guid gameId, [FromBody] JumpToAreaRequest request)
        {
            try
            {
                var result = await _starAPI.Game.JumpToAreaAsync(gameId, request.X, request.Y, request.Z, AvatarId, request.Radius);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Guid>(ex, "jumping to area");
            }
        }



        /// <summary>
        /// Shows the title screen
        /// </summary>
        [HttpPost("{gameId}/ui/title-screen")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ShowTitleScreen(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.ShowTitleScreenAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "showing title screen");
            }
        }

        /// <summary>
        /// Shows the main menu
        /// </summary>
        [HttpPost("{gameId}/ui/main-menu")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ShowMainMenu(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.ShowMainMenuAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "showing main menu");
            }
        }

        /// <summary>
        /// Shows the options menu
        /// </summary>
        [HttpPost("{gameId}/ui/options")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ShowOptions(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.ShowOptionsAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "showing options");
            }
        }

        /// <summary>
        /// Shows the credits screen
        /// </summary>
        [HttpPost("{gameId}/ui/credits")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ShowCredits(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.ShowCreditsAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "showing credits");
            }
        }



        /// <summary>
        /// Sets the master volume
        /// </summary>
        [HttpPost("{gameId}/audio/master-volume")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetMasterVolume(Guid gameId, [FromBody] VolumeRequest request)
        {
            try
            {
                var result = await _starAPI.Game.SetMasterVolumeAsync(gameId, AvatarId, request.Volume);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "setting master volume");
            }
        }

        /// <summary>
        /// Sets the voice volume
        /// </summary>
        [HttpPost("{gameId}/audio/voice-volume")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetVoiceVolume(Guid gameId, [FromBody] VolumeRequest request)
        {
            try
            {
                var result = await _starAPI.Game.SetVoiceVolumeAsync(gameId, AvatarId, request.Volume);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "setting voice volume");
            }
        }

        /// <summary>
        /// Sets the sound volume
        /// </summary>
        [HttpPost("{gameId}/audio/sound-volume")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetSoundVolume(Guid gameId, [FromBody] VolumeRequest request)
        {
            try
            {
                var result = await _starAPI.Game.SetSoundVolumeAsync(gameId, AvatarId, request.Volume);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "setting sound volume");
            }
        }

        /// <summary>
        /// Gets the master volume
        /// </summary>
        [HttpGet("{gameId}/audio/master-volume")]
        [ProducesResponseType(typeof(OASISResult<double>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<double>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMasterVolume(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.GetMasterVolumeAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<double>(ex, "getting master volume");
            }
        }

        /// <summary>
        /// Gets the voice volume
        /// </summary>
        [HttpGet("{gameId}/audio/voice-volume")]
        [ProducesResponseType(typeof(OASISResult<double>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<double>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetVoiceVolume(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.GetVoiceVolumeAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<double>(ex, "getting voice volume");
            }
        }

        /// <summary>
        /// Gets the sound volume
        /// </summary>
        [HttpGet("{gameId}/audio/sound-volume")]
        [ProducesResponseType(typeof(OASISResult<double>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<double>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSoundVolume(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.GetSoundVolumeAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<double>(ex, "getting sound volume");
            }
        }



        /// <summary>
        /// Sets the video quality setting
        /// </summary>
        [HttpPost("{gameId}/video/setting")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetVideoSetting(Guid gameId, [FromBody] VideoSettingRequest request)
        {
            try
            {
                var result = await _starAPI.Game.SetVideoSettingAsync(gameId, AvatarId, request.Setting);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "setting video setting");
            }
        }

        /// <summary>
        /// Gets the video quality setting
        /// </summary>
        [HttpGet("{gameId}/video/setting")]
        [ProducesResponseType(typeof(OASISResult<VideoSetting>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<VideoSetting>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetVideoSetting(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.GetVideoSettingAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<VideoSetting>(ex, "getting video setting");
            }
        }



        /// <summary>
        /// Binds keys to actions
        /// </summary>
        [HttpPost("{gameId}/input/bind-keys")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BindKeys(Guid gameId, [FromBody] Dictionary<string, string> keyBindings)
        {
            try
            {
                var result = await _starAPI.Game.BindKeysAsync(gameId, AvatarId, keyBindings);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "binding keys");
            }
        }

        /// <summary>
        /// Gets current key bindings
        /// </summary>
        [HttpGet("{gameId}/input/bind-keys")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<string, string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Dictionary<string, string>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetKeyBindings(Guid gameId)
        {
            try
            {
                var result = await _starAPI.Game.GetKeyBindingsAsync(gameId, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, string>>
                {
                    IsError = true,
                    Message = $"Error getting key bindings: {ex.Message}",
                    Exception = ex
                });
            }
        }


    }
}
