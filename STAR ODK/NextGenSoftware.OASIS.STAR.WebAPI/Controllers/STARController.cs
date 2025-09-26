using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
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
                Console.WriteLine($"STAR Status Check: IsOASISBooted = {isIgnited}");
                return Ok(new { isIgnited, status = isIgnited ? "ignited" : "extinguished" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"STAR Status Error: {ex.Message}");
                Console.WriteLine($"STAR Status StackTrace: {ex.StackTrace}");
                return BadRequest(new { error = ex.Message, details = ex.StackTrace });
            }
        }

        [HttpPost("ignite")]
        public async Task<IActionResult> IgniteSTAR([FromBody] IgniteRequest? request = null)
        {
            try
            {
                Console.WriteLine($"STAR Ignite Request: UserName = {request?.UserName ?? "admin"}");
                var starAPI = GetSTARAPI();
                var result = await starAPI.BootOASISAsync(
                    request?.UserName ?? "admin", 
                    request?.Password ?? "admin"
                );
                Console.WriteLine($"STAR Ignite Result: IsError = {result.IsError}, Message = {result.Message}");
                if (result.IsError)
                {
                    return Ok(new { 
                        result = result.Result, 
                        isError = true, 
                        message = result.Message,
                        exception = result.Exception?.ToString()
                    });
                }
                return Ok(new { 
                    result = result.Result, 
                    isError = false, 
                    message = "STAR ignited successfully" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"STAR Ignite Error: {ex.Message}");
                Console.WriteLine($"STAR Ignite StackTrace: {ex.StackTrace}");
                return Ok(new { 
                    result = false, 
                    isError = true, 
                    message = ex.Message,
                    exception = ex.StackTrace
                });
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
                    return Ok(new { 
                        result = result.Result, 
                        isError = true, 
                        message = result.Message,
                        exception = result.Exception?.ToString()
                    });
                }
                return Ok(new { 
                    result = result.Result, 
                    isError = false, 
                    message = "STAR extinguished successfully" 
                });
            }
            catch (Exception ex)
            {
                return Ok(new { 
                    result = false, 
                    isError = true, 
                    message = ex.Message,
                    exception = ex.StackTrace
                });
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
}
