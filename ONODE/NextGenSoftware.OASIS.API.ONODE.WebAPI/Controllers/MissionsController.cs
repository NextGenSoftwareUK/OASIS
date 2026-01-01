using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/missions")]
    [ApiController]
    [Authorize]
    public class MissionsController : OASISControllerBase
    {
        private readonly ILogger<MissionsController> _logger;

        public MissionsController(ILogger<MissionsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Create a new mission
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status500InternalServerError)]
        public async Task<OASISResult<Mission>> CreateMission([FromBody] CreateMissionRequest request)
        {
            OASISResult<Mission> result = new OASISResult<Mission>();

            try
            {
                if (request == null)
                {
                    result.IsError = true;
                    result.Message = "Mission request is required";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    result.IsError = true;
                    result.Message = "Mission name is required";
                    return result;
                }

                if (AvatarId == Guid.Empty)
                {
                    result.IsError = true;
                    result.Message = "Avatar ID is required. Please ensure you are authenticated.";
                    return result;
                }

                // Load STARDNA (simplified - in production this would come from configuration)
                STARDNA starDNA = new STARDNA();
                OASISDNA oasisDNA = null;

                // Create MissionManager
                MissionManager missionManager = new MissionManager(AvatarId, starDNA, oasisDNA);

                // Parse MissionType enum
                MissionType missionType = MissionType.Easy;
                if (!string.IsNullOrWhiteSpace(request.MissionType))
                {
                    if (Enum.TryParse<MissionType>(request.MissionType, true, out MissionType parsedType))
                    {
                        missionType = parsedType;
                    }
                }

                // Create the mission using MissionManager's CreateAsync method
                // Note: MissionManager.CreateAsync requires a fullPathToSourceFolder
                // For now, we'll create a simple path or handle it differently
                string fullPathToSourceFolder = $"missions/{request.Name}";

                var mission = new Mission
                {
                    Name = request.Name,
                    Description = request.Description ?? "",
                    MissionType = missionType,
                    RewardXP = request.RewardXP ?? 0,
                    RewardKarma = request.RewardKarma ?? 0,
                    Status = QuestStatus.NotStarted
                };

                // Get default storage provider and create HolonManager
                var providerResult = await GetAndActivateDefaultStorageProviderAsync();
                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message ?? "Failed to get storage provider";
                    return result;
                }

                var holonManager = new HolonManager(providerResult.Result);
                var saveResult = await holonManager.SaveHolonAsync(mission, AvatarId);

                if (saveResult.IsError || saveResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = saveResult.Message ?? "Failed to create mission";
                    return result;
                }

                result.Result = (Mission)saveResult.Result;
                result.Message = "Mission created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mission");
                result.IsError = true;
                result.Message = $"An error occurred while creating the mission: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Get all missions for the current avatar
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<List<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status500InternalServerError)]
        public async Task<OASISResult<List<Mission>>> GetMissions()
        {
            OASISResult<List<Mission>> result = new OASISResult<List<Mission>>();

            try
            {
                if (AvatarId == Guid.Empty)
                {
                    result.IsError = true;
                    result.Message = "Avatar ID is required. Please ensure you are authenticated.";
                    return result;
                }

                // Load STARDNA
                STARDNA starDNA = new STARDNA();
                OASISDNA oasisDNA = null;

                // Create MissionManager
                MissionManager missionManager = new MissionManager(AvatarId, starDNA, oasisDNA);

                // Load all missions for the avatar
                var missionsResult = await missionManager.LoadAllForAvatarAsync(AvatarId);

                if (missionsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = missionsResult.Message ?? "Failed to load missions";
                    return result;
                }

                var missionsList = new List<Mission>();
                if (missionsResult.Result != null)
                {
                    foreach (var mission in missionsResult.Result)
                    {
                        missionsList.Add((Mission)mission);
                    }
                }

                result.Result = missionsList;
                result.Message = "Missions loaded successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading missions");
                result.IsError = true;
                result.Message = $"An error occurred while loading missions: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }
    }

    /// <summary>
    /// Request model for creating a mission
    /// </summary>
    public class CreateMissionRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string MissionType { get; set; } = "Easy";
        public long? RewardXP { get; set; }
        public long? RewardKarma { get; set; }
        public List<ChapterRequest> Chapters { get; set; }
        public List<QuestRequest> Quests { get; set; }
    }

    /// <summary>
    /// Request model for chapter
    /// </summary>
    public class ChapterRequest
    {
        public string Name { get; set; }
        public string ChapterDisplayName { get; set; } = "Chapter";
    }

    /// <summary>
    /// Request model for quest
    /// </summary>
    public class QuestRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}

