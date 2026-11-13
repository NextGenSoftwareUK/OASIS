using System;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CqlSourceRecoveryRequest(Guid OperationId, long ExpectedGeneration, Guid HostInstanceId);

/// <summary>Operator health recovery, separate from ordinary priced query traffic.</summary>
[ApiController]
[Route("api/admin/cql/sources")]
[Authorize(AvatarType.Wizard)]
public sealed class CqlSourceRecoveryController : ControllerBase
{
    private readonly CqlHostedSources sources;
    public CqlSourceRecoveryController(CqlHostedSources sources) => this.sources = sources;

    [HttpPost("{provider}/recover")]
    [RequestSizeLimit(1024)]
    public async Task<IActionResult> Recover(string provider, CqlSourceRecoveryRequest request)
    {
        // Enforce authority at the operation boundary as well as the MVC filter.
        // A normal temporary sandbox avatar cannot change shared source lifetime.
        var actor = HttpContext?.Items["Avatar"] as IAvatar;
        if (actor == null || actor.Id == Guid.Empty)
            return Unauthorized(new OASISResult<object> { IsError = true, ErrorCode = "CQL_AUTHENTICATION_REQUIRED" });
        if (actor.AvatarType?.Value != AvatarType.Wizard)
            return StatusCode(403, new OASISResult<object> { IsError = true, ErrorCode = "CQL_OPERATOR_REQUIRED" });
        if (request == null || !Enum.TryParse<ProviderType>(provider, out var type)
            || !Enum.IsDefined(type) || type.ToString() != provider)
            return BadRequest(new OASISResult<object> { IsError = true, ErrorCode = "CQL_SOURCE_RECOVERY_REQUEST_INVALID" });
        if (sources == null)
            return StatusCode(503, new OASISResult<object> { IsError = true, ErrorCode = "CQL_SOURCE_CATALOGUE_UNAVAILABLE" });

        var result = await sources.RecoverReadSourceAsync(type, request.OperationId,
            request.ExpectedGeneration, request.HostInstanceId, HttpContext.RequestAborted);
        if (!result.IsError) return Ok(result);
        return StatusCode(result.ErrorCode switch
        {
            "CQL_SOURCE_RECOVERY_REQUEST_INVALID" => 400,
            "CQL_SOURCE_RECOVERY_NOT_ENROLLED" => 400,
            "CQL_SOURCE_RECOVERY_BUSY" or "CQL_SOURCE_GENERATION_CONFLICT" or "CQL_SOURCE_RECOVERY_NOT_FAILED"
                or "CQL_SOURCE_OPERATION_CONFLICT" or "CQL_SOURCE_HOST_INSTANCE_CONFLICT" => 409,
            "CQL_SOURCE_RECOVERY_CANCELLED" => 408,
            _ => 503
        }, result);
    }
}
