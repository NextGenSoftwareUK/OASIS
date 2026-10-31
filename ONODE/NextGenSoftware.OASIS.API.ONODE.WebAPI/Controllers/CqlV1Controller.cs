using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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

    [HttpGet("providers")]
    public IActionResult Providers()
    {
        var catalogue = HttpContext.RequestServices.GetService<CqlHostedSources>();
        if (catalogue == null)
            return StatusCode(503, new NextGenSoftware.OASIS.Common.OASISResult<object>
                { IsError = true, ErrorCode = "CQL_SOURCE_CATALOGUE_UNAVAILABLE" });
        return Ok(new NextGenSoftware.OASIS.Common.OASISResult<object>(catalogue.Describe()));
    }

    [HttpPost]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> Query(CqlV1Request request) => CqlV1Endpoint.ExecuteAsync(HttpContext, request, providers);

    [HttpPost("indexed")]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> Indexed(CqlIndexedV1Request request) => CqlV1Endpoint.ExecuteIndexedAsync(HttpContext, request);

    [HttpPost("indexed/scalar")]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> IndexedScalar(CqlIndexedScalarV1Request request) => CqlV1Endpoint.ExecuteIndexedScalarAsync(HttpContext, request);

    [HttpPost("scalar")]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> Scalar(CqlScalarV1Request request) => CqlV1Endpoint.ExecuteScalarAsync(HttpContext, request, providers);

    [HttpPost("projection")]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> Projection(CqlProjectionV1Request request) => CqlV1Endpoint.ExecuteProjectionAsync(HttpContext, request, providers);
    [HttpPost("grouped")]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> Grouped(CqlGroupedV1Request request) => CqlV1Endpoint.ExecuteGroupedAsync(HttpContext, request, providers);

    [HttpPost("join")]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> Join(CqlJoinV1Request request) => CqlV1Endpoint.ExecuteJoinAsync(HttpContext, request, providers);

    [HttpPost("composition")]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> Composition(CqlCompositionV1Request request) => CqlV1Endpoint.ExecuteCompositionAsync(HttpContext, request, providers);

    [HttpPost("federation")]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> Federation(CqlFederationV1Request request) => CqlV1Endpoint.ExecuteFederationAsync(HttpContext, request, providers);
}
