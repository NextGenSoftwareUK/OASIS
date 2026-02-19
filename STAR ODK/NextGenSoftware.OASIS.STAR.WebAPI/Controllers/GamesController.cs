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
    /// <summary>
    /// Game management endpoints for creating, updating, and managing STAR games.
    /// Games can be created, searched, edited, published, downloaded, and installed through the STARNET system.
    /// Also provides game session management, level/area loading, UI, audio, video, and input controls.
    /// Enables cross-game interoperability with shared assets, karma, NFTs, and quests.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());
        private static readonly SemaphoreSlim _bootLock = new(1, 1);

        private IActionResult ValidateAvatarId<T>()
        {
            // Skip validation if test mode is enabled - let the method try to execute and return test data on failure
            if (UseTestDataWhenLiveDataNotAvailable)
                return null;
                
            if (AvatarId == Guid.Empty)
            {
                return BadRequest(new OASISResult<T>
                {
                    IsError = true,
                    Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                });
            }
            return null;
        }

        private static async Task EnsureStarApiBootedAsync()
        {
            if (_starAPI.IsOASISBooted)
                return;

            await _bootLock.WaitAsync();
            try
            {
                if (_starAPI.IsOASISBooted)
                    return;

                var boot = await _starAPI.BootOASISAsync("admin", "admin");
                if (boot.IsError)
                    throw new OASISException(boot.Message ?? "Failed to ignite WEB5 STAR API runtime.");
            }
            finally
            {
                _bootLock.Release();
            }
        }

        #region STARNET CRUD Operations

        /// <summary>
        /// Retrieves all games in the system.
        /// </summary>
        /// <returns>List of all games available in the STAR system.</returns>
        /// <response code="200">Games retrieved successfully</response>
        /// <response code="400">Error retrieving games</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllGames()
        {
            try
            {
                var result = await _starAPI.Game.LoadAllAsync(AvatarId, null);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testGames = TestDataHelper.GetTestGames(5);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<Game>>(testGames, "Games retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testGames = TestDataHelper.GetTestGames(5);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<Game>>(testGames, "Games retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<Game>>(ex, "GetAllGames");
            }
        }

        /// <summary>
        /// Retrieves a specific game by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the game to retrieve.</param>
        /// <returns>The requested game details.</returns>
        /// <response code="200">Game retrieved successfully</response>
        /// <response code="400">Error retrieving game</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGame(Guid id)
        {
            try
            {
                var result = await _starAPI.Game.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testGame = TestDataHelper.GetTestGame(id);
                    return Ok(TestDataHelper.CreateSuccessResult<Game>(testGame, "Game retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testGame = TestDataHelper.GetTestGame(id);
                    return Ok(TestDataHelper.CreateSuccessResult<Game>(testGame, "Game retrieved successfully (using test data)"));
                }
                return HandleException<Game>(ex, "GetGame");
            }
        }

        /// <summary>
        /// Creates a new game for the authenticated avatar.
        /// </summary>
        /// <param name="game">The game details to create.</param>
        /// <returns>The created game with assigned ID and metadata.</returns>
        /// <response code="200">Game created successfully</response>
        /// <response code="400">Error creating game</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<IGame>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGame>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGame([FromBody] IGame game)
        {
            try
            {
                if (game == null)
                {
                    return BadRequest(new OASISResult<IGame>
                    {
                        IsError = true,
                        Message = "Game cannot be null. Please provide a valid Game object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGame>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                var result = await _starAPI.Game.UpdateAsync(AvatarId, (Game)game);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IGame>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IGame>(ex, "creating game");
            }
        }

        /// <summary>
        /// Updates an existing game.
        /// </summary>
        /// <param name="id">The unique identifier of the game to update.</param>
        /// <param name="game">The updated game details.</param>
        /// <returns>The updated game.</returns>
        /// <response code="200">Game updated successfully</response>
        /// <response code="400">Error updating game</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<IGame>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGame>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGame(Guid id, [FromBody] IGame game)
        {
            try
            {
                if (game == null)
                {
                    return BadRequest(new OASISResult<IGame>
                    {
                        IsError = true,
                        Message = "Game cannot be null. Please provide a valid Game object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGame>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                var result = await _starAPI.Game.UpdateAsync(AvatarId, (Game)game);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IGame>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IGame>(ex, "updating game");
            }
        }

        /// <summary>
        /// Deletes a game.
        /// </summary>
        /// <param name="id">The unique identifier of the game to delete.</param>
        /// <returns>Success status.</returns>
        /// <response code="200">Game deleted successfully</response>
        /// <response code="400">Error deleting game</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGame(Guid id)
        {
            try
            {
                var result = await _starAPI.Game.DeleteAsync(AvatarId, id, 0, true, true, true, ProviderType.Default);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting game");
            }
        }

        #endregion

        #region STARNET Management Operations

        /// <summary>
        /// Clones an existing game.
        /// </summary>
        /// <param name="id">The unique identifier of the game to clone.</param>
        /// <param name="request">Clone request containing the new name for the cloned game.</param>
        /// <returns>The newly created cloned game.</returns>
        [HttpPost("{id}/clone")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CloneGame(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.Game.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "cloning game");
            }
        }

        /// <summary>
        /// Publishes a game to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the game to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the game publish operation.</returns>
        /// <response code="200">Game published successfully</response>
        /// <response code="400">Error publishing game</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishGame(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Game.PublishAsync(
                    AvatarId, 
                    request.SourcePath, 
                    request.LaunchTarget, 
                    request.PublishPath, 
                    request.Edit, 
                    request.RegisterOnSTARNET, 
                    request.GenerateBinary, 
                    request.UploadToCloud
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Game>(ex, "publishing game");
            }
        }

        /// <summary>
        /// Downloads a game from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the game to download.</param>
        /// <param name="version">The version of the game to download.</param>
        /// <param name="downloadPath">Optional path where the game should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the game download operation.</returns>
        /// <response code="200">Game downloaded successfully</response>
        /// <response code="400">Error downloading game</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedGame>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedGame>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadGame(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Game.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedGame>(ex, "downloading game");
            }
        }

        /// <summary>
        /// Installs a downloaded game.
        /// </summary>
        /// <param name="id">The unique identifier of the game to install.</param>
        /// <param name="version">The version of the game to install.</param>
        /// <param name="installPath">Optional path where the game should be installed.</param>
        /// <returns>Result of the game install operation.</returns>
        /// <response code="200">Game installed successfully</response>
        /// <response code="400">Error installing game</response>
        [HttpPost("{id}/install")]
        [ProducesResponseType(typeof(OASISResult<InstalledGame>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InstalledGame>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> InstallGame(Guid id, [FromQuery] int version = 0, [FromQuery] string installPath = "")
        {
            try
            {
                var result = await _starAPI.Game.DownloadAndInstallAsync(AvatarId, id, version, installPath ?? "", "", true, false, ProviderType.Default);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<InstalledGame>(ex, "installing game");
            }
        }

        /// <summary>
        /// Searches games by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of games matching the search query.</returns>
        /// <response code="200">Games retrieved successfully</response>
        /// <response code="400">Error searching games</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchGames([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.Game.LoadAllAsync(AvatarId, null);
                if (result.IsError)
                    return BadRequest(result);

                var filteredGames = result.Result?.Where(g => 
                    g.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    g.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<Game>>
                {
                    Result = filteredGames,
                    IsError = false,
                    Message = "Games retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Game>>
                {
                    IsError = true,
                    Message = $"Error searching games: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets games by type.
        /// </summary>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGamesByType(string type)
        {
            try
            {
                var result = await _starAPI.Game.LoadAllAsync(AvatarId, null);
                if (result.IsError)
                    return BadRequest(result);

                var filteredGames = result.Result?.Where(g => g.GameType.ToString() == type);
                return Ok(new OASISResult<IEnumerable<Game>>
                {
                    Result = filteredGames,
                    IsError = false,
                    Message = "Games retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Game>>
                {
                    IsError = true,
                    Message = $"Error retrieving games by type: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a game by ID with optional version and holon type.
        /// </summary>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGameById(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Game.LoadAsync(AvatarId, id, version, holonTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testGame = TestDataHelper.GetTestGame();
                    return Ok(TestDataHelper.CreateSuccessResult<Game>(testGame, "Game loaded successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testGame = TestDataHelper.GetTestGame();
                    return Ok(TestDataHelper.CreateSuccessResult<Game>(testGame, "Game loaded successfully (using test data)"));
                }
                return HandleException<Game>(ex, "loading game");
            }
        }

        /// <summary>
        /// Loads a game from source or installed folder path.
        /// </summary>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGameFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Game.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testGame = TestDataHelper.GetTestGame();
                    return Ok(TestDataHelper.CreateSuccessResult<Game>(testGame, "Game loaded successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testGame = TestDataHelper.GetTestGame();
                    return Ok(TestDataHelper.CreateSuccessResult<Game>(testGame, "Game loaded successfully (using test data)"));
                }
                return HandleException<Game>(ex, "loading game from path");
            }
        }

        /// <summary>
        /// Loads a game from a published file.
        /// </summary>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGameFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Game.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Game>(ex, "loading game from published file");
            }
        }

        /// <summary>
        /// Loads all games for the authenticated avatar.
        /// </summary>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllGamesForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Game.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Game>>
                {
                    IsError = true,
                    Message = $"Error loading games for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all versions of a specific game.
        /// </summary>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Game>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGameVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Game.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Game>>
                {
                    IsError = true,
                    Message = $"Error retrieving game versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a game.
        /// </summary>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGameVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Game.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Game>(ex, "loading game version");
            }
        }

        /// <summary>
        /// Edits a game with new DNA configuration.
        /// </summary>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditGame(Guid id, [FromBody] EditGameRequest request)
        {
            try
            {
                var result = await _starAPI.Game.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Game>(ex, "editing game");
            }
        }

        /// <summary>
        /// Unpublishes a game from the STARNET system.
        /// </summary>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishGame(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Game.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "unpublishing game");
            }
        }

        /// <summary>
        /// Republishes a game to the STARNET system.
        /// </summary>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishGame(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Game.RepublishAsync(AvatarId, id, 0, ProviderType.Default);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Game>(ex, "republishing game");
            }
        }

        /// <summary>
        /// Activates a game.
        /// </summary>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateGame(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Game.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Game>(ex, "activating game");
            }
        }

        /// <summary>
        /// Deactivates a game.
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateGame(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Game.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Game>(ex, "deactivating game");
            }
        }

        /// <summary>
        /// Creates a new game with specified parameters.
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Game>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGameWithOptions([FromBody] CreateGameRequest request)
        {
            try
            {
                var result = await _starAPI.Game.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testGame = TestDataHelper.GetTestGame();
                    return Ok(TestDataHelper.CreateSuccessResult<Game>(testGame, "Game created successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testGame = TestDataHelper.GetTestGame();
                    return Ok(TestDataHelper.CreateSuccessResult<Game>(testGame, "Game created successfully (using test data)"));
                }
                return HandleException<Game>(ex, "creating game");
            }
        }

        #endregion

        #region Game Session Management

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

        #endregion

        #region Level Management

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

        #endregion

        #region Area Management

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

        #endregion

        #region UI Management

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

        #endregion

        #region Audio Settings

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

        #endregion

        #region Video Settings

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

        #endregion

        #region Input Management

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

        #endregion

        #region Cross-Game Interoperability - Shared Inventory System

        /// <summary>
        /// Gets shared inventory items (keycards, items, etc.) that can be used across games, apps, websites, services
        /// This uses the AvatarDetail.Inventory property - the avatar's actual owned inventory
        /// </summary>
        [HttpGet("shared-inventory")]
        [ProducesResponseType(typeof(OASISResult<List<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<List<IInventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSharedInventory()
        {
            try
            {
                var result = await _starAPI.Game.GetSharedAssetsAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error getting shared inventory: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Adds an item to the avatar's shared inventory (can be used across all games, apps, websites, services)
        /// </summary>
        [HttpPost("shared-inventory/add")]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddItemToInventory([FromBody] InventoryItem item)
        {
            try
            {
                var result = await _starAPI.Game.AddItemToInventoryAsync(AvatarId, item);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<IInventoryItem>(ex, "adding item to inventory");
            }
        }

        /// <summary>
        /// Removes an item from the avatar's shared inventory
        /// </summary>
        [HttpDelete("shared-inventory/{itemId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveItemFromInventory(Guid itemId)
        {
            try
            {
                var result = await _starAPI.Game.RemoveItemFromInventoryAsync(AvatarId, itemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "removing item from inventory");
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their shared inventory
        /// </summary>
        [HttpGet("shared-inventory/{itemId}/has")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HasItem(Guid itemId)
        {
            try
            {
                var result = await _starAPI.Game.HasItemAsync(AvatarId, itemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "checking for item");
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their shared inventory
        /// </summary>
        [HttpGet("shared-inventory/has-by-name")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HasItemByName([FromQuery] string itemName)
        {
            try
            {
                var result = await _starAPI.Game.HasItemByNameAsync(AvatarId, itemName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "checking for item by name");
            }
        }

        /// <summary>
        /// Gets active quests that span multiple games
        /// </summary>
        [HttpGet("cross-game-quests")]
        [ProducesResponseType(typeof(OASISResult<List<IQuestBase>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<List<IQuestBase>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCrossGameQuests()
        {
            try
            {
                var result = await _starAPI.Game.GetCrossGameQuestsAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<List<IQuestBase>>
                {
                    IsError = true,
                    Message = $"Error getting cross-game quests: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets avatar's karma score (shared across all games)
        /// </summary>
        [HttpGet("karma")]
        [ProducesResponseType(typeof(OASISResult<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvatarKarma()
        {
            try
            {
                var result = await _starAPI.Game.GetAvatarKarmaAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<int>(ex, "getting karma");
            }
        }

        #endregion
    }

    #region Request Models

    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public class LoadAreaRequest
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Radius { get; set; } = 100.0;
    }

    public class JumpToAreaRequest
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Radius { get; set; } = 100.0;
    }

    public class VolumeRequest
    {
        public double Volume { get; set; }
    }

    public class VideoSettingRequest
    {
        public VideoSetting Setting { get; set; }
    }

    public class CreateGameRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Game;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<Game, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditGameRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    #endregion
}

