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
                return Ok(new { isIgnited, status = isIgnited ? "ignited" : "extinguished" });
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
                return Ok(new { success = true, message = "STAR ignited successfully", result = result.Result });
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
}