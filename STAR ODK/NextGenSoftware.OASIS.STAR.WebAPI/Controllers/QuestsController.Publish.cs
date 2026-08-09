using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{    public partial class QuestsController
    {
        /// Publishes a quest to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the quest publish operation.</returns>
        /// <response code="200">Quest published successfully</response>
        /// <response code="400">Error publishing quest</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishQuest(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Quests.PublishAsync(
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
                return HandleException<Quest>(ex, "publishing quest");
            }
        }

        /// <summary>
        /// Downloads a quest from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to download.</param>
        /// <param name="version">The version of the quest to download.</param>
        /// <param name="downloadPath">Optional path where the quest should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the quest download operation.</returns>
        /// <response code="200">Quest downloaded successfully</response>
        /// <response code="400">Error downloading quest</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedQuest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedQuest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadQuest(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Quests.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedQuest>(ex, "downloading quest");
            }
        }

        /// <summary>
        /// Gets all versions of a specific quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to get versions for.</param>
        /// <returns>List of all versions of the specified quest.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Quests.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error retrieving quest versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested quest version details.</returns>
        /// <response code="200">Quest version loaded successfully</response>
        /// <response code="400">Error loading quest version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadQuestVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Quests.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "loading quest version");
            }
        }

        /// <summary>
        /// Edits a quest with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the quest edit operation.</returns>
        /// <response code="200">Quest edited successfully</response>
        /// <response code="400">Error editing quest</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditQuest(Guid id, [FromBody] EditQuestRequest request)
        {
            try
            {
                var result = await _starAPI.Quests.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "editing quest");
            }
        }

        /// <summary>
        /// Unpublishes a quest from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to unpublish.</param>
        /// <param name="version">The version of the quest to unpublish.</param>
        /// <returns>Result of the quest unpublish operation.</returns>
        /// <response code="200">Quest unpublished successfully</response>
        /// <response code="400">Error unpublishing quest</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "unpublishing quest");
            }
        }

        /// <summary>
        /// Republishes a quest to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to republish.</param>
        /// <param name="version">The version of the quest to republish.</param>
        /// <returns>Result of the quest republish operation.</returns>
        /// <response code="200">Quest republished successfully</response>
        /// <response code="400">Error republishing quest</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "republishing quest");
            }
        }

        /// <summary>
        /// Activates a quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to activate.</param>
        /// <param name="version">The version of the quest to activate.</param>
        /// <returns>Result of the quest activation operation.</returns>
        /// <response code="200">Quest activated successfully</response>
        /// <response code="400">Error activating quest</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "activating quest");
            }
        }

        /// <summary>
        /// Deactivates a quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to deactivate.</param>
        /// <param name="version">The version of the quest to deactivate.</param>
        /// <returns>Result of the quest deactivation operation.</returns>
        /// <response code="200">Quest deactivated successfully</response>
        /// <response code="400">Error deactivating quest</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "deactivating quest");
            }
        }

    }
}