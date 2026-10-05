namespace API.Controllers.V1;

[Route($"{ApiAddresses.Base}/rwa")]
public class RwaTokenController(IRwaTokenService service) : V1BaseController
{
    [HttpPost("tokenize")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateRwaTokenRequest request, CancellationToken token)
        => (await service.CreateAsync(request, token)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetailAsync(Guid id, CancellationToken token)
        => (await service.GetDetailAsync(id, token)).ToActionResult();


    [HttpGet("me")]
    public async Task<IActionResult> GetTokensCurrentOwnerAsync([FromQuery] RwaTokenOwnerFilter filter,
        CancellationToken token)
        => (await service.GetTokensOwnedByCurrentUserAsync(filter, token)).ToActionResult();


    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] RwaTokenFilter filter, CancellationToken token)
        => (await service.GetAllAsync(filter, token)).ToActionResult();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id,
        [FromBody] UpdateRwaTokenRequest request,
        CancellationToken token)
        => (await service.UpdateAsync(id, request, token)).ToActionResult();
}