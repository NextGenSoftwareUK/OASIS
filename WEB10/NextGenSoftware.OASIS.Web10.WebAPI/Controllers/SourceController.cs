using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web10.Core.Managers;
using NextGenSoftware.OASIS.Web10.Core.Models;

namespace NextGenSoftware.OASIS.Web10.WebAPI.Controllers
{
    /// <summary>The Source / WEB0 - the root of the OASIS stack. "The Omega that is the Alpha."</summary>
    [ApiController]
    [Route("v1/source")]
    public class SourceController : ControllerBase
    {
        private static readonly SourceManager _manager = new SourceManager();

        /// <summary>Returns the foundational runtime/version identity plus the live unified status across every layer built on top of it.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(SourceReport), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSource()
        {
            SourceReport report = await _manager.GetSourceAsync();
            return Ok(report);
        }
    }
}
