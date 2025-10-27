using Microsoft.AspNetCore.Mvc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Health check endpoint for Railway deployment
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                service = "OASIS API ONODE WebAPI"
            });
        }

        /// <summary>
        /// Health check endpoint for Railway deployment
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                service = "OASIS API ONODE WebAPI"
            });
        }
    }
}
