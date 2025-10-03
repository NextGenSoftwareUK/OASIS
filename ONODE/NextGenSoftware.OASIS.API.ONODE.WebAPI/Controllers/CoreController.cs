using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Core OASIS system endpoints for fundamental operations and system management.
    /// Provides access to core OASIS functionality and system-level operations.
    /// </summary>
    [ApiController]
    [Route("api/core")]
    public class CoreController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public CoreController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        public CoreController()
        {
          //  _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Generate a new Moon (OApp) PREVIEW - COMING SOON...
        /// </summary>
        /// <returns>OASIS result indicating whether moon generation was successful.</returns>
        /// <response code="200">Moon generation completed (success or failure)</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("generate-moon")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<bool> GenerateMoon()
        {
            // TODO: Finish implementing.
            return new ()
            {
                IsError = false,
                Result = true
            };
        }
    }
}
