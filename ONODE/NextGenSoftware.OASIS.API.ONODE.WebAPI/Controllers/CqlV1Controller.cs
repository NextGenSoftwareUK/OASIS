using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>Bounded native CQL leaf queries. Requires a configured usage/storage admission route.</summary>
[ApiController]
[Route("api/data/query-cql")]
[Authorize]
[ServiceFilter(typeof(CqlV1ResourceFilter))]
public sealed class CqlV1Controller : ControllerBase
{
    private readonly ICqlV1Providers providers;
    public CqlV1Controller(ICqlV1Providers providers) => this.providers = providers;

    [HttpPost]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> Query(CqlV1Request request) => CqlV1Endpoint.ExecuteAsync(HttpContext, request, providers);
}
