using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>Operator view of the on-demand sandbox sidecars: state, last use, uptime and memory.</summary>
[ApiController]
[Route("api/admin/cql/sidecars")]
[Authorize(AvatarType.Wizard)]
public sealed class CqlSidecarsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var actor = HttpContext?.Items["Avatar"] as IAvatar;
        if (actor == null || actor.AvatarType?.Value != AvatarType.Wizard)
            return StatusCode(403, new OASISResult<object> { IsError = true, ErrorCode = "CQL_OPERATOR_REQUIRED" });
        var status = await CqlSandboxSidecar.StatusAsync(HttpContext.RequestAborted);
        return status == null
            ? Ok(new OASISResult<object> { Message = "Sidecars are not running on demand on this node." })
            : Content(status, "application/json");
    }
}
