using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

public partial class DataController
{
    public sealed class RecoverConditionalOperationRequest
    {
        public Guid OperationId { get; set; }
        public string ProviderType { get; set; }
    }

    /// <summary>Read-only recovery from the protected original intent. Absence
    /// never authorizes replay, and elevated roles cannot substitute another actor.</summary>
    [Authorize]
    [HttpPost("~" + CosmicOperationRecoveryRoutes.Recover)]
    public async Task<OASISHttpResponseMessage<ConditionalHolonWriteOutcome>> RecoverConditionalOperation(
        [FromBody] RecoverConditionalOperationRequest request)
    {
        var result = await HostedCosmicRecoveryEndpoint.RecoverAsync(HttpContext, OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.HyperDriveMode,
            request?.OperationId ?? Guid.Empty, request?.ProviderType, () => HolonManager);
        var status = HostedCosmicRecoveryEndpoint.Status(result);
        Response.StatusCode = (int)status;
        return HttpResponseHelper.FormatResponse(result, status, false);
    }
}
