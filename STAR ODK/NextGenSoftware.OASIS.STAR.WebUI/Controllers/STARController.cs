/*
 * OLD STARController - COMMENTED OUT TO AVOID CONFUSION
 * This was replaced by the separate STAR Web API project
 * The WebUI now calls the STAR Web API directly via HTTP
 * 
 * Date: 2024-12-19
 * Reason: Replaced with separate STAR Web API project for better separation of concerns
 */

/*
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.STAR;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.WebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class STARController : ControllerBase
    {
        private static STARAPI? _starAPI;
        private static readonly object _lock = new object();

        private STARAPI GetSTARAPI()
        {
            if (_starAPI == null)
            {
                lock (_lock)
                {
                    if (_starAPI == null)
                    {
                        var starDNA = new STARDNA();
                        _starAPI = new STARAPI(starDNA);
                    }
                }
            }
            return _starAPI;
        }
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            try
            {
                var starAPI = GetSTARAPI();
                var isIgnited = starAPI.IsOASISBooted;
                
                // Return more detailed status information
                return Ok(new { 
                    isIgnited, 
                    status = isIgnited ? "ignited" : "extinguished",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    providers = starAPI.GetProviderManager()?.GetAllProviders()?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("ignite")]
        public async Task<IActionResult> IgniteSTAR([FromBody] IgniteRequest? request = null)
        {
            try
            {
                var starAPI = GetSTARAPI();
                var result = await starAPI.BootOASISAsync(
                    request?.UserName ?? "admin", 
                    request?.Password ?? "admin"
                );
                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }
                
                // Return detailed success information
                return Ok(new { 
                    success = true, 
                    message = "STAR ignited successfully", 
                    result = result.Result,
                    isIgnited = starAPI.IsOASISBooted,
                    timestamp = DateTime.UtcNow,
                    providers = starAPI.GetProviderManager()?.GetAllProviders()?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("extinguish")]
        public async Task<IActionResult> ExtinguishSTAR()
        {
            try
            {
                var result = await STARAPI.ShutdownOASISAsync();
                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }
                return Ok(new { success = true, message = "STAR extinguished successfully", result = result.Result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("beam-in")]
        public async Task<IActionResult> BeamIn([FromBody] BeamInRequest request)
        {
            try
            {
                var starAPI = GetSTARAPI();
                // First ensure OASIS is booted
                var bootResult = await starAPI.BootOASISAsync(request.Username, request.Password);
                if (bootResult.IsError)
                {
                    return BadRequest(new { error = bootResult.Message });
                }
                
                // For now, return success - the STARAPI handles avatar management internally
                return Ok(new { success = true, message = "Beamed in successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("create-avatar")]
        public async Task<IActionResult> CreateAvatar([FromBody] CreateAvatarRequest request)
        {
            try
            {
                // For now, return a placeholder - we can implement this later using STARAPI
                return Ok(new { success = true, message = "Avatar creation endpoint ready", request });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("light")]
        public async Task<IActionResult> Light([FromBody] LightRequest request)
        {
            try
            {
                // For now, return a placeholder - we can implement this later using STARAPI
                return Ok(new { success = true, message = "OAPP light endpoint ready", request });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("seed")]
        public async Task<IActionResult> Seed([FromBody] SeedRequest request)
        {
            try
            {
                // For now, return a placeholder - we can implement this later using STARAPI
                return Ok(new { success = true, message = "OAPP seed endpoint ready", request });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("unseed")]
        public async Task<IActionResult> UnSeed([FromBody] UnSeedRequest request)
        {
            try
            {
                // For now, return a placeholder - we can implement this later using STARAPI
                return Ok(new { success = true, message = "OAPP unseed endpoint ready", request });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Avatar Operations
        [HttpGet("avatar/current")]
        public async Task<IActionResult> GetBeamedInAvatar()
        {
            try
            {
                // For now, return a placeholder avatar
                var placeholderAvatar = new
                {
                    id = Guid.NewGuid().ToString(),
                    title = "Dr",
                    firstName = "John",
                    lastName = "Doe",
                    email = "john.doe@example.com",
                    username = "johndoe",
                    isBeamedIn = true,
                    lastBeamedIn = DateTime.UtcNow
                };
                return Ok(new { success = true, result = placeholderAvatar });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("avatar/beam-in")]
        public async Task<IActionResult> BeamInAvatar()
        {
            try
            {
                // For now, return a placeholder
                return Ok(new { success = true, message = "Avatar beamed in successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("avatar/{id}")]
        public async Task<IActionResult> GetAvatar(string id)
        {
            try
            {
                // For now, return a placeholder avatar
                var placeholderAvatar = new
                {
                    id = id,
                    title = "Dr",
                    firstName = "John",
                    lastName = "Doe",
                    email = "john.doe@example.com",
                    username = "johndoe",
                    isBeamedIn = false,
                    lastBeamedIn = DateTime.UtcNow.AddDays(-1)
                };
                return Ok(new { success = true, result = placeholderAvatar });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("avatar/username/{username}")]
        public async Task<IActionResult> GetAvatarByUsername(string username)
        {
            try
            {
                // For now, return a placeholder avatar
                var placeholderAvatar = new
                {
                    id = Guid.NewGuid().ToString(),
                    title = "Dr",
                    firstName = "John",
                    lastName = "Doe",
                    email = "john.doe@example.com",
                    username = username,
                    isBeamedIn = false,
                    lastBeamedIn = DateTime.UtcNow.AddDays(-1)
                };
                return Ok(new { success = true, result = placeholderAvatar });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("avatar/login")]
        public async Task<IActionResult> LoginAvatar([FromBody] LoginAvatarRequest request)
        {
            try
            {
                // For now, return a placeholder avatar
                var placeholderAvatar = new
                {
                    id = Guid.NewGuid().ToString(),
                    title = "Dr",
                    firstName = "John",
                    lastName = "Doe",
                    email = "john.doe@example.com",
                    username = request.Username,
                    isBeamedIn = true,
                    lastBeamedIn = DateTime.UtcNow
                };
                return Ok(new { success = true, result = placeholderAvatar });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("avatar")]
        public async Task<IActionResult> SaveAvatar([FromBody] object avatar)
        {
            try
            {
                // For now, return success
                return Ok(new { success = true, message = "Avatar saved successfully", result = avatar });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("avatar/{id}")]
        public async Task<IActionResult> DeleteAvatar(string id)
        {
            try
            {
                // For now, return success
                return Ok(new { success = true, message = "Avatar deleted successfully", result = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("avatars")]
        public async Task<IActionResult> GetAllAvatars()
        {
            try
            {
                // For now, return placeholder avatars
                var placeholderAvatars = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        title = "Dr",
                        firstName = "John",
                        lastName = "Doe",
                        email = "john.doe@example.com",
                        username = "johndoe",
                        isBeamedIn = true,
                        lastBeamedIn = DateTime.UtcNow
                    },
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        title = "Ms",
                        firstName = "Jane",
                        lastName = "Smith",
                        email = "jane.smith@example.com",
                        username = "janesmith",
                        isBeamedIn = false,
                        lastBeamedIn = DateTime.UtcNow.AddDays(-2)
                    }
                };
                return Ok(new { success = true, result = placeholderAvatars });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("avatars/search")]
        public async Task<IActionResult> SearchAvatars([FromQuery] string searchTerm)
        {
            try
            {
                // For now, return placeholder avatars
                var placeholderAvatars = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        title = "Dr",
                        firstName = "John",
                        lastName = "Doe",
                        email = "john.doe@example.com",
                        username = "johndoe",
                        isBeamedIn = true,
                        lastBeamedIn = DateTime.UtcNow
                    }
                };
                return Ok(new { success = true, result = placeholderAvatars });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Karma Operations
        [HttpGet("karma/{avatarId}")]
        public async Task<IActionResult> GetKarma(string avatarId)
        {
            try
            {
                // For now, return placeholder karma
                var placeholderKarma = new
                {
                    avatarId = avatarId,
                    karma = 100,
                    karmaLevel = 5,
                    lastUpdated = DateTime.UtcNow
                };
                return Ok(new { success = true, result = placeholderKarma });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("karma/{avatarId}/add")]
        public async Task<IActionResult> AddKarma(string avatarId, [FromBody] int karma)
        {
            try
            {
                // For now, return success
                return Ok(new { success = true, message = $"Added {karma} karma to avatar {avatarId}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("karma/{avatarId}/remove")]
        public async Task<IActionResult> RemoveKarma(string avatarId, [FromBody] int karma)
        {
            try
            {
                // For now, return success
                return Ok(new { success = true, message = $"Removed {karma} karma from avatar {avatarId}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("karma/{avatarId}/set")]
        public async Task<IActionResult> SetKarma(string avatarId, [FromBody] int karma)
        {
            try
            {
                // For now, return success
                return Ok(new { success = true, message = $"Set karma to {karma} for avatar {avatarId}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("karma")]
        public async Task<IActionResult> GetAllKarma()
        {
            try
            {
                // For now, return placeholder karma data
                var placeholderKarma = new[]
                {
                    new
                    {
                        avatarId = Guid.NewGuid().ToString(),
                        karma = 100,
                        karmaLevel = 5,
                        lastUpdated = DateTime.UtcNow
                    },
                    new
                    {
                        avatarId = Guid.NewGuid().ToString(),
                        karma = 75,
                        karmaLevel = 4,
                        lastUpdated = DateTime.UtcNow.AddHours(-1)
                    }
                };
                return Ok(new { success = true, result = placeholderKarma });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("karma/between")]
        public async Task<IActionResult> GetKarmaBetween([FromQuery] string fromDate, [FromQuery] string toDate)
        {
            try
            {
                // For now, return placeholder karma data
                var placeholderKarma = new[]
                {
                    new
                    {
                        avatarId = Guid.NewGuid().ToString(),
                        karma = 100,
                        karmaLevel = 5,
                        lastUpdated = DateTime.UtcNow
                    }
                };
                return Ok(new { success = true, result = placeholderKarma });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("karma/above/{karmaLevel}")]
        public async Task<IActionResult> GetKarmaAbove(int karmaLevel)
        {
            try
            {
                // For now, return placeholder karma data
                var placeholderKarma = new[]
                {
                    new
                    {
                        avatarId = Guid.NewGuid().ToString(),
                        karma = 100,
                        karmaLevel = 5,
                        lastUpdated = DateTime.UtcNow
                    }
                };
                return Ok(new { success = true, result = placeholderKarma });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("karma/below/{karmaLevel}")]
        public async Task<IActionResult> GetKarmaBelow(int karmaLevel)
        {
            try
            {
                // For now, return placeholder karma data
                var placeholderKarma = new[]
                {
                    new
                    {
                        avatarId = Guid.NewGuid().ToString(),
                        karma = 50,
                        karmaLevel = 3,
                        lastUpdated = DateTime.UtcNow
                    }
                };
                return Ok(new { success = true, result = placeholderKarma });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    // Request models
    public class IgniteRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class BeamInRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateAvatarRequest
    {
        public string Title { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LightRequest
    {
        public string OAPPName { get; set; } = string.Empty;
        public string OAPPDescription { get; set; } = string.Empty;
        public OAPPType OAPPType { get; set; }
        public Guid OAPPTemplateId { get; set; }
        public int OAPPTemplateVersion { get; set; }
        public GenesisType GenesisType { get; set; }
    }

    public class SeedRequest
    {
        public string FullPathToOAPP { get; set; } = string.Empty;
        public string LaunchTarget { get; set; } = string.Empty;
        public string FullPathToPublishTo { get; set; } = string.Empty;
        public bool RegisterOnSTARNET { get; set; } = true;
        public bool DotnetPublish { get; set; } = true;
        public bool GenerateOAPPSource { get; set; } = true;
        public bool UploadOAPPSourceToSTARNET { get; set; } = true;
        public bool MakeOAPPSourcePublic { get; set; } = false;
        public bool GenerateOAPPBinary { get; set; } = true;
        public bool GenerateOAPPSelfContainedBinary { get; set; } = false;
        public bool GenerateOAPPSelfContainedFullBinary { get; set; } = false;
        public bool UploadOAPPToCloud { get; set; } = false;
        public bool UploadOAPPSelfContainedToCloud { get; set; } = false;
        public bool UploadOAPPSelfContainedFullToCloud { get; set; } = false;
    }

    public class UnSeedRequest
    {
        public Guid OAPPId { get; set; }
        public int Version { get; set; } = 0;
    }

    public class LoginAvatarRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
*/