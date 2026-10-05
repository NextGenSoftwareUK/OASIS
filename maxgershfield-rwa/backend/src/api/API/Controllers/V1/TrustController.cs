using Microsoft.AspNetCore.Mvc;
using Application.Contracts;
using Application.DTOs.Trust;

namespace API.Controllers.V1;

[ApiController]
[Route("api/v1/trusts")]
public sealed class TrustController : BaseController
{
    private readonly ITrustService _trustService;

    public TrustController(ITrustService trustService)
    {
        _trustService = trustService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTrustAsync([FromBody] CreateTrustRequest request)
    {
        var result = await _trustService.CreateTrustAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTrustAsync(Guid id)
    {
        var result = await _trustService.GetTrustAsync(id);
        return Ok(result);
    }

    [HttpPost("{trustId}/properties")]
    public async Task<IActionResult> AddPropertyAsync(Guid trustId, [FromBody] AddPropertyRequest request)
    {
        var result = await _trustService.AddPropertyAsync(trustId, request);
        return Ok(result);
    }

    [HttpPost("{trustId}/generate-contract")]
    public async Task<IActionResult> GenerateContractAsync(Guid trustId, [FromBody] GenerateContractRequest request)
    {
        var result = await _trustService.GenerateContractAsync(trustId, request);
        return Ok(result);
    }

    [HttpGet("{trustId}/contracts")]
    public async Task<IActionResult> GetContractsAsync(Guid trustId)
    {
        var result = await _trustService.GetContractsAsync(trustId);
        return Ok(result);
    }
}
