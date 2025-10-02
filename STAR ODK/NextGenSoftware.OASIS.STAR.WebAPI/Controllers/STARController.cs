using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// STAR system management endpoints for controlling the STAR API lifecycle.
    /// Provides endpoints for checking status, igniting (starting), and extinguishing (stopping) the STAR system.
    /// </summary>
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

        /// <summary>
        /// Retrieves the current status of the STAR system.
        /// </summary>
        /// <returns>The current ignition status of the STAR system.</returns>
        /// <response code="200">Status retrieved successfully</response>
        /// <response code="400">Error retrieving status</response>
        [HttpGet("status")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Ignites (starts) the STAR system with optional authentication credentials.
        /// </summary>
        /// <param name="request">Optional authentication request with username and password.</param>
        /// <returns>Result of the STAR ignition process.</returns>
        /// <response code="200">STAR ignition completed (success or failure)</response>
        [HttpPost("ignite")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Extinguishes (stops) the STAR system.
        /// </summary>
        /// <returns>Result of the STAR extinguishing process.</returns>
        /// <response code="200">STAR extinguishing completed (success or failure)</response>
        [HttpPost("extinguish")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Beams in (authenticates) an avatar to the STAR system.
        /// </summary>
        /// <param name="request">Authentication request with username and password.</param>
        /// <returns>Result of the beam-in process.</returns>
        /// <response code="200">Beam-in completed (success or failure)</response>
        [HttpPost("beam-in")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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
